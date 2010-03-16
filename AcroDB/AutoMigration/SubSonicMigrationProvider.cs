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
        public void Migrate(string connectionString, string providerName, IEnumerable<Type> entities)
        {
            var provider = ProviderFactory.GetProvider(connectionString, providerName);
            foreach (var entity in entities)
            {
                var batch = new BatchQuery(provider);
                var m = new Migrator(Assembly.GetExecutingAssembly());
                var commands = m.MigrateFromModel(entity, provider);
                foreach (var s in commands)
                    batch.QueueForTransaction(new QueryCommand(s, provider));
                batch.ExecuteTransaction();
            }
        }
    }
}
