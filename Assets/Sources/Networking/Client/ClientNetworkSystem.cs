using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using DisruptorUnity3d;
using ENet;
using Entitas;
using NetStack.Serialization;
using Sources.Networking.Server;
using EventType = ENet.EventType;
using Logger = Sources.Tools.Logger;

namespace Sources.Networking.Client
{
    public class ClientNetworkSystem : IExecuteSystem, ITearDownSystem
    {
        public ClientState  State = ClientState.Disconnected;
        public ConnectionId ConnectionId;
        public Peer         ServerConnection;

        public ushort TickRate           = 20;
        public int    PanicCleanupTarget = 6;
        public int    PanicStateCount    = 10;

        public int StatesCount => _states.Count;

        private readonly GameContext          _game;
        private readonly ClientCommandHandler _handler;

        private readonly Thread                            _networkThread;
        private readonly RingBuffer<ReceivedEvent>         _eventsToHandle = new RingBuffer<ReceivedEvent>(1024);
        private readonly RingBuffer<SendData>              _sendData       = new RingBuffer<SendData>(64);
        private readonly RingBuffer<NetworkThreadResponse> _responses      = new RingBuffer<NetworkThreadResponse>(8);
        private readonly RingBuffer<NetworkThreadRequest>  _requests       = new RingBuffer<NetworkThreadRequest>(8);
        private          Address                           _address;

        private readonly Queue<IntPtr> _states = new Queue<IntPtr>(124);

        private readonly IGroup<GameEntity> _syncGroup;
        private readonly List<GameEntity>   _syncBuffer = new List<GameEntity>(ServerNetworkSystem.MaxPlayers);

        public          ushort    EnqueuedCommandCount;
        public readonly BitBuffer ToServer = new BitBuffer(512);

        private readonly BitBuffer _fromServer = new BitBuffer(512);

        private bool _firstPacket = true;
        private Host _host        = new Host();

        private readonly PacketFreeCallback _freeCallback = packet => { Marshal.FreeHGlobal(packet.Data); };
        private readonly IntPtr             _cachedFreeCallback;

        public ClientNetworkSystem(Contexts contexts)
        {
            Logger.I.Log(this, "Created");

            _game = contexts.game;
            _host.Create();
            _handler           = new ClientCommandHandler(_game, this);
            ConnectionId.IsSet = false;

            _networkThread = NetworkThread();
            _networkThread.Start();
            _syncGroup          = _game.GetGroup(GameMatcher.Sync);
            _cachedFreeCallback = Marshal.GetFunctionPointerForDelegate(_freeCallback);
        }

        public void Execute()
        {
            if (_states.Count > PanicStateCount)
            {
                //Catchup after lag
                while (_states.Count > PanicCleanupTarget)
                {
                    var state = _states.Dequeue();
                    ExecuteState(state);
                }
            }
            else if (_states.Count > 0)
            {
                var state = _states.Dequeue();
                ExecuteState(state);
            }
        }

        public void TearDown()
        {
            if (State == ClientState.Connected) _requests.Enqueue(NetworkThreadRequest.Disconnect);

            Thread.Sleep(20);
            _networkThread.Abort();
        }


        private Thread NetworkThread()
        {
            return new Thread(() =>
            {
                while (true)
                {
                    while (_sendData.TryDequeue(out var data))
                    {
                        var packet = new Packet();
                        packet.Create(data.Data, data.Length, PacketFlags.Reliable | PacketFlags.NoAllocate);
                        packet.SetFreeCallback(_cachedFreeCallback);
                        data.Peer.Send(0, ref packet);
                    }

                    while (_requests.TryDequeue(out var request))
                        switch (request)
                        {
                            case NetworkThreadRequest.Connect:
                                try
                                {
                                    _host.Connect(_address, 2);
                                }
                                catch (Exception e)
                                {
                                    Logger.I.Log(this, e.Message);
                                    _host = new Host();
                                    _host.Create();
                                    _responses.Enqueue(NetworkThreadResponse.ConnectFailure);
                                }

                                break;
                            case NetworkThreadRequest.Disconnect:
                                ServerConnection.DisconnectNow(0);
                                _host.Flush();
                                _host.Dispose();
                                _host = new Host();
                                _host.Create();
                                _responses.Enqueue(NetworkThreadResponse.Disconnected);
                                break;
                            case NetworkThreadRequest.Cleanup:
                                _host = new Host();
                                _host.Create();
                                break;
                            case NetworkThreadRequest.CancelConnect:
                                _host.Dispose();
                                _host = new Host();
                                _host.Create();
                                _responses.Enqueue(NetworkThreadResponse.ConnectCancelled);
                                break;
                        }

                    if (!_host.IsSet) Thread.Sleep(15);
                    if (_host.Service(15, out var @event) > 0)
                        switch (@event.Type)
                        {
                            case EventType.Connect:
                            case EventType.Disconnect:
                            case EventType.Timeout:
                                _eventsToHandle.Enqueue(new ReceivedEvent
                                    {EventType = @event.Type, Peer = @event.Peer});
                                break;
                            case EventType.Receive:
                                unsafe
                                {
                                    var length = @event.Packet.Length;
                                    var newPtr = Marshal.AllocHGlobal(length);
                                    Buffer.MemoryCopy(@event.Packet.Data.ToPointer(), newPtr.ToPointer(), length,
                                        length);
                                    _eventsToHandle.Enqueue(new ReceivedEvent
                                        {Data = newPtr, Peer = @event.Peer, EventType = EventType.Receive});
                                }
                                
                                @event.Packet.Dispose();
                                break;
                        }
                }
            });
        }

        public void Connect(Address address)
        {
            if (State != ClientState.Disconnected) return;

            Logger.I.Log(this, $"Connecting to {address.GetIP()}:{address.Port}");
            State = ClientState.Connecting;

            _address = address;
            _requests.Enqueue(NetworkThreadRequest.Connect);
        }

        public void Disconnect()
        {
            if (State != ClientState.Connected) return;

            Logger.I.Log(this, "Disconnecting");

            State = ClientState.Disconnecting;
            _requests.Enqueue(NetworkThreadRequest.Disconnect);
        }

        public void EnqueueCommand<T>(T command) where T : ICommand, IClientCommand
        {
            Logger.I.Log(this, $"Enqueued {command.GetType().Name}");

            EnqueuedCommandCount++;
            command.Serialize(ToServer);
        }

        public void EnqueueSendData(SendData data)
        {
            _sendData.Enqueue(data);
        }

        public void EnqueueRequest(NetworkThreadRequest request)
        {
            _requests.Enqueue(request);
        }

        public void UpdateNetwork()
        {
            while (_responses.TryDequeue(out var response))
                switch (response)
                {
                    case NetworkThreadResponse.ConnectFailure:
                        State = ClientState.Disconnected;
                        break;
                    case NetworkThreadResponse.Disconnected:
                        CleanupState();
                        Logger.I.Log(this, "Disconnected");
                        break;
                    case NetworkThreadResponse.ConnectCancelled:
                        CleanupState();
                        Logger.I.Log(this, "Connect cancelled");
                        break;
                }

            if (State == ClientState.Disconnected) return;
            while (_eventsToHandle.TryDequeue(out var @event))
                switch (@event.EventType)
                {
                    case EventType.Connect:
                        _handler.OnConnected(@event.Peer);
                        break;
                    case EventType.Disconnect:
                        _handler.OnDisconnected(@event.Peer);
                        break;
                    case EventType.Receive:
                        _states.Enqueue(@event.Data);
                        break;
                    case EventType.Timeout:
                        _handler.OnDisconnected(@event.Peer);
                        break;
                }
        }

        private unsafe void ExecuteState(IntPtr state)
        {
            PackedDataFlags flags;
            int cursor;
            
            if (_firstPacket)
            {
                _firstPacket = false;
                flags = PackedDataFlags.Commands | PackedDataFlags.CreatedEntities;
                cursor = 0;
            }
            else
            {
                flags = (PackedDataFlags) Marshal.ReadByte(state);
                cursor = 1;
            }
            
            #region commands

            if ((flags & PackedDataFlags.Commands) == PackedDataFlags.Commands)
            {
                var commandsHeaderSpan = new ReadOnlySpan<ushort>(IntPtr.Add(state, cursor).ToPointer(), 2);
                var commandCount       = commandsHeaderSpan[0];
                var commandLength      = commandsHeaderSpan[1];
                cursor += 4;

                if (commandCount > 0)
                {
                    var dataSpan = new ReadOnlySpan<byte>(IntPtr.Add(state, cursor).ToPointer(),
                        commandLength);
                    _fromServer.Clear();
                    _fromServer.FromSpan(ref dataSpan, commandLength);
                    ClientCommandExecutor.Execute(_handler, _fromServer, commandCount);
                    cursor += commandLength;
                }
            }
            #endregion

            #region created entities

            if ((flags & PackedDataFlags.CreatedEntities) == PackedDataFlags.CreatedEntities)
            {
                var createdEntitiesHeaderSpan =
                    new ReadOnlySpan<ushort>(IntPtr.Add(state, cursor).ToPointer(), 2);
                var createdEntitiesCount  = createdEntitiesHeaderSpan[0];
                var createdEntitiesLength = createdEntitiesHeaderSpan[1];
                cursor += 4;

                if (createdEntitiesCount > 0)
                {
                    var dataSpan = new ReadOnlySpan<byte>(IntPtr.Add(state, cursor).ToPointer(),
                        createdEntitiesLength);
                    _fromServer.Clear();
                    _fromServer.FromSpan(ref dataSpan, createdEntitiesLength);
                    UnpackEntityUtility.CreateEntities(_game, _fromServer, createdEntitiesCount);
                    cursor += createdEntitiesLength;
                }
            }

            #endregion

            #region removed entities

            if ((flags & PackedDataFlags.RemovedEntities) == PackedDataFlags.RemovedEntities)
            {
                var removedEntitiesHeaderSpan =
                    new ReadOnlySpan<ushort>(IntPtr.Add(state, cursor).ToPointer(), 2);
                var removedEntitiesCount  = removedEntitiesHeaderSpan[0];
                var removedEntitiesLength = removedEntitiesHeaderSpan[1];

                cursor += 4;

                if (removedEntitiesCount > 0)
                {
                    var dataSpan = new ReadOnlySpan<byte>(IntPtr.Add(state, cursor).ToPointer(),
                        removedEntitiesLength);
                    _fromServer.Clear();
                    _fromServer.FromSpan(ref dataSpan, removedEntitiesLength);
                    UnpackEntityUtility.RemoveEntities(_game, _fromServer, removedEntitiesCount);
                    cursor += removedEntitiesLength;
                }
            }

            #endregion

            #region removed components

            if ((flags & PackedDataFlags.RemovedComponents) == PackedDataFlags.RemovedComponents)
            { 
                var removedComponentsHeaderSpan =
                    new ReadOnlySpan<ushort>(IntPtr.Add(state, cursor).ToPointer(), 2);
                var removedComponentsCount  = removedComponentsHeaderSpan[0];
                var removedComponentsLength = removedComponentsHeaderSpan[1];
                cursor += 4;

                if (removedComponentsCount > 0)
                {
                    var dataSpan = new ReadOnlySpan<byte>(IntPtr.Add(state, cursor).ToPointer(),
                        removedComponentsLength);
                    _fromServer.Clear();
                    _fromServer.FromSpan(ref dataSpan, removedComponentsLength);
                    UnpackEntityUtility.RemoveComponents(_game, _fromServer, removedComponentsCount);
                    cursor += removedComponentsLength;
                }
            }

            #endregion

            #region changed components
            
            if ((flags & PackedDataFlags.ChangedComponents) == PackedDataFlags.ChangedComponents)
            {
                var changedComponentsHeaderSpan =
                    new ReadOnlySpan<ushort>(IntPtr.Add(state, cursor).ToPointer(), 2);
                var changedComponentsCount  = changedComponentsHeaderSpan[0];
                var changedComponentsLength = changedComponentsHeaderSpan[1];
                cursor += 4;

                if (changedComponentsCount > 0)
                {
                    var dataSpan = new ReadOnlySpan<byte>(IntPtr.Add(state, cursor).ToPointer(),
                        changedComponentsLength);
                    _fromServer.Clear();
                    _fromServer.FromSpan(ref dataSpan, changedComponentsLength);
                    UnpackEntityUtility.ChangeComponents(_game, _fromServer, changedComponentsCount);
                }
            }

            #endregion

            Marshal.FreeHGlobal(state);
        }

        public void CleanupState()
        {
            while (_eventsToHandle.TryDequeue(out _))
            {
            }

            while (_states.Count > 0) Marshal.FreeHGlobal(_states.Dequeue());

            State              = ClientState.Disconnected;
            ServerConnection   = new Peer();
            ConnectionId.IsSet = false;
            _firstPacket       = true;

            _syncGroup.GetEntities(_syncBuffer);
            foreach (var e in _syncBuffer) e.isDestroyed = true;

            EnqueuedCommandCount = 0;
            ToServer.Clear();
            _fromServer.Clear();
        }
    }

    public enum NetworkThreadRequest
    {
        Connect,
        CancelConnect,
        Disconnect,
        Cleanup
    }

    public enum NetworkThreadResponse
    {
        ConnectCancelled,
        ConnectFailure,
        Disconnected
    }

    public enum ClientState
    {
        Disconnected,
        Connecting,
        WaitingForId,
        Connected,
        Disconnecting
    }

    public struct ConnectionId
    {
        public bool   IsSet;
        public ushort Id;
    }
}
