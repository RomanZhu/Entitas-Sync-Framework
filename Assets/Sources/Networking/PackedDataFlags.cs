using System;

namespace Sources.Networking
{
    [Flags]
    public enum PackedDataFlags : byte
    {
        None = 0,
        Commands = 1,
        CreatedEntities = 2,
        RemovedEntities = 4,
        RemovedComponents = 8,
        ChangedComponents = 16
    }
}