using Codegen.CodegenAttributes;
using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game]
[Sync]
public partial class Connection : IComponent
{
    [PrimaryEntityIndex] public ushort Id;

    public Connection()
    {
        Id = ushort.MaxValue;
    }
}