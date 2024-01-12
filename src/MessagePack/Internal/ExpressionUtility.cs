// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace MessagePack.Internal
{
    internal static class ExpressionUtility
    {
        private static MethodInfo GetMethodInfoCore(LambdaExpression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            return ((MethodCallExpression)expression.Body).Method;
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

        private static MemberInfo GetMemberInfoCore<T>(Expression<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            var memberExpression = (MemberExpression)source.Body;
            return memberExpression.Member;
        }

        public static PropertyInfo GetPropertyInfo<T, TR>(Expression<Func<T, TR>> expression)
        {
            return (PropertyInfo)GetMemberInfoCore(expression);
        }

        public static FieldInfo GetFieldInfo<T, TR>(Expression<Func<T, TR>> expression)
        {
            return (FieldInfo)GetMemberInfoCore(expression);
        }
    }
}
