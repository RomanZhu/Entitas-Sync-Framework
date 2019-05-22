using Sources.Networking.Server;

public class ServerFeature : Feature
{
    public ServerFeature(Contexts contexts, Services services)
    {
        Add(new CommonGameplayFeature(contexts, services));
        Add(new ServerGameplayFeature(contexts, services));

        Add(new ServerNetworkFeature(contexts, services));
        Add(new CommonGeneratedFeature(contexts));
    }
}