using Codegen.CodegenAttributes;
using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game]
[Sync]
public partial class IdComponent : IComponent
{
    [PrimaryEntityIndex] public ushort Value;

    public IdComponent()
    {
        Value = ushort.MaxValue;
    }
}