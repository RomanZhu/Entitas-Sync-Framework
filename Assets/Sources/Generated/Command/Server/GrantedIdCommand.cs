using NetStack.Serialization;

public struct ServerGrantedIdCommand : ICommand, IServerCommand
{
	public System.UInt16 Id;
    public void Serialize(BitBuffer bitBuffer)
	{
		bitBuffer.AddUShort(1);

		bitBuffer.AddUShort(Id); 
	}

	public void Deserialize(BitBuffer bitBuffer)
	{
		Id = bitBuffer.ReadUShort(); 
	}
}