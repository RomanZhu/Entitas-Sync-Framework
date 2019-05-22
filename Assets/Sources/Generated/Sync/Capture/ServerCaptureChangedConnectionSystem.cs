using System.Collections.Generic;
using Entitas;
using Sources.Networking.Server;


public class ServerCaptureChangedConnectionSystem : ReactiveSystem<GameEntity>
{
	private readonly ServerNetworkSystem _server;
	public ServerCaptureChangedConnectionSystem (Contexts contexts, Services services) : base(contexts.game)
	{
		_server = services.ServerSystem;
	}
		
	protected override ICollector<GameEntity> GetTrigger(IContext<GameEntity> context) {
		return context.CreateCollector(GameMatcher.Connection.Added());
	}
		
	protected override bool Filter(GameEntity entity)
	{
        return !entity.isDestroyed && entity.isWasSynced && entity.hasConnection;
	}

	protected override void Execute(List<GameEntity> entities) {
		if (_server.State != ServerState.Working) return;

		foreach (var e in entities) {
		    _server.ChangedComponents.AddUShort(e.id.Value);
            e.connection.Serialize(_server.ChangedComponents);
			_server.ChangedComponentsCount++;
		}
	}
}