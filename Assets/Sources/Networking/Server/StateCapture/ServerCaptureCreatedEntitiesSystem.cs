using System.Collections.Generic;
using Entitas;

namespace Sources.Networking.Server.StateCapture
{
    public class ServerCaptureCreatedEntitiesSystem : ReactiveSystem<GameEntity>
    {
        private readonly ServerNetworkSystem _server;

        public ServerCaptureCreatedEntitiesSystem(Contexts contexts, Services services) : base(contexts.game)
        {
            _server = services.ServerSystem;
        }

        protected override ICollector<GameEntity> GetTrigger(IContext<GameEntity> context)
        {
            return context.CreateCollector(GameMatcher.Sync.Added());
        }

        protected override bool Filter(GameEntity entity)
        {
            return !entity.isDestroyed && !entity.isWasSynced && entity.isSync;
        }

        protected override void Execute(List<GameEntity> entities)
        {
            if (_server.State != ServerState.Working) return;

            foreach (var e in entities)
            {
                PackEntityUtility.Pack(e, _server.CreatedEntities);

                e.isWasSynced = true;
                _server.CreatedEntitiesCount++;
            }
        }
    }
}