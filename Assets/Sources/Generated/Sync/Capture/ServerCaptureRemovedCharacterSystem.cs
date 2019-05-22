using System.Collections.Generic;
using Entitas;
using Sources.Networking.Server;

public class ServerCaptureRemovedCharacterSystem : ReactiveSystem<GameEntity>
{
	private readonly ServerNetworkSystem _server;
	public ServerCaptureRemovedCharacterSystem (Contexts contexts, Services services) : base(contexts.game)
	{
		_server = services.ServerSystem;
	}
		
	protected override ICollector<GameEntity> GetTrigger(IContext<GameEntity> context) {
		return context.CreateCollector(GameMatcher.Character.Removed());
	}
		
	protected override bool Filter(GameEntity entity)
	{
		return !entity.isDestroyed && entity.isWasSynced && entity.isCharacter;
	}

	protected override void Execute(List<GameEntity> entities) {
		if (_server.State != ServerState.Working) return;

        foreach (var e in entities) {
		    _server.RemovedComponents.AddUShort(e.id.Value);
			_server.RemovedComponents.AddUShort(1);
		    _server.RemovedComponentsCount++;
		}
	}
}