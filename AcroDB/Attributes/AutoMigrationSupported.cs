using System;
using AcroDB.AutoMigration;

namespace AcroDB.Attributes
{
    public class AutoMigrationSupportedAttribute : Attribute
    {
        public Type MigrationProvider
        {
            get; private set;
        }

        public string DbProviderName
        {
            get; private set;
        }

        public AutoMigrationSupportedAttribute(string dbProviderName) : this(dbProviderName, typeof(SubSonicMigrationProvider)) { }
        public AutoMigrationSupportedAttribute(string dbProviderName, Type migrationProvider)
        {
            DbProviderName = dbProviderName;
            MigrationProvider = migrationProvider;
        }
    }
}
