using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Collections;

namespace AcroDB.LinqKit
{
    public class ExpandableQuery<T> : IOrderedQueryable<T>
    {
        readonly ExpandableQueryProvider<T> _provider;
        readonly IQueryable<T> _inner;

        internal IQueryable<T> InnerQuery { get { return _inner; } }

        internal ExpandableQuery (IQueryable<T> inner)
        {
            _inner = inner;
            _provider = new ExpandableQueryProvider<T> (this);
        }

        Expression IQueryable.Expression { get { return _inner.Expression; } }
        Type IQueryable.ElementType { get { return typeof (T); } }
        IQueryProvider IQueryable.Provider { get { return _provider; } }
        public IEnumerator<T> GetEnumerator () { return _inner.GetEnumerator (); }
        IEnumerator IEnumerable.GetEnumerator () { return _inner.GetEnumerator (); }
        public override string ToString () { return _inner.ToString (); }
    }

    class ExpandableQueryProvider<T> : IQueryProvider
    {
        readonly ExpandableQuery<T> _query;

        internal ExpandableQueryProvider (ExpandableQuery<T> query)
        {
            _query = query;
        }

        IQueryable<TElement> IQueryProvider.CreateQuery<TElement> (Expression expression)
        {
            return new ExpandableQuery<TElement> (_query.InnerQuery.Provider.CreateQuery<TElement> (expression.Expand()));
        }

        IQueryable IQueryProvider.CreateQuery (Expression expression)
        {
            return _query.InnerQuery.Provider.CreateQuery (expression.Expand());
        }

        TResult IQueryProvider.Execute<TResult> (Expression expression)
        {
            return _query.InnerQuery.Provider.Execute<TResult> (expression.Expand());
        }

        object IQueryProvider.Execute (Expression expression)
        {
            return _query.InnerQuery.Provider.Execute (expression.Expand());
        }
    }
}