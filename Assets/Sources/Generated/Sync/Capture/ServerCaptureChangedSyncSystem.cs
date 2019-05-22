using System.Collections.Generic;
using Entitas;
using Sources.Networking.Server;


public class ServerCaptureChangedSyncSystem : ReactiveSystem<GameEntity>
{
	private readonly ServerNetworkSystem _server;
	public ServerCaptureChangedSyncSystem (Contexts contexts, Services services) : base(contexts.game)
	{
		_server = services.ServerSystem;
	}
		
	protected override ICollector<GameEntity> GetTrigger(IContext<GameEntity> context) {
		return context.CreateCollector(GameMatcher.Sync.Added());
	}
		
	protected override bool Filter(GameEntity entity)
	{
        return !entity.isDestroyed && entity.isWasSynced && entity.isSync;
	}

	protected override void Execute(List<GameEntity> entities) {
		if (_server.State != ServerState.Working) return;

		foreach (var e in entities) {
		    _server.ChangedComponents.AddUShort(e.id.Value);
            _server.ChangedComponents.AddUShort(4);
			_server.ChangedComponentsCount++;
		}
	}
}