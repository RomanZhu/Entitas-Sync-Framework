using Sources.Networking.Server.StateCapture;

public class ServerStateCaptureFeature : Feature
{
    public ServerStateCaptureFeature(Contexts contexts, Services services)
    {
        Add(new ServerCaptureChangedIdSystem(contexts, services));
        Add(new ServerCaptureRemovedIdSystem(contexts, services));
        Add(new ServerCaptureChangedCharacterSystem(contexts, services));
        Add(new ServerCaptureRemovedCharacterSystem(contexts, services));
        Add(new ServerCaptureChangedControlledBySystem(contexts, services));
        Add(new ServerCaptureRemovedControlledBySystem(contexts, services));
        Add(new ServerCaptureChangedConnectionSystem(contexts, services));
        Add(new ServerCaptureRemovedConnectionSystem(contexts, services));
        Add(new ServerCaptureChangedSyncSystem(contexts, services));
        Add(new ServerCaptureRemovedSyncSystem(contexts, services));

	    Add(new ServerCreateWorldStateSystem(contexts));
        Add(new ServerCaptureCreatedEntitiesSystem(contexts, services));
        Add(new ServerCaptureRemovedEntitiesSystem(contexts, services));
	}
}