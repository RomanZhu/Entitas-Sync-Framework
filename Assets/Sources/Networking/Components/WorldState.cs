using Entitas;
using Entitas.CodeGeneration.Attributes;
using NetStack.Serialization;

[Game]
[Unique]
public class WorldState : IComponent
{
    public ushort    EntityCount;
    public BitBuffer Buffer;
}