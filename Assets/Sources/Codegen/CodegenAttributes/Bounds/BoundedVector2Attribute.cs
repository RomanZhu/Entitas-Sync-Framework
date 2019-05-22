using System;

namespace Codegen.CodegenAttributes.Bounds
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BoundedVector2Attribute : Attribute
    {
        public BoundedVector2Attribute(float xMin, float xMax, float xPrecision, float yMin, float yMax,
            float                            yPrecision)
        {
            XMin       = xMin;
            XMax       = xMax;
            XPrecision = xPrecision;
            YMin       = yMin;
            YMax       = yMax;
            YPrecision = yPrecision;
        }

        public float XMin       { get; }
        public float XMax       { get; }
        public float XPrecision { get; }
        public float YMin       { get; }
        public float YMax       { get; }
        public float YPrecision { get; }
    }
}