using System;
using System.Linq.Expressions;

namespace AcroDB.LinqKit
{
    public static class Linq
    {
        public static Expression<Func<T, TResult>> Expr<T, TResult> (Expression<Func<T, TResult>> expr)
        {
            return expr;
        }

        public static Func<T, TResult> Func<T, TResult> (Func<T, TResult> expr)
        {
            return expr;
        }
    }
}