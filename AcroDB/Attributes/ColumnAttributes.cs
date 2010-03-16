using System;

namespace AcroDB.Attributes
{
    public class AcroColumnIsPrimaryKeyAttribute : Attribute { }
    public class AcroColumnIgnoreStorageAttribute : Attribute { }
    public class AcroColumnLongStringAttribute : Attribute { }
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
