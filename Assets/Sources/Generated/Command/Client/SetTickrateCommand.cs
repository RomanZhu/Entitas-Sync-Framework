using NetStack.Serialization;

public struct ClientSetTickrateCommand : ICommand, IClientCommand
{
	public System.UInt16 Tickrate;
    public void Serialize(BitBuffer bitBuffer)
	{
		bitBuffer.AddUShort(2);

		bitBuffer.AddUShort(Tickrate); 
	}

	public void Deserialize(BitBuffer bitBuffer)
	{
		Tickrate = bitBuffer.ReadUShort(); 
	}
}