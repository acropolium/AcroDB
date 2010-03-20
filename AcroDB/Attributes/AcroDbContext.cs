using System;
using AcroDB.AutoMigration;

namespace AcroDB.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class AcroDbContextAttribute : Attribute
    {
        public Type DataProvider
        {
            get; private set;
        }
        public string UniqueName
        {
            get; private set;
        }
        public string LowLevelConnectionProviderType
        {
            get; private set;
        }
        public bool AllowAutoMigration
        {
            get; private set;
        }
        public Type CustomAutoMigrationProvider
        {
            get; private set;
        }

        public AcroDbContextAttribute(string uniqueName, Type dataProvider)
            : this(uniqueName, dataProvider, null, false, null) { }
        public AcroDbContextAttribute(string uniqueName, Type dataProvider, string connectionProvider)
            : this(uniqueName, dataProvider, connectionProvider, false, null) { }
        public AcroDbContextAttribute(string uniqueName, Type dataProvider, bool allowAutoMigration)
            : this(uniqueName, dataProvider, null, allowAutoMigration, null) { }
        public AcroDbContextAttribute(string uniqueName, Type dataProvider, bool allowAutoMigration, Type customMigrationProvider)
            : this(uniqueName, dataProvider, null, allowAutoMigration, customMigrationProvider) { }
        public AcroDbContextAttribute(string uniqueName, Type dataProvider, string connectionProvider, bool allowAutoMigration)
            : this(uniqueName, dataProvider, connectionProvider, allowAutoMigration, null) { }

        public AcroDbContextAttribute(string uniqueName, Type dataProvider, string connectionProvider, bool allowAutoMigration, Type customMigrationProvider)
        {
            UniqueName = uniqueName;
            DataProvider = dataProvider;
            LowLevelConnectionProviderType = connectionProvider;
            AllowAutoMigration = allowAutoMigration;
            CustomAutoMigrationProvider = customMigrationProvider ?? typeof (SubSonicMigrationProvider);
        }
    }
}
