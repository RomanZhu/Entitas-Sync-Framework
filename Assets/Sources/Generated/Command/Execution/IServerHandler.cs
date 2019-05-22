public interface IServerHandler
{
	void HandleChatMessageCommand(ref ClientChatMessageCommand command);
	void HandleRequestCharacterCommand(ref ClientRequestCharacterCommand command);
	void HandleSetTickrateCommand(ref ClientSetTickrateCommand command);
}

public interface IServerCommand{}