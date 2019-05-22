using NetStack.Serialization;
using Sources.Tools;

public static class ClientCommandExecutor
{
    public static void Execute(IClientHandler handler, BitBuffer buffer, ushort commandCount)
	{
		for (int i = 0; i < commandCount; i++)
        {
            var commandId = buffer.ReadUShort();
            switch (commandId)
            {
							
                case 0:
                {
					Logger.I.Log("ClientCommandExecutor", "Executing ServerChatMessageCommand");
                    var c = new  ServerChatMessageCommand();
                    c.Deserialize(buffer);
                    handler.HandleChatMessageCommand(ref c);
                    break;
                }
								
                case 1:
                {
					Logger.I.Log("ClientCommandExecutor", "Executing ServerGrantedIdCommand");
                    var c = new  ServerGrantedIdCommand();
                    c.Deserialize(buffer);
                    handler.HandleGrantedIdCommand(ref c);
                    break;
                }
								
                case 2:
                {
					Logger.I.Log("ClientCommandExecutor", "Executing ServerSetTickrateCommand");
                    var c = new  ServerSetTickrateCommand();
                    c.Deserialize(buffer);
                    handler.HandleSetTickrateCommand(ref c);
                    break;
                }
				            }
        }
	}
}