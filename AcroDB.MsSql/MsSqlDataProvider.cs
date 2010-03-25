using System;
using System.Data.Linq;
using System.Linq;

namespace AcroDB.MsSql
{
    public class MsSqlDataProvider<TEntity, TInterface> : BaseEntityProvider<TEntity, TInterface>
        where TInterface : class
        where TEntity : AcroEntity, TInterface, new()
    {
        public MsSqlDataProvider(IDataContextProvider acroDataContext, IDataContext dataContext)
            : base(acroDataContext, dataContext)
        {
        }

        private Table<TEntity> Table
        {
            get
            {
                return DC<MsSqlDataContext>().Context.GetTable<TEntity>();
            }
        }

        protected override bool Insert(TInterface instanceOfEntity)
        {
            Table.InsertOnSubmit((TEntity)instanceOfEntity);
            return true;
        }

        protected override bool Update(TInterface instanceOfEntity)
        {
            try
            {
                Table.Attach((TEntity)instanceOfEntity);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override bool Delete(TInterface interfaceOfEntity)
        {
            DC<MsSqlDataContext>().Context.GetTable<TEntity>().DeleteOnSubmit((TEntity)interfaceOfEntity);
            return true;
        }

        protected override IQueryable<TEntity> Queryable
        {
            get { return Table.AsQueryable(); }
        }
    }
}
