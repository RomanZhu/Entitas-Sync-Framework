using Codegen.CodegenAttributes;
using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game]
[Sync]
public partial class ControlledBy : IComponent
{
    [PrimaryEntityIndex] public ushort Value;
}