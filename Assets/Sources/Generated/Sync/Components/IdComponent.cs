using NetStack.Serialization;

public partial class IdComponent : INetworkComponent
{
    public void Serialize(BitBuffer bitBuffer)
	{
		bitBuffer.AddUShort(0);

		bitBuffer.AddUShort(Value); 
	}

	public void Deserialize(BitBuffer bitBuffer)
	{
		Value = bitBuffer.ReadUShort(); 
	}
}