using System.Collections.Generic;
using Entitas;
using Sources.Networking.Server;


public class ServerCaptureChangedIdSystem : ReactiveSystem<GameEntity>
{
	private readonly ServerNetworkSystem _server;
	public ServerCaptureChangedIdSystem (Contexts contexts, Services services) : base(contexts.game)
	{
		_server = services.ServerSystem;
	}
		
	protected override ICollector<GameEntity> GetTrigger(IContext<GameEntity> context) {
		return context.CreateCollector(GameMatcher.Id.Added());
	}
		
	protected override bool Filter(GameEntity entity)
	{
        return !entity.isDestroyed && entity.isWasSynced && entity.hasId;
	}

	protected override void Execute(List<GameEntity> entities) {
		if (_server.State != ServerState.Working) return;

		foreach (var e in entities) {
		    _server.ChangedComponents.AddUShort(e.id.Value);
            e.id.Serialize(_server.ChangedComponents);
			_server.ChangedComponentsCount++;
		}
	}
}