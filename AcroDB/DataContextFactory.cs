using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AcroDB.Attributes;

namespace AcroDB
{
    public sealed class DataContextFactory
    {
        private readonly IDictionary<string, DataContextDescription> _descriptions = new Dictionary<string, DataContextDescription>();
        public static Func<string, string[]> SettingsCallback;

        #region Safe threaded singleton
        private DataContextFactory() { }
        public static DataContextFactory Instance
        {
            get
            {
                return ThreadSafeSingletonHelper.SafeInstance;
            }
        }
        private class ThreadSafeSingletonHelper
        {
            static ThreadSafeSingletonHelper() { }
            internal static readonly DataContextFactory SafeInstance = new DataContextFactory();
        }
        #endregion

        public DataContextFactory ScanAssembly(Assembly assembly)
        {
            foreach (var contextType in from type in assembly.GetTypes()
                                          where type.GetCustomAttributes(typeof(AcroDbContextAttribute), true).Length > 0
                                          select type)
            {
                var dcd = new DataContextDescription {DataContext = contextType};
                var contextDescription = contextType.GetCustomAttributes(typeof(AcroDbContextAttribute), true).FirstOrDefault() as AcroDbContextAttribute;
                if (contextDescription == null)
                    continue;
                dcd.ConnectionProviderType = contextDescription.LowLevelConnectionProviderType;
                dcd.DataProvider = contextDescription.DataProvider;
                dcd.Name = contextDescription.UniqueName;
                if (SettingsCallback != null)
                    dcd.DefaultParameters = SettingsCallback(dcd.Name);
                _descriptions[dcd.Name] = dcd;
            }
            return this;
        }

        public IDataContextDescription Get(string contextProviderName)
        {
            DataContextDescription temp;
            return _descriptions.TryGetValue(contextProviderName, out temp) ? temp : null;
        }

        public IEnumerable<IDataContextDescription> All
        {
            get
            {
                return _descriptions.Values.Select(x => (IDataContextDescription)x);
            }
        }
    }

    internal class DataContextDescription : IDataContextDescription
    {
        public string[] DefaultParameters { get; set; }
        public Type DataContext { get; set; }
        public Type DataProvider { get; set; }
        public string ConnectionProviderType { get; set; }
        public string Name { get; set; }
    }

    public interface IDataContextDescription
    {
        string[] DefaultParameters { get; set; }
        Type DataContext { get; set; }
        Type DataProvider { get; set; }
        string ConnectionProviderType { get; set; }
        string Name { get; set; }
    }
}
