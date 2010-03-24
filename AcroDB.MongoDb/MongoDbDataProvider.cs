using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NoRM;

namespace AcroDB.MongoDb
{
    public class MongoDbDataProvider<TEntity, TInterface> : BaseEntityProvider<TEntity, TInterface>
        where TInterface : class
        where TEntity : AcroEntity, TInterface, new()
    {
        public MongoDbDataProvider(IDataContextProvider acroDataContext, IDataContext dataContext)
            : base(acroDataContext, dataContext)
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
            foreach (var item in GetFilteredReal(Table, predicate, orderPredicate, orderAscending))
            {
                ModifyEntity(item);
                yield return item;
            }
        }
    }
}
