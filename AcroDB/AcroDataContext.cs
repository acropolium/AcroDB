using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AcroDB.Attributes;
using AcroDB.AutoMigration;
using AcroDB.EntityFactory;

namespace AcroDB
{
    public interface IDataContextProvider
    {
        IEntityDataProvider<TEntityInterface> Provide<TEntityInterface>() where TEntityInterface : class;
        void SubmitChanges();
    }

    public class AcroDataContext : IDataContextProvider, IDisposable
    {
        private readonly IDataContext _currentDataContext;
        private readonly Dictionary<Type, object> _entitiyProviders = new Dictionary<Type, object>();
        //private readonly string _lockerObject;
        private readonly object _lockerObject = new object();

        ///<summary>
        ///
        ///</summary>
        ///<param name="currentDataContext"></param>
        public AcroDataContext(IDataContext currentDataContext)
        {
            _currentDataContext = currentDataContext;
            //_lockerObject = String.Format("AcroDataContext{0}{1}", Math.Abs(DateTime.Now.ToFileTime()), Math.Abs((new Random()).Next()));
        }

        ///<summary>
        ///
        ///</summary>
        public AcroDataContext() : this(CreateDefaultDataContext()) { }

        public static AcroDataContext Go
        {
            get
            {
                return new AcroDataContext();
            }
        }

        private static IDataContext CreateDefaultDataContext()
        {
            return (IDataContext) DefaultDataContext.DataContext
                                      .GetConstructor(new[] {typeof (string[])})
                                      .Invoke(new object[] {DefaultDataContext.DefaultParameters});
        }

        public static IDataContextDescription DefaultDataContext;

        ///<summary>
        ///
        ///</summary>
        public IDataContext CurrentDataContext
        {
            get { return _currentDataContext; }
        }

        private struct EntityInterfaceTypesDescription
        {
            public Type Entity { get; set; }
            public Type Provider { get; set; }
            public Type DataContext { get; set; }
        }

        private static readonly object EntityTypeLock = new object();
        private static readonly IDictionary<Type, EntityInterfaceTypesDescription> EntityDescription = new Dictionary<Type, EntityInterfaceTypesDescription>();
        public static void RegisterEntityInterface(Type interfaceType)
        {
            lock (EntityTypeLock)
            {
                if (EntityDescription.ContainsKey(interfaceType))
                    return;
                var attr = interfaceType.GetCustomAttributes(typeof(AcroDbEntityAttribute), true).FirstOrDefault() as AcroDbEntityAttribute;
                if (attr == null)
                    return;
                var providerSettings =
                    (ProviderSettingsAttribute)(attr.CustomDataProvider ?? DefaultDataContext.DataProvider).GetCustomAttributes(
                        typeof (ProviderSettingsAttribute), true).FirstOrDefault();
                var basetype = providerSettings == null ? null : providerSettings.BaseEntityType;
                EntityDescription[interfaceType] = new EntityInterfaceTypesDescription
                                                       {
                                                           Entity = attr.CustomEntity ?? EntityBuilder.Build(interfaceType, basetype, attr.Name, null),
                                                           DataContext = attr.CustomDataContext,
                                                           Provider = attr.CustomDataProvider
                                                       };
            }
        }

        private static EntityInterfaceTypesDescription? GetEntityDescription<TEntityInterface>()
        {
            EntityInterfaceTypesDescription container;
            return EntityDescription.TryGetValue(typeof(TEntityInterface), out container) ? container : (EntityInterfaceTypesDescription?) null;
        }

        public static void ResetEntityMaps()
        {
            lock (EntityTypeLock)
            {
                EntityDescription.Clear();
            }
        }

        public static IEnumerable<KeyValuePair<Type, Type>> GetInterfacesTypes<TDataContextType>()
        {
            return GetInterfacesTypes(typeof (TDataContextType));
        }

        public static IEnumerable<KeyValuePair<Type, Type>> GetInterfacesTypes<TDataContextType>(bool skipGenericTypes)
        {
            return GetInterfacesTypes(typeof (TDataContextType), skipGenericTypes);
        }

        public static Type MapInterface<TInterface>()
        {
            var desc = GetEntityDescription<TInterface>();
            return desc == null ? null : desc.Value.Entity;
        }

        public static IEnumerable<KeyValuePair<Type, Type>> GetInterfacesTypes(Type contextType)
        {
            return GetInterfacesTypes(contextType, false);
        }

        public static IEnumerable<KeyValuePair<Type, Type>> GetInterfacesTypes(Type contextType, bool skipGenericTypes)
        {
            var enu = from description in EntityDescription
                      where
                          (description.Value.DataContext == null && DefaultDataContext.DataContext == contextType) ||
                          (description.Value.DataContext != null && description.Value.DataContext == contextType)
                      select new KeyValuePair<Type, Type>(description.Key, description.Value.Entity);
            if (skipGenericTypes)
                enu = enu.Where(i => !i.Key.GetCustomAttributes(typeof (AcroGenericEntityAttribute), true).Any());
            return enu;
        }

        public static void ScanAssemblyForEntities(Assembly asm)
        {
            foreach (var entityInterface in asm.GetTypes().Where(t => t.IsInterface && t.GetCustomAttributes(typeof(AcroDbEntityAttribute), true).Length > 0))
                RegisterEntityInterface(entityInterface);
        }

        public static void PerformMigrations()
        {
            PerformMigrations(null);
        }

        private class MigrationAutoCallBack
        {
            private readonly IList<KeyValuePair<AcroDbMigrationAttribute, Type>> _types;

            public MigrationAutoCallBack(IEnumerable<Type> interfaces)
            {
                _types = (from i in interfaces
                          where i.GetCustomAttributes(typeof (AcroDbMigrationAttribute), true).Any()
                          select
                              new KeyValuePair<AcroDbMigrationAttribute, Type>(
                              (AcroDbMigrationAttribute)
                              i.GetCustomAttributes(typeof (AcroDbMigrationAttribute), true).First(), i)).ToList();
            }

            internal void CallBack(MigratorResult migratorResult, Type @interface)
            {
                var temp = @interface == null ? _types : _types.Where(i => i.Value == @interface);
                var events = temp.Where(e => (e.Key.WhenToFire & migratorResult) == migratorResult && (e.Value != null)).Select(v => v.Key);
                foreach (var action in events)
                    action.Call(migratorResult, @interface);
            }
        }

        public static void PerformMigrations(Action<MigratorResult, Type> callbackChanges)
        {
            var contexts = new List<Type>();
            foreach (var context in EntityDescription.Select(description => description.Value.DataContext ?? DefaultDataContext.DataContext))
            {
                if (contexts.Contains(context) || !((AcroDbContextAttribute)context.GetCustomAttributes(typeof(AcroDbContextAttribute), true).Single()).AllowAutoMigration)
                    continue;
                contexts.Add(context);
            }
            foreach (var context in contexts)
            {
                var migrationAttr =
                    (AcroDbContextAttribute) context.GetCustomAttributes(typeof (AcroDbContextAttribute), true).First();
                var migrator = (IMigrator) Activator.CreateInstance(migrationAttr.CustomAutoMigrationProvider);
                var info = DataContextFactory.Instance.Get(migrationAttr.UniqueName);
                var interfaces = GetInterfacesTypes(context).Where(i => !i.Key.GetCustomAttributes(typeof(AcroGenericEntityAttribute), true).Any()).Select(v => v.Value).ToList();
                var callback = new MigrationAutoCallBack(interfaces);
                var callbackOnChanges = callbackChanges == null
                                            ? callback.CallBack
                                            : callback.CallBack + callbackChanges;
                migrator.Migrate(info.DefaultParameters[0], info.ConnectionProviderType, interfaces,
                                 callbackOnChanges);
            }
        }

        private readonly object _dataContextLock = new object();
        private readonly IDictionary<Type, IDataContext> _entityContexts = new Dictionary<Type, IDataContext>();
        private IDataContext ShotForDataContext<TEntityInterface>(EntityInterfaceTypesDescription? description)
        {
            IDataContext context;
            if (_entityContexts.TryGetValue(typeof(TEntityInterface), out context))
                return context;
            if (description != null && description.Value.DataContext != null)
            {
                lock (_dataContextLock)
                {
                    if (!_entityContexts.ContainsKey(typeof(TEntityInterface)))
                    {
                        var c = (IDataContext) Activator.CreateInstance(description.Value.DataContext);
                        _entityContexts[typeof(TEntityInterface)] = c;
                        return c;
                    }
                }
            }
            return CurrentDataContext;
        }

        public IEntityDataProvider<TEntityInterface> Provide<TEntityInterface>() where TEntityInterface : class
        {
            var type = typeof(TEntityInterface);
            if (_entitiyProviders.ContainsKey(type))
                return (IEntityDataProvider<TEntityInterface>)(_entitiyProviders[type]);
            lock (_lockerObject)
            {
                if (_entitiyProviders.ContainsKey(type))
                    return (IEntityDataProvider<TEntityInterface>) (_entitiyProviders[type]);
                var containerDescription = GetEntityDescription<TEntityInterface>();
                IEntityDataProvider<TEntityInterface> val;
                if (containerDescription == null)
                    return null;
                if (containerDescription.Value.Provider == null)
                {
                    val = (IEntityDataProvider<TEntityInterface>)
                          (Activator.CreateInstance(
                              DefaultDataContext.DataProvider.MakeGenericType(
                                  containerDescription.Value.Entity,
                                  type), this, ShotForDataContext<TEntityInterface>(containerDescription)));
                }
                else
                {
                    val = (IEntityDataProvider<TEntityInterface>)
                          (Activator.CreateInstance(containerDescription.Value.Provider,
                                                    this, ShotForDataContext<TEntityInterface>(containerDescription)));
                }
                _entitiyProviders[type] = val;
                return val;
            }
        }

        public void SubmitChanges()
        {
            CurrentDataContext.SubmitChanges();
        }

        #region IDisposable Members
        ///<summary>
        /// Clean the DataContext and close it
        ///</summary>
        public void Dispose()
        {
            foreach (var disposable in _entitiyProviders.Select(pair => pair.Value).OfType<IDisposable>())
                disposable.Dispose();
            foreach (var e in _entityContexts)
                e.Value.Dispose();
            _currentDataContext.Dispose();
        }
        #endregion
    }
}
