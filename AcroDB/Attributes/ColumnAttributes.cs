using System;

namespace AcroDB.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AcroColumnIsPrimaryKeyAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AcroColumnIgnoreStorageAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AcroColumnLongStringAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AcroColumnStringLengthAttribute : Attribute
    {
        public AcroColumnStringLengthAttribute(int length)
        {
            Length = length;
        }

        public int Length
        {
            get; private set;
        }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AcroColumnNumericPrecisionAttribute : Attribute
    {
        public AcroColumnNumericPrecisionAttribute(int precision, int scale)
        {
            Precision = precision;
            Scale = scale;
        }

        public int Precision
        {
            get; private set;
        }
        public int Scale
        {
            get; private set;
        }
    }
}
