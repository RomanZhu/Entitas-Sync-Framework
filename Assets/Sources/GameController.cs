using System;
using System.Collections.Generic;
using ENet;
using Entitas;
using Sources.Networking.Client;
using Sources.Networking.Server;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static          GameController I;
    [NonSerialized] public Mode           Mode = Mode.Inactive;

    [Header("Server")] public int TargetTickPerSecond = 20;

    [Header("Client")] public int PanicStateCount    = 10;
    public                    int PanicCleanupTarget = 6;

    private          IGroup<GameEntity> _connectionsGroup;
    private readonly List<GameEntity>   _connectionsBuffer = new List<GameEntity>(ServerNetworkSystem.MaxPlayers);

    private Contexts _contexts;

    private ClientNetworkSystem _client;
    private ClientFeature       _clientFeature;

    private ServerNetworkSystem _server;
    private ServerFeature       _serverFeature;

    private int    _tickCount;
    private ushort _tickrate = 10;
    private float  _timer    = 1f;
    private int    _totalTicksThisSecond;

    private string _ip = "192.168.1.1";
    private float  _lastUpdate;
    private string _message = "";
    private ushort _port    = 9500;

    private int _lastId;

    private void Awake()
    {
        I = this;
        Library.Initialize();

        _contexts         = Contexts.sharedInstance;
        _connectionsGroup = _contexts.game.GetGroup(GameMatcher.Connection);
        _lastUpdate       = Time.realtimeSinceStartup;
    }

    public void StartServer()
    {
        if (Mode != Mode.Inactive)
            throw new ApplicationException("Can't start server if already active.");

        SetupHooks(_contexts);

        _server          = new ServerNetworkSystem(_contexts);
        _server.TickRate = (ushort) TargetTickPerSecond;

        var services = new Services
        {
            ServerSystem = _server
        };

        _serverFeature = new ServerFeature(_contexts, services);
        Mode           = Mode.Server;
    }

    public void StartClient()
    {
        if (Mode != Mode.Inactive)
            throw new ApplicationException("Can't start client if already active.");

        _client                    = new ClientNetworkSystem(_contexts);
        _client.TickRate           = (ushort) TargetTickPerSecond;
        _client.PanicStateCount    = PanicStateCount;
        _client.PanicCleanupTarget = PanicCleanupTarget;

        var services = new Services
        {
            ClientSystem = _client
        };

        _clientFeature = new ClientFeature(_contexts, services);
        Mode           = Mode.Client;
    }

    private void Update()
    {
        switch (Mode)
        {
            case Mode.Server:
                TargetTickPerSecond = _server.TickRate;
                break;
            case Mode.Client:
                TargetTickPerSecond = _client.TickRate;
                break;
        }

        while (_lastUpdate < Time.realtimeSinceStartup)
        {
            _lastUpdate += 1f / TargetTickPerSecond;
            _tickCount++;
            switch (Mode)
            {
                case Mode.Server:
                    _server.Execute();
                    _serverFeature.Execute();
                    _serverFeature.Cleanup();
                    break;
                case Mode.Client:
                    _client.UpdateNetwork();
                    _client.Execute();
                    _clientFeature.Execute();
                    _clientFeature.Cleanup();
                    break;
            }
        }

        _timer -= Time.deltaTime;
        if (_timer < 0f)
        {
            _totalTicksThisSecond =  _tickCount;
            _tickCount            =  0;
            _timer                += 1;
        }
    }

    private void OnGUI()
    {
        GUILayout.Label($"TPS {_totalTicksThisSecond}");
        switch (Mode)
        {
            case Mode.Inactive:
                if (GUILayout.Button("Setup Server")) StartServer();
                if (GUILayout.Button("Setup Client")) StartClient();
                break;
            case Mode.Server:
                switch (_server.State)
                {
                    case ServerState.Stopped:
                        GUILayout.Label("Server stopped");
                        GUILayout.BeginHorizontal();
                        _ip = GUILayout.TextField(_ip, GUILayout.Width(120));
                        var tmp =
                            GUILayout.TextField(_port.ToString(), GUILayout.Width(60));
                        if (ushort.TryParse(tmp, out var result)) _port = result;
                        if (GUILayout.Button("Start!"))
                        {
                            var address = new Address();
                            address.Port = _port;
                            address.SetHost(_ip);
                            _server.StartServer(address);
                        }

                        GUILayout.EndHorizontal();
                        if (GUILayout.Button("localhost")) _ip = "localhost";
                        break;
                    case ServerState.Starting:
                        GUILayout.Label("Server starting");
                        break;
                    case ServerState.Working:
                        GUILayout.Label("Server working");
                        if (GUILayout.Button("Stop!")) _server.StopServer();
                        break;
                    case ServerState.Stopping:
                        GUILayout.Label("Server stopping");
                        break;
                }

                break;
            case Mode.Client:
                switch (_client.State)
                {
                    case ClientState.Disconnected:
                        GUILayout.Label("Disconnected");
                        GUILayout.BeginHorizontal();
                        _ip = GUILayout.TextField(_ip, GUILayout.Width(120));
                        var tmp =
                            GUILayout.TextField(_port.ToString(), GUILayout.Width(60));
                        if (ushort.TryParse(tmp, out var result)) _port = result;
                        if (GUILayout.Button("Connect!"))
                        {
                            var address = new Address();
                            address.Port = _port;
                            address.SetHost(_ip);
                            _client.Connect(address);
                        }

                        GUILayout.EndHorizontal();
                        if (GUILayout.Button("localhost")) _ip = "localhost";
                        break;
                    case ClientState.Connecting:
                        GUILayout.Label("Connecting");
                        if (GUILayout.Button("Cancel")) _client.EnqueueRequest(NetworkThreadRequest.CancelConnect);
                        break;
                    case ClientState.WaitingForId:
                        GUILayout.Label("WaitingForId");
                        break;
                    case ClientState.Connected:
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Connected");
                        tmp = GUILayout.TextField(_tickrate.ToString(), GUILayout.Width(60));
                        if (ushort.TryParse(tmp, out result))
                            if (result > 0)
                                _tickrate = result;

                        if (GUILayout.Button("Set tickRate"))
                            _client.EnqueueCommand(new ClientSetTickrateCommand {Tickrate = _tickrate});

                        var str = "States :";
                        for (var i = 0; i < _client.StatesCount; i++) str += "#";
                        GUILayout.Label(str);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        _message = GUILayout.TextField(_message, GUILayout.Width(120));
                        if (GUILayout.Button("Send message"))
                            _client.EnqueueCommand(new ClientChatMessageCommand {Message = _message});
                        if (GUILayout.Button("Request character"))
                            _client.EnqueueCommand(new ClientRequestCharacterCommand());
                        GUILayout.EndHorizontal();
                        if (GUILayout.Button("Disconnect")) _client.Disconnect();
                        break;
                    case ClientState.Disconnecting:
                        GUILayout.Label("Disconnecting");
                        break;
                }

                break;
        }
    }

    private void OnDestroy()
    {
        switch (Mode)
        {
            case Mode.Server:
                _server.TearDown();
                _serverFeature.TearDown();
                break;
            case Mode.Client:
                _client.TearDown();
                _clientFeature.TearDown();
                break;
        }

        Library.Deinitialize();
    }

    private void SetupHooks(Contexts contexts)
    {
        contexts.Reset();
        contexts.game.OnEntityCreated += (context, entity) =>
        {
            ((GameEntity) entity).AddId((ushort) _lastId);
            _lastId++;
        };
    }
}

public enum Mode
{
    Inactive,
    Server,
    Client
}