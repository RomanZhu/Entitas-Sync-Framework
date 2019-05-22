public class CommonGeneratedFeature : Feature
{
    public CommonGeneratedFeature(Contexts contexts)
    {
        Add(new GameEventSystems(contexts));
        Add(new GameCleanupSystems(contexts));
    }
}