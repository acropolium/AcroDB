using System;
using System.Collections.Generic;

namespace AcroDB.AutoMigration
{
    public interface IMigrator
    {
        bool Migrate(string connectionString, string providerName, IEnumerable<Type> entities, Action<MigratorResult, Type> callbackOnChanges);
    }

    public interface IMigrationTrack
    {
        void MigrationChange(MigratorResult result, Type @interface);
    }

    [Flags]
    public enum MigratorResult
    {
        NoChanges = 0x0,
        ChangesMadeToEntity = 0x1,
        ChangesMadeToAllEntities = 0x2
    }
}
