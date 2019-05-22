using Codegen.CodegenAttributes;

namespace Sources.CommandSchemes.ToClient
{
    [CommandToClient]
    public class ChatMessageScheme
    {
        public string Message;
        public ushort Sender;
    }
}