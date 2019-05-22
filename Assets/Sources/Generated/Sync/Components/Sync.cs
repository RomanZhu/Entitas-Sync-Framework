using NetStack.Serialization;

public partial class Sync : INetworkComponent
{
    public void Serialize(BitBuffer bitBuffer)
	{
		bitBuffer.AddUShort(4);

	}

	public void Deserialize(BitBuffer bitBuffer)
	{
	}
}