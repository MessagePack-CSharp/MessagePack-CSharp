using System;
using System.Linq.Expressions;
using System.Reflection;

namespace MessagePack.Internal
{
    public static class ExpressionUtility
    {
        // Method

        static MethodInfo GetMethodInfoCore(LambdaExpression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            return (expression.Body as MethodCallExpression).Method;
        }

        /// <summary>
        /// Get MethodInfo from Expression for Static(with result) method.
        /// </summary>
        public static MethodInfo GetMethodInfo<T>(Expression<Func<T>> expression)
        {
            return GetMethodInfoCore(expression);
        }

        /// <summary>
        /// Get MethodInfo from Expression for Static(void) method.
        /// </summary>
        public static MethodInfo GetMethodInfo(Expression<Action> expression)
        {
            return GetMethodInfoCore(expression);
        }

        /// <summary>
        /// Get MethodInfo from Expression for Instance(with result) method.
        /// </summary>
        public static MethodInfo GetMethodInfo<T, TR>(Expression<Func<T, TR>> expression)
        {
            return GetMethodInfoCore(expression);
        }

        /// <summary>
        /// Get MethodInfo from Expression for Instance(void) method.
        /// </summary>
        public static MethodInfo GetMethodInfo<T>(Expression<Action<T>> expression)
        {
            return GetMethodInfoCore(expression);
        }

        // WithArgument(for ref, out) helper

        /// <summary>
        /// Get MethodInfo from Expression for Instance(with result) method.
        /// </summary>
        public static MethodInfo GetMethodInfo<T, TArg1, TR>(Expression<Func<T, TArg1, TR>> expression)
        {
            return GetMethodInfoCore(expression);
        }

        // Property

        static MemberInfo GetMemberInfoCore<T>(Expression<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            var memberExpression = source.Body as MemberExpression;
            return memberExpression.Member;
        }

        public static PropertyInfo GetPropertyInfo<T, TR>(Expression<Func<T, TR>> expression)
        {
            return GetMemberInfoCore(expression) as PropertyInfo;
        }

        // Field

        public static FieldInfo GetFieldInfo<T, TR>(Expression<Func<T, TR>> expression)
        {
            return GetMemberInfoCore(expression) as FieldInfo;
        }
    }
}
