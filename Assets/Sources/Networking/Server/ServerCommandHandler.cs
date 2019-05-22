using System.Collections.Generic;
using ENet;
using Entitas;
using NetStack.Serialization;
using UnityEngine;
using Logger = Sources.Tools.Logger;

namespace Sources.Networking.Server
{
    public class ServerCommandHandler : IServerHandler
    {
        private readonly List<GameEntity> _connectionsBuffer = new List<GameEntity>(ServerNetworkSystem.MaxPlayers);

        private readonly IGroup<GameEntity> _connectionsGroup;

        private readonly GameContext         _game;
        private readonly ServerNetworkSystem _server;
        public           ushort              CurrentClientId;

        public ServerCommandHandler(GameContext game, ServerNetworkSystem server)
        {
            _game             = game;
            _server           = server;
            _connectionsGroup = _game.GetGroup(GameMatcher.Connection);
        }

        public void HandleChatMessageCommand(ref ClientChatMessageCommand command)
        {
            Logger.I.Log(this, $"Client-{CurrentClientId}: {command.Message}");
            _server.EnqueueCommandForEveryone(new ServerChatMessageCommand
                {Message = command.Message, Sender = CurrentClientId});
        }

        public void HandleRequestCharacterCommand(ref ClientRequestCharacterCommand command)
        {
            var e = _game.GetEntityWithControlledBy(CurrentClientId);
            if (e == null)
            {
                e        = _game.CreateEntity();
                e.isSync = true;
                e.AddControlledBy(CurrentClientId);
            }
            else
            {
                _server.EnqueueCommandForClient(CurrentClientId,
                    new ServerChatMessageCommand {Message = "You already have one.", Sender = 0});
            }
        }

        public void HandleSetTickrateCommand(ref ClientSetTickrateCommand command)
        {
            _server.TickRate = command.Tickrate;
            _server.EnqueueCommandForEveryone(new ServerSetTickrateCommand {Tickrate = command.Tickrate});
        }

        public void OnClientConnected(Peer peer)
        {
            Logger.I.Log(this, $"Client connected - {peer.ID}");

            if (_connectionsGroup.count == ServerNetworkSystem.MaxPlayers)
            {
                _server.EnqueueDisconnectData(new DisconnectData {Peer = peer});
                return;
            }

            var id = (ushort) peer.ID;
            var e  = _game.CreateEntity();
            e.isSync = true;
            e.AddConnectionPeer(peer);
            e.AddConnection(id);
            e.AddClientDataBuffer(0, new BitBuffer(64));
            e.isRequiresWorldState = true;

            _server.EnqueueCommandForClient(id, new ServerGrantedIdCommand {Id         = id});
            _server.EnqueueCommandForClient(id, new ServerSetTickrateCommand {Tickrate = _server.TickRate});
        }

        public void OnClientDisconnected(Peer peer)
        {
            Logger.I.Log(this, $"Client disconnected - {peer.ID}");

            var e = _game.GetEntityWithConnection((ushort) peer.ID);

            if (e != null) e.isDestroyed = true;
        }
    }
}