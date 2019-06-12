using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using DisruptorUnity3d;
using ENet;
using Entitas;
using NetStack.Serialization;
using Sources.Tools;

namespace Sources.Networking.Server
{
    public class ServerNetworkSystem : IExecuteSystem, ITearDownSystem
    {
        public const ushort MaxPlayers = 5;
        public       ushort TickRate   = 20;

        private readonly List<GameEntity>   _connectionsBuffer = new List<GameEntity>(MaxPlayers);
        private readonly IGroup<GameEntity> _connectionsGroup;


        private readonly GameContext          _game;
        private readonly ServerCommandHandler _handler;

        //Data for clients
        public          ushort    ChangedComponentsCount;
        public readonly BitBuffer ChangedComponents = new BitBuffer(512);
        public          ushort    CreatedEntitiesCount;
        public readonly BitBuffer CreatedEntities = new BitBuffer(512);
        public          ushort    RemovedComponentsCount;
        public readonly BitBuffer RemovedComponents = new BitBuffer(512);
        public          ushort    RemovedEntitiesCount;
        public readonly BitBuffer RemovedEntities = new BitBuffer(512);

        //Data from clients
        private readonly BitBuffer _fromClients = new BitBuffer(512);

        private ushort _currentPeerId;
        private Host   _host = new Host();

        private readonly Thread                            _networkThread;
        private readonly RingBuffer<ReceivedEvent>         _eventsToHandle = new RingBuffer<ReceivedEvent>(1024);
        private readonly RingBuffer<DisconnectData>        _disconnectData = new RingBuffer<DisconnectData>(128);
        private readonly RingBuffer<SendData>              _sendData       = new RingBuffer<SendData>(1024);
        private readonly RingBuffer<NetworkThreadResponse> _responses      = new RingBuffer<NetworkThreadResponse>(8);
        private readonly RingBuffer<NetworkThreadRequest>  _requests       = new RingBuffer<NetworkThreadRequest>(8);
        private          Address                           _address;

        public ServerState State = ServerState.Stopped;

        private readonly PacketFreeCallback _freeCallback = packet => { Marshal.FreeHGlobal(packet.Data); };
        private readonly IntPtr             _cachedFreeCallback;

        public ServerNetworkSystem(Contexts contexts)
        {
            Logger.I.Log(this, "Created");
            _game    = contexts.game;
            _handler = new ServerCommandHandler(_game, this);

            _networkThread = NetworkThread();
            _networkThread.Start();
            _connectionsGroup   = _game.GetGroup(GameMatcher.Connection);
            _cachedFreeCallback = Marshal.GetFunctionPointerForDelegate(_freeCallback);
        }

        public void Execute()
        {
            while (_responses.TryDequeue(out var response))
                switch (response)
                {
                    case NetworkThreadResponse.StartSuccess:
                        Logger.I.Log(this, "Server is working");
                        State = ServerState.Working;
                        break;
                    case NetworkThreadResponse.StartFailure:
                        Logger.I.Log(this, "Server start failed");
                        State = ServerState.Stopped;
                        break;
                    case NetworkThreadResponse.Stoppoed:
                        Logger.I.Log(this, "Server is stopped");
                        ClearBuffers();
                        while (_eventsToHandle.TryDequeue(out _))
                        {
                        }

                        State = ServerState.Stopped;
                        break;
                }

            if (State != ServerState.Working) return;
            while (_eventsToHandle.TryDequeue(out var @event))
                unsafe
                {
                    switch (@event.EventType)
                    {
                        case EventType.Connect:
                            _handler.OnClientConnected(@event.Peer);
                            break;
                        case EventType.Disconnect:
                            _handler.OnClientDisconnected(@event.Peer);
                            break;
                        case EventType.Receive:
                            _currentPeerId           = (ushort) @event.Peer.ID;
                            _handler.CurrentClientId = _currentPeerId;

                            var e = _game.GetEntityWithConnection(_currentPeerId);
                            if (e == null)
                            {
                                Marshal.FreeHGlobal(@event.Data);
                                break;
                            }

                            var headerSpan    = new ReadOnlySpan<ushort>(@event.Data.ToPointer(), 2);
                            var commandCount  = headerSpan[0];
                            var commandLength = headerSpan[1];

                            if (commandCount > 0)
                            {
                                var commandsSpan =
                                    new ReadOnlySpan<byte>(IntPtr.Add(@event.Data, 4).ToPointer(), commandLength);
                                _fromClients.Clear();
                                _fromClients.FromSpan(ref commandsSpan, commandLength);
                                ServerCommandExecutor.Execute(_handler, _fromClients, commandCount);
                            }

                            Marshal.FreeHGlobal(@event.Data);
                            _currentPeerId = ushort.MaxValue;
                            break;
                        case EventType.Timeout:
                            _handler.OnClientDisconnected(@event.Peer);
                            break;
                    }
                }
        }

        public void TearDown()
        {
            StopServer();
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

                    while (_disconnectData.TryDequeue(out var data)) data.Peer.DisconnectNow(0);

                    while (_requests.TryDequeue(out var request))
                        switch (request)
                        {
                            case NetworkThreadRequest.Start:
                                try
                                {
                                    _host.Create(_address, MaxPlayers * 2, 2);
                                    _responses.Enqueue(NetworkThreadResponse.StartSuccess);
                                }
                                catch (Exception e)
                                {
                                    Logger.I.Log(this, e.Message);
                                    _host = new Host();
                                    _responses.Enqueue(NetworkThreadResponse.StartFailure);
                                }

                                break;
                            case NetworkThreadRequest.Stop:
                                _host.Flush();
                                _host.Dispose();
                                _host = new Host();
                                _responses.Enqueue(NetworkThreadResponse.Stoppoed);
                                break;
                        }

                    if (!_host.IsSet)
                    {
                        Thread.Sleep(15);
                    }
                    else
                    {
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
                }
            });
        }

        public void StartServer(Address address)
        {
            if (State != ServerState.Stopped) return;

            Logger.I.Log(this, $"Starting server on {address.GetIP()}:{address.Port}");
            State = ServerState.Starting;

            _address = address;
            _requests.Enqueue(NetworkThreadRequest.Start);
        }

        public void StopServer()
        {
            if (State != ServerState.Working) return;

            Logger.I.Log(this, "Stopping server");
            State = ServerState.Stopping;

            _connectionsGroup.GetEntities(_connectionsBuffer);
            foreach (var e in _connectionsBuffer)
            {
                _disconnectData.Enqueue(new DisconnectData {Peer = e.connectionPeer.Value});
                e.isDestroyed = true;
            }

            _requests.Enqueue(NetworkThreadRequest.Stop);
        }

        public void EnqueueCommandForEveryone<T>(T command) where T : ICommand, IServerCommand
        {
            Logger.I.Log(this, $"Enqueued {command.GetType().Name} for everyone");

            _connectionsGroup.GetEntities(_connectionsBuffer);
            foreach (var e in _connectionsBuffer)
            {
                e.clientDataBuffer.CommandCount++;
                command.Serialize(e.clientDataBuffer.Value);
            }
        }

        public void EnqueueCommandForClient<T>(ushort connectionId, T command) where T : ICommand, IServerCommand
        {
            Logger.I.Log(this, $"Enqueued {command.GetType().Name} for {connectionId}");

            var e = _game.GetEntityWithConnection(connectionId);
            if (e != null)
            {
                e.clientDataBuffer.CommandCount++;
                command.Serialize(e.clientDataBuffer.Value);
            }
        }

        public void EnqueueSendData(SendData data)
        {
            _sendData.Enqueue(data);
        }

        public void EnqueueDisconnectData(DisconnectData data)
        {
            _disconnectData.Enqueue(data);
        }

        public void ClearBuffers()
        {
            RemovedEntities.Clear();
            RemovedComponents.Clear();
            ChangedComponents.Clear();
            CreatedEntities.Clear();
            ChangedComponentsCount = 0;
            CreatedEntitiesCount   = 0;
            RemovedComponentsCount = 0;
            RemovedEntitiesCount   = 0;
        }

        private enum NetworkThreadRequest
        {
            Start,
            Stop
        }

        private enum NetworkThreadResponse
        {
            StartSuccess,
            StartFailure,
            Stoppoed
        }
    }

    public enum ServerState
    {
        Stopped,
        Starting,
        Working,
        Stopping
    }
}
