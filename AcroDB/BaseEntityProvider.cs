using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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

        protected void ModifyEntity(TEntity entity)
        {
            entity.DataContextProvider = AcroDataContext;
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
        public abstract IEnumerable<TInterface> GetFiltered(Expression<Func<TInterface, bool>> predicate, Expression<Func<TInterface, object>> orderPredicate, bool orderAscending);

        public virtual void Dispose() { }

        #region Expression Converter
        private class TypeModifier<TInput> : LinqKit.ExpressionVisitor
        {
            private readonly ParameterExpression _param;
            public TypeModifier(ParameterExpression param)
            {
                _param = param;
            }

            public Expression Go(Expression expression)
            {
                return Visit(expression);
            }

            protected override Expression VisitParameter(ParameterExpression p)
            {
                return ((p.Type.Equals(typeof(TInput)) || (p.Type.Equals(typeof(IIdEntity)))) && p.Name.Equals(_param.Name)) ? _param : base.VisitParameter(p);
            }
        }

        protected IEnumerable<TEntity> GetFilteredReal(IQueryable<TEntity> queryable, Expression<Func<TInterface, bool>> predicate, Expression<Func<TInterface, object>> orderPredicate, bool orderAscending)
        {
            var originalPredicate = GetRealPredicate<TInterface, TEntity, bool>(predicate);
            var originalOrderPredicate = GetRealPredicate<TInterface, TEntity, object>(orderPredicate);
            var collection = predicate == null ? queryable : (queryable.Where(originalPredicate));
            if (orderPredicate != null)
            {
                if (orderAscending)
                    collection.OrderBy(originalOrderPredicate.Compile());
                else
                    collection.OrderByDescending(originalOrderPredicate.Compile());
            }
            return collection;
        }

        protected static Expression<Func<TOutput, TParamType>> GetRealPredicate<TInput, TOutput, TParamType>(Expression<Func<TInput, TParamType>> predicate)
        {
            if (predicate == null)
                return null;
            var param = Expression.Parameter(typeof(TOutput), predicate.Parameters[0].Name);
            var temp = (Expression<Func<TInput, TParamType>>)(new TypeModifier<TInput>(param)).Go(predicate);
            return Expression.Lambda<Func<TOutput, TParamType>>(temp.Body, new[] { param });
        }
        #endregion
    }
}
