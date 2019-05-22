using System.Collections.Generic;
using Entitas;
using NetStack.Serialization;
using Sources.Tools;

namespace Sources.Networking.Server.StateCapture
{
    public class ServerCreateWorldStateSystem : ReactiveSystem<GameEntity>
    {
        private readonly BitBuffer        _buffer = new BitBuffer(512);
        private readonly GameContext      _game;
        private readonly List<GameEntity> _syncBuffer = new List<GameEntity>(256);

        private readonly IGroup<GameEntity> _syncGroup;

        public ServerCreateWorldStateSystem(Contexts contexts) : base(contexts.game)
        {
            _game      = contexts.game;
            _syncGroup = _game.GetGroup(GameMatcher.Sync);
        }

        protected override ICollector<GameEntity> GetTrigger(IContext<GameEntity> context)
        {
            return context.CreateCollector(GameMatcher.RequiresWorldState.Added());
        }

        protected override bool Filter(GameEntity entity)
        {
            return entity.isRequiresWorldState;
        }

        protected override void Execute(List<GameEntity> entities)
        {
            Logger.I.Log(this, "Creating world snapshot.");

            var e = _game.CreateEntity();
            e.AddWorldState(0, _buffer);
            e.isDestroyed = true;

            _buffer.Clear();
            _syncGroup.GetEntities(_syncBuffer);

            foreach (var entity in _syncBuffer)
                if (!entity.isDestroyed)
                {
                    e.worldState.EntityCount++;
                    PackEntityUtility.Pack(entity, _buffer);
                }
        }
    }
}