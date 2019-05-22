using System.Collections.Generic;
using Entitas;

namespace Sources.Networking.Server.StateCapture
{
    public class ServerCaptureRemovedEntitiesSystem : ReactiveSystem<GameEntity>
    {
        private readonly ServerNetworkSystem _server;

        public ServerCaptureRemovedEntitiesSystem(Contexts contexts, Services services) : base(contexts.game)
        {
            _server = services.ServerSystem;
        }

        protected override ICollector<GameEntity> GetTrigger(IContext<GameEntity> context)
        {
            return context.CreateCollector(GameMatcher.Destroyed.Added());
        }

        protected override bool Filter(GameEntity entity)
        {
            return entity.isDestroyed && entity.isWasSynced;
        }

        protected override void Execute(List<GameEntity> entities)
        {
            if (_server.State != ServerState.Working) return;

            foreach (var e in entities)
            {
                _server.RemovedEntities.AddUShort(e.id.Value);
                _server.RemovedEntitiesCount++;
            }
        }
    }
}