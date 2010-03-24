using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SubSonic.DataProviders;
using SubSonic.Query;
using SubSonic.Schema;

namespace AcroDB.AutoMigration
{
    public class SubSonicMigrationProvider : IMigrator
    {
        private static string PrepareCommand(string s)
        {
            return s.Replace(" Order ", " [Order] ");
        }

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
                try
                {
                    foreach (var s in commands)
                        batch.QueueForTransaction(new QueryCommand(PrepareCommand(s), provider));
                    batch.ExecuteTransaction();
                }
                catch(Exception ex)
                {
                    var sb = new StringBuilder(ex.Message);
                    sb.Append(": ");
                    foreach (var command in commands)
                        sb.Append(command);
                    throw new Exception(sb.ToString(), ex);
                }
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
