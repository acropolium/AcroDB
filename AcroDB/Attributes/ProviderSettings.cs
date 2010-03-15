using System;

namespace AcroDB.Attributes
{
    public class ProviderSettingsAttribute : Attribute
    {
        public Type BaseEntityType
        {
            get; set;
        }
    }
}
