using NetStack.Serialization;

public interface INetworkComponent
{
    void Serialize(BitBuffer   bitbuffer);
    void Deserialize(BitBuffer bitBuffer);
}