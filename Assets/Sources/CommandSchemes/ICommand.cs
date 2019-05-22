using NetStack.Serialization;

public interface ICommand
{
    void Serialize(BitBuffer   bitBuffer);
    void Deserialize(BitBuffer bitBuffer);
}