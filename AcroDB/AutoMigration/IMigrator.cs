using System;
using System.Collections.Generic;

namespace AcroDB.AutoMigration
{
    public interface IMigrator
    {
        bool Migrate(string connectionString, string providerName, IEnumerable<Type> entities, Action<MigratorResult, Type> callbackOnChanges);
    }

    public enum MigratorResult
    {
        NoChanges,
        ChangesMadeToEntity,
        ChangesMadeToAllEntities
    }
}
