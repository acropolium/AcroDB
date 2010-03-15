using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AcroDB
{
    public abstract class BaseEntityProvider<TEntity, TInterface> : IDisposable, IEntityDataProvider<TInterface>
        where TInterface : class
        where TEntity : BaseEntity, TInterface, new()
    {
        protected IDataContext DataContext
        {
            get; private set;
        }

        protected void ModifyEntity(TEntity entity)
        {
            entity.OwnerDataContext = DataContext;
        }

        protected TDataContext DC<TDataContext>()
            where TDataContext : IDataContext
        {
            return (TDataContext) DataContext;
        }

        protected BaseEntityProvider(IDataContext dataContext)
        {
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
        public abstract IEnumerable<TInterface> GetFiltered(Expression<Func<TInterface, bool>> predicate, Expression<Func<TInterface, object>> orderPredicate, bool orderAscending);

        public virtual void Dispose() { }
    }
}
