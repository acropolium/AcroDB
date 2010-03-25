using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AcroDB.QueryableProxy
{
    internal class QueryableProxy<TEntity, TInterface> : IOrderedQueryable<TInterface>
        where TInterface : class
        where TEntity : AcroEntity, TInterface, new()
    {
        private readonly IDataContextProvider _acroDataContext;

        public QueryableProxy(IDataContextProvider acroDataContext, IQueryable<TEntity> queryable)
            : this(acroDataContext, null, null, queryable) { }
        public QueryableProxy(IDataContextProvider acroDataContext, IQueryable<TEntity> queryable, Expression expression)
            : this(acroDataContext, null, expression, queryable) { }
        public QueryableProxy(IDataContextProvider acroDataContext, IQueryProvider provider, Expression expression, IQueryable<TEntity> queryable)
        {
            _acroDataContext = acroDataContext;
            Expression = expression ?? Expression.Constant(this);
            Provider = provider ?? new CustomProvider<TEntity, TInterface>(_acroDataContext, queryable);
        }

        private class InternalEnumerator : IEnumerator<TInterface>
        {
            private readonly IDataContextProvider _acroDataContext;
            private readonly IEnumerator<TEntity> _enumerator;
            public InternalEnumerator(IDataContextProvider acroDataContext, IEnumerator<TEntity> enumerator)
            {
                _acroDataContext = acroDataContext;
                _enumerator = enumerator;
            }

            public void Dispose()
            {
                _enumerator.Dispose();
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                _enumerator.Reset();
            }

            public TInterface Current
            {
                get
                {
                    _enumerator.Current.DataContextProvider = _acroDataContext;
                    return _enumerator.Current;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }
        }

        public IEnumerator<TInterface> GetEnumerator()
        {
            return new InternalEnumerator(_acroDataContext, Provider.Execute<IEnumerable<TEntity>>(Expression).GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Expression Expression
        {
            get; private set;
        }

        public Type ElementType
        {
            get { return typeof (TInterface); }
        }

        public IQueryProvider Provider
        {
            get; private set;
        }
    }
}
