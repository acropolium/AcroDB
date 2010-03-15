using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AcroDB.Attributes;
using NoRM;

namespace AcroDB.MongoDb
{
    [ProviderSettings(BaseEntityType = typeof(MongoDbEntity))]
    public class MongoDbDataProvider<TEntity, TInterface> : BaseEntityProvider<TEntity, TInterface>
        where TInterface : class
        where TEntity : MongoDbEntity, TInterface, new()
    {
        public MongoDbDataProvider(IDataContext dataContext)
            : base(dataContext)
        {
        }

        private IQueryable<TEntity> Table
        {
            get
            {
                return DC<MongoDbDataContext>().Context.GetTable<TEntity>();
            }
        }

        private MongoCollection<TEntity> Collection
        {
            get
            {
                return DC<MongoDbDataContext>().Context.GetCollection<TEntity>();
            }
        }

        protected override bool Insert(TInterface instanceOfEntity)
        {
            Collection.Insert((TEntity)instanceOfEntity);
            return true;
        }

        protected override bool Update(TInterface instanceOfEntity)
        {
            try
            {
                var entity = (TEntity) instanceOfEntity;
                Collection.UpdateOne(entity, entity);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override bool Delete(TInterface interfaceOfEntity)
        {
            Collection.Delete((TEntity)interfaceOfEntity);
            return true;
        }

        public override IEnumerable<TInterface> GetFiltered(Expression<Func<TInterface, bool>> predicate, Expression<Func<TInterface, object>> orderPredicate, bool orderAscending)
        {
            var c = GetFiltered(GetRealPredicate<TInterface, TEntity>(predicate),
                                GetRealOPredicate<TInterface, TEntity>(orderPredicate), orderAscending);
            foreach (var item in c)
            {
                ModifyEntity(item);
                yield return item;
            }
        }

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

        private IEnumerable<TEntity> GetFiltered(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, object>> orderPredicate, bool orderAscending)
        {
            var collection = predicate == null ? Table : Table.Where(predicate);
            if (orderPredicate != null)
            {
                if (orderAscending)
                    collection.OrderBy(orderPredicate.Compile());
                else
                    collection.OrderByDescending(orderPredicate.Compile());
            }
            return collection;
        }

        private static Expression<Func<TOutput, object>> GetRealOPredicate<TInput, TOutput>(Expression<Func<TInput, object>> predicate)
        {
            if (predicate == null)
                return null;
            var param = Expression.Parameter(typeof(TOutput), predicate.Parameters[0].Name);
            var temp = (Expression<Func<TInput, object>>)(new TypeModifier<TInput>(param)).Go(predicate);
            return Expression.Lambda<Func<TOutput, object>>(temp.Body, new[] { param });
        }

        private static Expression<Func<TOutput, bool>> GetRealPredicate<TInput, TOutput>(Expression<Func<TInput, bool>> predicate)
        {
            if (predicate == null)
                return null;
            var param = Expression.Parameter(typeof(TOutput), predicate.Parameters[0].Name);
            var temp = (Expression<Func<TInput, bool>>)(new TypeModifier<TInput>(param)).Go(predicate);
            return Expression.Lambda<Func<TOutput, bool>>(temp.Body, new[] { param });
        }
        #endregion
    }
}
