using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Entitas;

namespace Sources.Networking.Server
{
    public class ServerSendPacketsSystem : IExecuteSystem
    {
        private readonly List<GameEntity>   _connectionsBuffer = new List<GameEntity>(ServerNetworkSystem.MaxPlayers);
        private readonly IGroup<GameEntity> _connectionsGroup;

        private readonly byte[]      _data = new byte[2048];
        private readonly GameContext _game;

        private readonly ServerNetworkSystem _server;

        public ServerSendPacketsSystem(Contexts contexts, Services services)
        {
            _game             = contexts.game;
            _connectionsGroup = _game.GetGroup(GameMatcher.Connection);

            _server = services.ServerSystem;
        }

        public void Execute()
        {
            if (_server.State != ServerState.Working) return;

            var createdEntitiesLength   = _server.CreatedEntities.Length;
            var changedComponentsLength = _server.ChangedComponents.Length;
            var removedEntitiesLength   = _server.RemovedEntities.Length;
            var removedComponentsLength = _server.RemovedComponents.Length;

            _connectionsGroup.GetEntities(_connectionsBuffer);
            foreach (var e in _connectionsBuffer)
                unsafe
                {
                    if (e.isDestroyed) continue;

                    if (e.isRequiresWorldState)
                    {
                        var cb            = e.clientDataBuffer;
                        var commandLength = cb.Value.Length;

                        var commandCount = cb.CommandCount;
                        fixed (byte* destination = &_data[0])
                        {
                            var shortsSpan = new Span<ushort>(destination, 2);
                            shortsSpan[0] = commandCount;
                            shortsSpan[1] = (ushort) commandLength;
                        }

                        var offset = 4;
                        if (commandCount > 0)
                        {
                            cb.Value.ToArray(_data, offset);
                            offset += commandLength;

                            cb.Value.Clear();
                            cb.CommandCount = 0;
                        }

                        fixed (byte* destination = &_data[offset])
                        {
                            var shortsSpan = new Span<ushort>(destination, 2);
                            shortsSpan[0] = _game.worldState.EntityCount;
                            shortsSpan[1] = (ushort) _game.worldState.Buffer.Length;
                        }

                        offset += 4;

                        _game.worldState.Buffer.ToArray(_data, offset);
                        offset += _game.worldState.Buffer.Length;

                        var newPtr = Marshal.AllocHGlobal(offset);
                        fixed (byte* source = &_data[0])
                        {
                            Buffer.MemoryCopy(source, newPtr.ToPointer(), offset, offset);
                        }

                        _server.EnqueueSendData(new SendData
                            {Data = newPtr, Length = offset, Peer = e.connectionPeer.Value});
                        e.isRequiresWorldState = false;
                    }
                    else
                    {
                        var cb            = e.clientDataBuffer;
                        var commandLength = cb.Value.Length;

                        var commandCount           = cb.CommandCount;
                        var createdEntitiesCount   = _server.CreatedEntitiesCount;
                        var removedEntitiesCount   = _server.RemovedEntitiesCount;
                        var removedComponentsCount = _server.RemovedComponentsCount;
                        var changedComponentsCount = _server.ChangedComponentsCount;

                        var flags = PackedDataFlags.None;
                        
                        if (commandCount > 0)
                            flags |= PackedDataFlags.Commands;
                        if (createdEntitiesCount > 0)
                            flags |= PackedDataFlags.CreatedEntities;
                        if (removedEntitiesCount > 0)
                            flags |= PackedDataFlags.RemovedEntities;
                        if (removedComponentsCount > 0)
                            flags |= PackedDataFlags.RemovedComponents;
                        if (changedComponentsCount > 0)
                            flags |= PackedDataFlags.ChangedComponents;

                        var offset = 1;
                        _data[0] = (byte) flags;

                        #region commands
                        
                        if (commandCount > 0)
                        {
                            fixed (byte* destination = &_data[offset])
                            {
                                var shortsSpan = new Span<ushort>(destination, 2);
                                shortsSpan[0] = commandCount;
                                shortsSpan[1] = (ushort) commandLength;
                            }

                            offset += 4;
                            cb.Value.ToArray(_data, offset);
                            offset += commandLength;

                            cb.Value.Clear();
                            cb.CommandCount = 0;
                        }

                        #endregion

                        #region created entities

                        if (createdEntitiesCount > 0)
                        {
                            fixed (byte* destination = &_data[offset])
                            {
                                var shortsSpan = new Span<ushort>(destination, 2);
                                shortsSpan[0] = createdEntitiesCount;
                                shortsSpan[1] = (ushort) createdEntitiesLength;
                            }

                            offset += 4;
                            _server.CreatedEntities.ToArray(_data, offset);
                            offset += createdEntitiesLength;
                        }

                        #endregion

                        #region removed entities

                        if (removedEntitiesCount > 0)
                        {
                            fixed (byte* destination = &_data[offset])
                            {
                                var shortsSpan = new Span<ushort>(destination, 2);
                                shortsSpan[0] = removedEntitiesCount;
                                shortsSpan[1] = (ushort) removedEntitiesLength;
                            }

                            offset += 4;
                            _server.RemovedEntities.ToArray(_data, offset);
                            offset += removedEntitiesLength;
                        }

                        #endregion

                        #region removed components

                        if (removedComponentsCount > 0)
                        {
                            fixed (byte* destination = &_data[offset])
                            {
                                var shortsSpan = new Span<ushort>(destination, 2);
                                shortsSpan[0] = removedComponentsCount;
                                shortsSpan[1] = (ushort) removedComponentsLength;
                            }

                            offset += 4;
                            _server.RemovedComponents.ToArray(_data, offset);
                            offset += removedComponentsLength;
                        }

                        #endregion

                        #region changed components

                        if (changedComponentsCount > 0)
                        {
                            fixed (byte* destination = &_data[offset])
                            {
                                var shortsSpan = new Span<ushort>(destination, 2);
                                shortsSpan[0] = changedComponentsCount;
                                shortsSpan[1] = (ushort) changedComponentsLength;
                            }

                            offset += 4;
                            _server.ChangedComponents.ToArray(_data, offset);
                            offset += changedComponentsLength;
                        }

                        #endregion

                        var newPtr = Marshal.AllocHGlobal(offset);
                        fixed (byte* source = &_data[0])
                        {
                            Buffer.MemoryCopy(source, newPtr.ToPointer(), offset, offset);
                        }

                        _server.EnqueueSendData(new SendData
                            {Data = newPtr, Length = offset, Peer = e.connectionPeer.Value});
                    }
                }

            _server.ClearBuffers();
        }
    }
}