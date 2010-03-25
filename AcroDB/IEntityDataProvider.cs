using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AcroDB
{
    public interface IEntityDataProvider<TSubInterface>
        where TSubInterface : class
    {
        TSubInterface Create();
        bool Save(TSubInterface instanceOfEntity);
        bool Delete(Guid id);
        bool Delete(TSubInterface interfaceOfEntity);
        bool Delete(Expression<Func<TSubInterface, bool>> predicate);

        IQueryable<TSubInterface> Query { get; }

        TSubInterface Get(Guid id);
        [Obsolete("In future versions this method could be removed. Use Query instead")]
        TSubInterface FirstOrDefault(Expression<Func<TSubInterface, bool>> predicate);
        [Obsolete("In future versions this method could be removed. Use Query instead")]
        TSubInterface FirstOrDefault(Expression<Func<TSubInterface, bool>> predicate,
                                               Expression<Func<TSubInterface, object>> orderPredicate);
        [Obsolete("In future versions this method could be removed. Use Query instead")]
        TSubInterface FirstOrDefault(Expression<Func<TSubInterface, bool>> predicate,
                                               Expression<Func<TSubInterface, object>> orderPredicate,
                                               bool orderAscending);
        [Obsolete("In future versions this method could be removed. Use Query instead", true)]
        IEnumerable<TSubInterface> GetAll();
        [Obsolete("In future versions this method could be removed. Use Query instead", true)]
        IEnumerable<TSubInterface> GetFiltered(Expression<Func<TSubInterface, bool>> predicate);
        [Obsolete("In future versions this method could be removed. Use Query instead", true)]
        IEnumerable<TSubInterface> GetFiltered(Expression<Func<TSubInterface, bool>> predicate,
                                               Expression<Func<TSubInterface, object>> orderPredicate);
        [Obsolete("In future versions this method could be removed. Use Query instead", true)]
        IEnumerable<TSubInterface> GetFiltered(Expression<Func<TSubInterface, bool>> predicate,
                                               Expression<Func<TSubInterface, object>> orderPredicate,
                                               bool orderAscending);
    }
}
