using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AcroDB
{
    public interface IEntityDataProvider<TSubInterface> where TSubInterface : class
    {
        TSubInterface Create();
        bool Save(TSubInterface instanceOfEntity);
        bool Delete(Guid id);
        bool Delete(TSubInterface interfaceOfEntity);
        bool Delete(Expression<Func<TSubInterface, bool>> predicate);

        TSubInterface Get(Guid id);
        TSubInterface FirstOrDefault(Expression<Func<TSubInterface, bool>> predicate);
        TSubInterface FirstOrDefault(Expression<Func<TSubInterface, bool>> predicate,
                                               Expression<Func<TSubInterface, object>> orderPredicate);
        TSubInterface FirstOrDefault(Expression<Func<TSubInterface, bool>> predicate,
                                               Expression<Func<TSubInterface, object>> orderPredicate,
                                               bool orderAscending);
        IEnumerable<TSubInterface> GetAll();
        IEnumerable<TSubInterface> GetFiltered(Expression<Func<TSubInterface, bool>> predicate);

        IEnumerable<TSubInterface> GetFiltered(Expression<Func<TSubInterface, bool>> predicate,
                                               Expression<Func<TSubInterface, object>> orderPredicate);

        IEnumerable<TSubInterface> GetFiltered(Expression<Func<TSubInterface, bool>> predicate,
                                               Expression<Func<TSubInterface, object>> orderPredicate,
                                               bool orderAscending);
    }
}
