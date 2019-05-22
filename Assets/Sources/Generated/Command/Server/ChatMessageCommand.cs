using NetStack.Serialization;

public struct ServerChatMessageCommand : ICommand, IServerCommand
{
	public System.String Message;
	public System.UInt16 Sender;
    public void Serialize(BitBuffer bitBuffer)
	{
		bitBuffer.AddUShort(0);

		bitBuffer.AddString(Message); 
		bitBuffer.AddUShort(Sender); 
	}

	public void Deserialize(BitBuffer bitBuffer)
	{
		Message = bitBuffer.ReadString();
		Sender = bitBuffer.ReadUShort(); 
	}
}