using System;

namespace Codegen.CodegenAttributes.Bounds
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BoundedVector3Attribute : Attribute
    {
        public BoundedVector3Attribute(float xMin,       float xMax, float xPrecision, float yMin, float yMax,
            float                            yPrecision, float zMin, float zMax,       float zPrecision)
        {
            XMin       = xMin;
            XMax       = xMax;
            XPrecision = xPrecision;
            YMin       = yMin;
            YMax       = yMax;
            YPrecision = yPrecision;
            ZMin       = zMin;
            ZMax       = zMax;
            ZPrecision = zPrecision;
        }

        public float XMin       { get; }
        public float XMax       { get; }
        public float XPrecision { get; }
        public float YMin       { get; }
        public float YMax       { get; }
        public float YPrecision { get; }
        public float ZMin       { get; }
        public float ZMax       { get; }
        public float ZPrecision { get; }
    }
}