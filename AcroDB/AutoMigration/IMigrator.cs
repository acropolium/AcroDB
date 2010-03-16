using System;
using System.Collections.Generic;

namespace AcroDB.AutoMigration
{
    public interface IMigrator
    {
        void Migrate(string connectionString, string providerName, IEnumerable<Type> entities);
    }
}
