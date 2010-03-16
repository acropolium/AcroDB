using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AcroUtils.Patterns;

namespace AcroDB
{
    public class DataContextFactory : Singleton<DataContextFactory>, IInitializer
    {
        private IDictionary<string, DataContextDescription> _descriptions;
        public static Func<string, string[]> SettingsCallback;

        public void Initialize()
        {
            if (_descriptions != null)
                return;
            _descriptions = new Dictionary<string, DataContextDescription>();
        }

        public DataContextFactory ScanAssembly(Assembly assembly)
        {
            foreach (var publisherType in from type in assembly.GetTypes()
                                          where type.GetInterfaces().Any(i => i == typeof(IDataContextPublisher))
                                          select type)
            {
                var publisher = Activator.CreateInstance(publisherType) as IDataContextPublisher;
                if (publisher == null)
                    continue;
                var dcd = new DataContextDescription();
                publisher.FillDataContextDescription(dcd);
                if (SettingsCallback != null)
                    dcd.DefaultParameters = SettingsCallback(dcd.Name);
                _descriptions[dcd.Name] = dcd;
            }
            return this;
        }

        public DataContextDescription Get(string contextProviderName)
        {
            DataContextDescription temp;
            return _descriptions.TryGetValue(contextProviderName, out temp) ? temp : null;
        }

        public IEnumerable<DataContextDescription> All
        {
            get
            {
                return _descriptions.Values;
            }
        }
    }

    public class DataContextDescription
    {
        public string[] DefaultParameters { get; set; }
        public Type DataContext { get; set; }
        public Type DataProvider { get; set; }
        public string ConnectionProviderType { get; set; }
        public string Name { get; set; }
    }

    public interface IDataContextPublisher
    {
        void FillDataContextDescription(DataContextDescription description);
    }
}
