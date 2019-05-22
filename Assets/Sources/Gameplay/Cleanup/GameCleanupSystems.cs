public sealed class GameCleanupSystems : Feature {

    public GameCleanupSystems(Contexts contexts) {
        Add(new DestroyDestroyedGameSystem(contexts));
    }
}