using System;
using System.Collections.Generic;
using AcroDB.Attributes;

namespace AcroDB.InMemory
{
    [AcroDbContext("InMemory", typeof(InMemoryDataProvider<,>))]
    public class InMemoryDataContext : BaseDataContext
    {
        private readonly IDictionary<Type, IList<object>> _storage = new Dictionary<Type, IList<object>>();

        public InMemoryDataContext(string[] parameters) : base(parameters)
        {
        }

        public IList<object> Get<TInterfaceType>()
        {
            IList<object> container;
            if (_storage.TryGetValue(typeof(TInterfaceType), out container))
                return container;
            var c = new List<object>();
            _storage[typeof (TInterfaceType)] = c;
            return c;
        }

        public override void SubmitChanges()
        {
        }
    }
}
