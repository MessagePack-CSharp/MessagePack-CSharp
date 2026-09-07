// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MessagePack.Tests
{
    public static partial class AssertEx
    {
        public static void IsZero<T>(this IEnumerable<T> source)
        {
            source.Any().IsFalse();
        }

        /// <summary>Alternative of ExpectedExceptionAttribute(allow derived type).</summary>
        public static T Catch<T>(Action testCode, string message = "")
            where T : Exception
        {
            Exception exception = ExecuteCode(testCode);
            var headerMsg = "Failed Throws<" + typeof(T).Name + ">.";
            var additionalMsg = string.IsNullOrEmpty(message) ? string.Empty : ", " + message;

            if (exception == null)
            {
                var formatted = headerMsg + " No exception was thrown" + additionalMsg;
                throw new AssertFailedException(formatted);
            }
            else if (!typeof(T).GetTypeInfo().IsInstanceOfType(exception))
            {
                var formatted = string.Format("{0} Catched:{1}{2}", headerMsg, exception.GetType().Name, additionalMsg);
                throw new AssertFailedException(formatted);
            }

            return (T)exception;
        }

        public static T Throws<T>(Action testCode, string message = "")
            where T : Exception
        {
            T exception = Catch<T>(testCode, message);

            if (!typeof(T).Equals(exception.GetType()))
            {
                var headerMsg = "Failed Throws<" + typeof(T).Name + ">.";
                var additionalMsg = string.IsNullOrEmpty(message) ? string.Empty : ", " + message;
                var formatted = string.Format("{0} Catched:{1}{2}", headerMsg, exception.GetType().Name, additionalMsg);
                throw new AssertFailedException(formatted);
            }

            return (T)exception;
        }

        /// <summary>execute action and return exception when catched otherwise return null.</summary>
        private static Exception ExecuteCode(Action testCode)
        {
            try
            {
                testCode();
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }
    }
}
