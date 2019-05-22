namespace Sources.Networking.Server
{
    public class ServerNetworkFeature : Feature
    {
        public ServerNetworkFeature(Contexts contexts, Services services)
        {
            Add(new ServerStateCaptureFeature(contexts, services));
            Add(new ServerSendPacketsSystem(contexts, services));
        }
    }
}