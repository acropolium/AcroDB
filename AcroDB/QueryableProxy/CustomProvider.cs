using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AcroDB.LinqKit;

namespace AcroDB.QueryableProxy
{
    internal class CustomProvider<TEntity, TInterface> : IQueryProvider
        where TInterface : class
        where TEntity : AcroEntity, TInterface, new()
    {
        private readonly IDataContextProvider _acroDataContext;
        private readonly IQueryable<TEntity> _queryable;
        public CustomProvider(IDataContextProvider acroDataContext, IQueryable<TEntity> queryable)
        {
            _acroDataContext = acroDataContext;
            _queryable = queryable;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return CreateQuery<TInterface>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return (IQueryable<TElement>)new QueryableProxy<TEntity, TInterface>(_acroDataContext, this, expression, _queryable);
        }

        public object Execute(Expression expression)
        {
            return Execute<TInterface>(expression);
        }

        private class LambdaFinder : ExpressionVisitor
        {
            private string[] _methodNames;
            private readonly IDictionary<string, int> _indicies = new Dictionary<string, int>();
            private readonly List<KeyValuePair<string, object>> _expressions = new List<KeyValuePair<string, object>>();

            public IEnumerable<KeyValuePair<string, object>> Get(Expression expression, params string[] methodNames)
            {
                _methodNames = methodNames;
                _expressions.Clear();
                _indicies.Clear();
                var i = 0;
                foreach (var methodName in _methodNames)
                {
                    _indicies[methodName] = i;
                    i++;
                }
                Visit(expression);
                _expressions.Sort((x, y) => _indicies[x.Key].CompareTo(_indicies[y.Key]));
                return _expressions.ToArray();
            }

            private static object AnalyzeExpression(MethodCallExpression expression)
            {
                if (expression.Arguments.Count < 2)
                    return null;
                var arg = expression.Arguments[1];
                if (arg is UnaryExpression)
                    return ((UnaryExpression) arg).Operand;
                if (arg is ConstantExpression)
                    return ((ConstantExpression)arg).Value;
                return null;
            }

            protected override Expression VisitMethodCall(MethodCallExpression expression)
            {
                if (_methodNames.Contains(expression.Method.Name))
                    _expressions.Add(new KeyValuePair<string, object>(expression.Method.Name, AnalyzeExpression(expression)));
                Visit(expression.Arguments[0]);
                return expression;
            }
        }

        private class TypeModifier<TInput> : ExpressionVisitor
        {
            private readonly ParameterExpression _param1;
            //private readonly ParameterExpression _param2;
            public TypeModifier(ParameterExpression param1)
            {
                _param1 = param1;
                //_param2 = param2;
            }

            public Expression Go(Expression expression)
            {
                return Visit(expression);
            }

            protected override Expression VisitParameter(ParameterExpression p)
            {
                if ((p.Type.Equals(typeof(TInput)) || (p.Type.Equals(typeof(IIdEntity)))) && p.Name.Equals(_param1.Name))
                    return _param1;
                //if (p.Name.Equals(_param2.Name))
                //    return _param2;
                return base.VisitParameter(p);
            }
        }

        protected static Expression<Func<TOutput, TParamType>> GetRealPredicate<TInput, TOutput, TParamType>(LambdaExpression predicate)
        {
            if (predicate == null)
                return null;
            var param1 = Expression.Parameter(typeof(TOutput), predicate.Parameters[0].Name);
            var temp = (Expression<Func<TInput, TParamType>>)(new TypeModifier<TInput>(param1)).Go(predicate);
            return Expression.Lambda<Func<TOutput, TParamType>>(temp.Body, new[] { param1 });
        }

        protected static LambdaExpression GetRealPredicateObj<TInput, TOutput>(LambdaExpression predicate)
        {
            if (predicate == null)
                return null;
            var param1 = Expression.Parameter(typeof(TOutput), predicate.Parameters[0].Name);
            var temp = (LambdaExpression)(new TypeModifier<TInput>(param1).Go(predicate));
            return Expression.Lambda(temp.Body, new[] { param1 });
        }

        protected static IOrderedQueryable<TEntity> OrderQueryInternal<TKey>(IQueryable<TEntity> query, LambdaExpression lambdaExpression, bool ascending)
        {
            return ascending
                       ? query.OrderBy((Expression<Func<TEntity, TKey>>) lambdaExpression)
                       : query.OrderByDescending((Expression<Func<TEntity, TKey>>) lambdaExpression);
        }

        private static MethodInfo _methodInfo;
        private static readonly object Lock = new object();
        private static IOrderedQueryable<TEntity> OrderQuery(IQueryable<TEntity> query, LambdaExpression lambdaExpression, bool ascending)
        {
            if (_methodInfo == null)
            {
                lock (Lock)
                {
                    if (_methodInfo == null)
                    {
                        _methodInfo = typeof (CustomProvider<TEntity, TInterface>).GetMethod("OrderQueryInternal",
                                                                                             BindingFlags.Static |
                                                                                             BindingFlags.NonPublic);
                    }
                }
            }
            return
                (IOrderedQueryable<TEntity>)
                _methodInfo.MakeGenericMethod(lambdaExpression.Body.Type).Invoke(null,
                                                                                 new object[]
                                                                                     {
                                                                                         query, lambdaExpression, ascending
                                                                                     });
        }

        private TInterface ModifyEntity(object entity)
        {
            var e = ((TEntity)entity);
            e.DataContextProvider = _acroDataContext;
            return e;
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var q = _queryable;
            object result = null;
            foreach (var lambda in new LambdaFinder().Get(expression, "Where", "OrderBy", "OrderByDescending", "Skip", "Take", "Count", "FirstOrDefault", "First", "SingleOrDefault", "Single"))
            {
                if (lambda.Key.Equals("Where"))
                {
                    var originalPredicate = GetRealPredicate<TInterface, TEntity, bool>((Expression<Func<TInterface, bool>>) lambda.Value);
                    q = q.Where(originalPredicate);
                    continue;
                }
                if (lambda.Key.Equals("OrderBy"))
                {
                    q = OrderQuery(q, GetRealPredicateObj<TInterface, TEntity>((LambdaExpression)lambda.Value), true);
                    continue;
                }
                if (lambda.Key.Equals("OrderByDescending"))
                {
                    q = OrderQuery(q, GetRealPredicateObj<TInterface, TEntity>((LambdaExpression)lambda.Value), false);
                    continue;
                }
                if (lambda.Key.Equals("Skip"))
                {
                    q = q.Skip((int)lambda.Value);
                    continue;
                }
                if (lambda.Key.Equals("Take"))
                {
                    q = q.Take((int)lambda.Value);
                    continue;
                }
                if (lambda.Key.Equals("Count"))
                {
                    result = q.Count();
                    continue;
                }
                if (lambda.Key.Equals("FirstOrDefault"))
                {
                    result = ModifyEntity(q.FirstOrDefault());
                    continue;
                }
                if (lambda.Key.Equals("First"))
                {
                    result = ModifyEntity(q.First());
                    continue;
                }
                if (lambda.Key.Equals("SingleOrDefault"))
                {
                    result = ModifyEntity(q.SingleOrDefault());
                    continue;
                }
                if (lambda.Key.Equals("Single"))
                {
                    result = ModifyEntity(q.Single());
                    continue;
                }
            }
            if (result == null)
                result = q;
            return (TResult)result;
        }
    }
}
