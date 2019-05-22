using NetStack.Serialization;

public struct ClientChatMessageCommand : ICommand, IClientCommand
{
	public System.String Message;
    public void Serialize(BitBuffer bitBuffer)
	{
		bitBuffer.AddUShort(0);

		bitBuffer.AddString(Message); 
	}

	public void Deserialize(BitBuffer bitBuffer)
	{
		Message = bitBuffer.ReadString();
	}
}