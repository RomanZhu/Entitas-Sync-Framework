public interface IClientHandler
{
	void HandleChatMessageCommand(ref ServerChatMessageCommand command);
	void HandleGrantedIdCommand(ref ServerGrantedIdCommand command);
	void HandleSetTickrateCommand(ref ServerSetTickrateCommand command);
}

public interface IClientCommand{}