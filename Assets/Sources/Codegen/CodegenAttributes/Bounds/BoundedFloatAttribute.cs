using System;

namespace Codegen.CodegenAttributes.Bounds
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BoundedFloatAttribute : Attribute
    {
        public BoundedFloatAttribute(float min, float max, float precision)
        {
            Min       = min;
            Max       = max;
            Precision = precision;
        }

        public float Min       { get; }
        public float Max       { get; }
        public float Precision { get; }
    }
}