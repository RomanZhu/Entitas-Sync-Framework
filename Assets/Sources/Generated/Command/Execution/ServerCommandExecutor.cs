using NetStack.Serialization;
using Sources.Tools;

public static class ServerCommandExecutor
{
    public static void Execute(IServerHandler handler, BitBuffer buffer, ushort commandCount)
	{
		for (int i = 0; i < commandCount; i++)
        {
            var commandId = buffer.ReadUShort();
            switch (commandId)
            {
							
                case 0:
                {
					Logger.I.Log("ServerCommandExecutor", "Executing ClientChatMessageCommand");
                    var c = new  ClientChatMessageCommand();
                    c.Deserialize(buffer);
                    handler.HandleChatMessageCommand(ref c);
                    break;
                }
								
                case 1:
                {
					Logger.I.Log("ServerCommandExecutor", "Executing ClientRequestCharacterCommand");
                    var c = new  ClientRequestCharacterCommand();
                    c.Deserialize(buffer);
                    handler.HandleRequestCharacterCommand(ref c);
                    break;
                }
								
                case 2:
                {
					Logger.I.Log("ServerCommandExecutor", "Executing ClientSetTickrateCommand");
                    var c = new  ClientSetTickrateCommand();
                    c.Deserialize(buffer);
                    handler.HandleSetTickrateCommand(ref c);
                    break;
                }
				            }
        }
	}
}