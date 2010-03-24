using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AcroDB.InMemory
{
    public class InMemoryDataProvider<TEntity, TInterface> : BaseEntityProvider<TEntity, TInterface>
        where TInterface : class
        where TEntity : AcroEntity, TInterface, new()
    {
        public InMemoryDataProvider(IDataContextProvider acroDataContext, IDataContext dataContext)
            : base(acroDataContext, dataContext)
        {
        }

        protected override bool Insert(TInterface instanceOfEntity)
        {
            DC<InMemoryDataContext>().Get<TInterface>().Add(instanceOfEntity);
            return true;
        }

        protected override bool Update(TInterface instanceOfEntity)
        {
            return true;
        }

        public override bool Delete(TInterface interfaceOfEntity)
        {
            DC<InMemoryDataContext>().Get<TInterface>().Remove(interfaceOfEntity);
            return true;
        }

        public override IEnumerable<TInterface> GetFiltered(Expression<Func<TInterface, bool>> predicate, Expression<Func<TInterface, object>> orderPredicate, bool orderAscending)
        {
            var c = DC<InMemoryDataContext>().Get<TInterface>();
            var func = predicate != null ? predicate.Compile() : null;
            var p = c.OfType<TInterface>().Where(f => func == null || func(f));
            if (orderPredicate != null)
            {
                if (orderAscending)
                    p.OrderBy(orderPredicate.Compile());
                else
                    p.OrderByDescending(orderPredicate.Compile());
            }
            return p;
        }
    }
}
