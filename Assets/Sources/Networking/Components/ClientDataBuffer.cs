using Entitas;
using NetStack.Serialization;

[Game]
public class ClientDataBuffer : IComponent
{
    public ushort    CommandCount;
    public BitBuffer Value;
}