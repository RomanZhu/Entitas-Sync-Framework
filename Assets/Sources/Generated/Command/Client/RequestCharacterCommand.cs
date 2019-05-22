using NetStack.Serialization;

public struct ClientRequestCharacterCommand : ICommand, IClientCommand
{
    public void Serialize(BitBuffer bitBuffer)
	{
		bitBuffer.AddUShort(1);

	}

	public void Deserialize(BitBuffer bitBuffer)
	{
	}
}