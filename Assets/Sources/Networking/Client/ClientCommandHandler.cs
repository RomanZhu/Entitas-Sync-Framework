using ENet;
using Sources.Tools;

namespace Sources.Networking.Client
{
    public class ClientCommandHandler : IClientHandler
    {
        private readonly ClientNetworkSystem _client;
        private readonly GameContext         _game;

        public ClientCommandHandler(GameContext game, ClientNetworkSystem client)
        {
            _game   = game;
            _client = client;
        }

        public void HandleChatMessageCommand(ref ServerChatMessageCommand command)
        {
            Logger.I.Log(this, $"Client-{command.Sender}: {command.Message}");
        }

        public void HandleGrantedIdCommand(ref ServerGrantedIdCommand command)
        {
            Logger.I.Log(this, $"Got ID - {command.Id}");
            _client.State              = ClientState.Connected;
            _client.ConnectionId.IsSet = true;
            _client.ConnectionId.Id    = command.Id;
        }

        public void HandleSetTickrateCommand(ref ServerSetTickrateCommand command)
        {
            _client.TickRate = command.Tickrate;
        }

        public void OnConnected(Peer peer)
        {
            Logger.I.Log(this, $"Connected to server {peer.IP} {peer.Port}, waiting for ID");
            _client.State            = ClientState.WaitingForId;
            _client.ServerConnection = peer;
        }

        public void OnDisconnected(Peer peer)
        {
            Logger.I.Log(this, "Disconnected from server");
            _client.EnqueueRequest(NetworkThreadRequest.Cleanup);
            _client.CleanupState();
        }
    }
}