using System.Collections.Generic;
using Entitas;
using Sources.Networking.Server;

public class ServerCaptureRemovedIdSystem : ReactiveSystem<GameEntity>
{
	private readonly ServerNetworkSystem _server;
	public ServerCaptureRemovedIdSystem (Contexts contexts, Services services) : base(contexts.game)
	{
		_server = services.ServerSystem;
	}
		
	protected override ICollector<GameEntity> GetTrigger(IContext<GameEntity> context) {
		return context.CreateCollector(GameMatcher.Id.Removed());
	}
		
	protected override bool Filter(GameEntity entity)
	{
        return !entity.isDestroyed && entity.isWasSynced && !entity.hasId;
	}

	protected override void Execute(List<GameEntity> entities) {
		if (_server.State != ServerState.Working) return;

        foreach (var e in entities) {
		    _server.RemovedComponents.AddUShort(e.id.Value);
			_server.RemovedComponents.AddUShort(0);
		    _server.RemovedComponentsCount++;
		}
	}
}