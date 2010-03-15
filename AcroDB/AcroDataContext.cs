using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AcroDB.Attributes;
using AcroDB.EntityFactory;
using AcroDB.InMemory;

namespace AcroDB
{
    public class AcroDataContext : IDisposable
    {
        private readonly IDataContext _currentDataContext;
        private readonly Dictionary<Type, object> _entitiyProviders = new Dictionary<Type, object>();
        private readonly string _lockerObject;

        ///<summary>
        ///
        ///</summary>
        ///<param name="currentDataContext"></param>
        public AcroDataContext(IDataContext currentDataContext)
        {
            _currentDataContext = currentDataContext;
            _lockerObject = String.Format("AcroDataContext{0}{1}", Math.Abs(DateTime.Now.ToFileTime()), Math.Abs((new Random()).Next()));
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
            return (IDataContext) Activator.CreateInstance(DefaultDataContext, (object)DefaultDataContextParams);
        }

        public static Type DefaultDataContext = typeof(InMemoryDataContext);
        public static Type DefaultDataProvider = typeof(InMemoryDataProvider<,>);
        public static object[] DefaultDataContextParams = new object[0];

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
                    (ProviderSettingsAttribute)(attr.CustomDataProvider ?? DefaultDataProvider).GetCustomAttributes(
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
            var dcType = typeof (TDataContextType);
            return from description in EntityDescription
                   where
                       (description.Value.DataContext == null && DefaultDataContext == dcType) ||
                       (description.Value.DataContext != null && description.Value.DataContext == dcType)
                   select new KeyValuePair<Type, Type>(description.Key, description.Value.Entity);
        }

        public static void ScanAssemblyForEntities(Assembly asm)
        {
            foreach (var entityInterface in asm.GetTypes().Where(t => t.IsInterface && t.GetCustomAttributes(typeof(AcroDbEntityAttribute), true).Length > 0))
                RegisterEntityInterface(entityInterface);
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
                              DefaultDataProvider.MakeGenericType(
                                  containerDescription.Value.Entity,
                                  type), ShotForDataContext<TEntityInterface>(containerDescription)));
                }
                else
                {
                    val = (IEntityDataProvider<TEntityInterface>)
                          (Activator.CreateInstance(containerDescription.Value.Provider,
                                                    ShotForDataContext<TEntityInterface>(containerDescription)));
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
