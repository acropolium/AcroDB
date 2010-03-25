using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AcroDB.QueryableProxy;

namespace AcroDB
{
    public abstract class BaseEntityProvider<TEntity, TInterface> : IDisposable, IEntityDataProvider<TInterface>
        where TInterface : class
        where TEntity : AcroEntity, TInterface, new()
    {
        protected IDataContext DataContext
        {
            get; private set;
        }

        protected IDataContextProvider AcroDataContext
        {
            get; private set;
        }

        protected TDataContext DC<TDataContext>()
            where TDataContext : IDataContext
        {
            return (TDataContext) DataContext;
        }

        protected BaseEntityProvider(IDataContextProvider acroDataContext, IDataContext dataContext)
        {
            AcroDataContext = acroDataContext;
            DataContext = dataContext;
        }

        protected abstract bool Insert(TInterface instanceOfEntity);
        protected abstract bool Update(TInterface instanceOfEntity);

        protected static Guid GetEntityId(TInterface instanceOfEntity)
        {
            return ((IIdEntity)instanceOfEntity).GetEntityId();
        }
        public virtual TInterface Create()
        {
            return new TEntity();
        }
        public virtual bool Save(TInterface instanceOfEntity)
        {
            if (!Guid.Empty.Equals(GetEntityId(instanceOfEntity)))
                return Update(instanceOfEntity);
            ((IIdEntity) instanceOfEntity).SetEntityId(Guid.NewGuid());
            return Insert(instanceOfEntity);
        }
        public virtual bool Delete(Guid id)
        {
            return Delete(Get(id));
        }
        public abstract bool Delete(TInterface interfaceOfEntity);
        public virtual bool Delete(Expression<Func<TInterface, bool>> predicate)
        {
            return GetFiltered(predicate).Aggregate(true, (current, entity) => current & Delete(entity));
        }

        public virtual TInterface Get(Guid id)
        {
            var pe = Expression.Parameter(typeof(TInterface), "entityObject");
            return FirstOrDefault(Expression.Lambda<Func<TInterface, bool>>(
                Expression.Equal(Expression.Property(pe, "ID"), Expression.Constant(id)),
                pe));
        }
        public virtual TInterface FirstOrDefault(Expression<Func<TInterface, bool>> predicate)
        {
            return FirstOrDefault(predicate, null);
        }
        public virtual TInterface FirstOrDefault(Expression<Func<TInterface, bool>> predicate, Expression<Func<TInterface, object>> orderPredicate)
        {
            return FirstOrDefault(predicate, orderPredicate, true);
        }
        public virtual TInterface FirstOrDefault(Expression<Func<TInterface, bool>> predicate, Expression<Func<TInterface, object>> orderPredicate, bool orderAscending)
        {
            return GetFiltered(predicate, orderPredicate, orderAscending).FirstOrDefault();
        }
        public virtual IEnumerable<TInterface> GetAll()
        {
            return GetFiltered(null);
        }
        public virtual IEnumerable<TInterface> GetFiltered(Expression<Func<TInterface, bool>> predicate)
        {
            return GetFiltered(predicate, null);
        }
        public virtual IEnumerable<TInterface> GetFiltered(Expression<Func<TInterface, bool>> predicate, Expression<Func<TInterface, object>> orderPredicate)
        {
            return GetFiltered(predicate, orderPredicate, true);
        }
        public virtual IEnumerable<TInterface> GetFiltered(Expression<Func<TInterface, bool>> predicate, Expression<Func<TInterface, object>> orderPredicate, bool orderAscending)
        {
            var q = Query;
            if (predicate != null)
                q = q.Where(predicate);
            if (orderPredicate != null)
                return orderAscending ? q.OrderBy(orderPredicate.Compile()) : q.OrderByDescending(orderPredicate.Compile());
            return q;
        }

        public virtual void Dispose() { }

        protected abstract IQueryable<TEntity> Queryable { get; }

        public IQueryable<TInterface> Query
        {
            get { return new QueryableProxy<TEntity, TInterface>(AcroDataContext, Queryable); }
        }
    }
}
