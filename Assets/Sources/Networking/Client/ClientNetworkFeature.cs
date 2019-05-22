namespace Sources.Networking.Client
{
    public class ClientNetworkFeature : Feature
    {
        public ClientNetworkFeature(Contexts contexts, Services services)
        {
            Add(new ClientSendPacketSystem(services));
        }
    }
}