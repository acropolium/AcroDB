using System;
using System.Reflection;
using SubSonic.SqlGeneration.Schema;

namespace AcroDB
{
    public abstract class AcroEntity : IIdEntity
    {
        [SubSonicIgnore]
        public IDataContextProvider DataContextProvider { get; set; }

        private PropertyInfo _idPropertyInfo;
        private PropertyInfo IdPropertyInfo
        {
            get
            {
                return _idPropertyInfo ??
                       (_idPropertyInfo = GetType().GetProperty("ID", BindingFlags.Public | BindingFlags.Instance));
            }
        }

        public Guid GetEntityId()
        {
            return (Guid)(IdPropertyInfo.GetValue(this, null));
        }

        public void SetEntityId(Guid id)
        {
            IdPropertyInfo.SetValue(this, id, null);
        }

        protected void SetNullableItem<TType>(string propertyName, string dbName, TType? val) where TType : struct
        {
            //SendPropertyChanging(propertyName);
            GetType().GetField(dbName, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, val);
            //SendPropertyChanged(propertyName);
        }
    }
}
