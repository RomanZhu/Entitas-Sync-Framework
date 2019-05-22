using NetStack.Serialization;

public partial class Character : INetworkComponent
{
    public void Serialize(BitBuffer bitBuffer)
	{
		bitBuffer.AddUShort(1);

	}

	public void Deserialize(BitBuffer bitBuffer)
	{
	}
}