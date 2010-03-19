using System;
using System.Collections.Generic;
using System.Reflection;
using SubSonic.DataProviders;
using SubSonic.Query;
using SubSonic.Schema;

namespace AcroDB.AutoMigration
{
    public class SubSonicMigrationProvider : IMigrator
    {
        public bool Migrate(string connectionString, string providerName, IEnumerable<Type> entities, Action<MigratorResult, Type> callbackOnChanges)
        {
            var bResult = false;
            var provider = ProviderFactory.GetProvider(connectionString, providerName);
            foreach (var entity in entities)
            {
                var batch = new BatchQuery(provider);
                var m = new Migrator(Assembly.GetExecutingAssembly());
                var commands = m.MigrateFromModel(entity, provider);
                bResult |= commands.Length > 0;
                foreach (var s in commands)
                    batch.QueueForTransaction(new QueryCommand(s, provider));
                batch.ExecuteTransaction();
                if (callbackOnChanges == null) continue;
                callbackOnChanges(commands.Length > 0 ? MigratorResult.ChangesMadeToEntity : MigratorResult.NoChanges,
                                  entity);
            }
            if (callbackOnChanges != null)
                callbackOnChanges(MigratorResult.ChangesMadeToAllEntities, null);
            return bResult;
        }
    }
}
