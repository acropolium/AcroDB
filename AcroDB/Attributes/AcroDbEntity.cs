using System;

namespace AcroDB.Attributes
{
    public class AcroDbEntityAttribute : Attribute
    {
        public string Name
        {
            get; set;
        }

        public Type ParentType
        {
            get; set;
        }

        public Type[] InterfacesToImplement
        {
            get; set;
        }

        public Type CustomDataContext
        {
            get; set;
        }

        public Type CustomDataProvider
        {
            get; set;
        }

        public Type CustomEntity
        {
            get; set;
        }

        public AcroDbEntityAttribute()
        {
            Name = String.Empty;
            CustomDataContext = null;
            CustomEntity = null;
            CustomDataProvider = null;
            ParentType = typeof(AcroEntity);
            InterfacesToImplement = new Type[0];
        }
    }
}
