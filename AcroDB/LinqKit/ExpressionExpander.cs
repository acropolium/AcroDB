using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace AcroDB.LinqKit
{
    class ExpressionExpander : ExpressionVisitor
    {
        readonly Dictionary<ParameterExpression, Expression> _replaceVars;

        internal ExpressionExpander () { }

        private ExpressionExpander (Dictionary<ParameterExpression, Expression> replaceVars)
        {
            _replaceVars = replaceVars;
        }

        protected override Expression VisitParameter (ParameterExpression p)
        {
            if ((_replaceVars != null) && (_replaceVars.ContainsKey (p)))
                return _replaceVars[p];
            return base.VisitParameter (p);
        }

        protected override Expression VisitInvocation (InvocationExpression iv)
        {
            var target = iv.Expression;
            if (target is MemberExpression) target = TransformExpr ((MemberExpression)target);
            if (target is ConstantExpression) target = ((ConstantExpression)target).Value as Expression;
            var lambda = (LambdaExpression)target;
            var replaceVars = _replaceVars == null
                                  ? new Dictionary<ParameterExpression, Expression>()
                                  : new Dictionary<ParameterExpression, Expression>(_replaceVars);
            try
            {
                if (lambda == null)
                    throw new ArgumentException("lambda");
                for (var i = 0; i < lambda.Parameters.Count; i++)
                    replaceVars.Add (lambda.Parameters[i], iv.Arguments[i]);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException ("Invoke cannot be called recursively - try using a temporary variable.", ex);
            }

            return new ExpressionExpander (replaceVars).Visit (lambda.Body);
        }

        protected override Expression VisitMethodCall (MethodCallExpression m)
        {
            if (m.Method.Name == "Invoke" && m.Method.DeclaringType == typeof (Extensions))
            {
                var target = m.Arguments[0];
                if (target is MemberExpression) target = TransformExpr ((MemberExpression)target);
                if (target is ConstantExpression) target = ((ConstantExpression) target).Value as Expression;
                var lambda = (LambdaExpression)target;
                var replaceVars = _replaceVars == null
                                      ? new Dictionary<ParameterExpression, Expression>()
                                      : new Dictionary<ParameterExpression, Expression>(_replaceVars);
                try
                {
                    if (lambda == null)
                        throw new ArgumentException("lambda");
                    for (var i = 0; i < lambda.Parameters.Count; i++)
                        replaceVars.Add (lambda.Parameters[i], m.Arguments[i + 1]);
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidOperationException ("Invoke cannot be called recursively - try using a temporary variable.", ex);
                }

                return new ExpressionExpander (replaceVars).Visit (lambda.Body);
            }
            if (m.Method.Name == "Compile" && m.Object is MemberExpression)
            {
                var me = (MemberExpression)m.Object;
                var newExpr = TransformExpr (me);
                if (newExpr != me) return newExpr;
            }
            if (m.Method.Name == "AsExpandable" && m.Method.DeclaringType == typeof (Extensions))
                return m.Arguments[0];

            return base.VisitMethodCall (m);
        }

        protected override Expression VisitMemberAccess (MemberExpression m)
        {
            return m.Member.DeclaringType.Name.StartsWith("<>") ? TransformExpr(m) : base.VisitMemberAccess(m);
        }

        Expression TransformExpr (MemberExpression input)
        {
            if (input == null
                || !(input.Member is FieldInfo)
                || !input.Member.ReflectedType.IsNestedPrivate
                || !input.Member.ReflectedType.Name.StartsWith ("<>"))
                return input;

            if (input.Expression is ConstantExpression)
            {
                var obj = ((ConstantExpression)input.Expression).Value;
                if (obj == null) return input;
                var t = obj.GetType ();
                if (!t.IsNestedPrivate || !t.Name.StartsWith ("<>")) return input;
                var fi = (FieldInfo)input.Member;
                var result = fi.GetValue (obj);
                if (result is Expression) return Visit ((Expression)result);
            }
            return input;
        }
    }
}