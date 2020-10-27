// Xunit to NUnit bridge.

using NUnit.Framework;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Xunit
{
    public class FactAttribute : NUnit.Framework.TestAttribute
    {
        public string Skip { get; set; }
    }

    public class SkippableFactAttribute : FactAttribute
    {
    }

    public class TheoryAttribute : FactAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class InlineDataAttribute : NUnit.Framework.TestCaseAttribute
    {
        public InlineDataAttribute(params Object[] data)
            : base(data)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class MemberDataAttribute : NUnit.Framework.TestCaseSourceAttribute
    {
        public MemberDataAttribute(string memberName)
            : base(memberName)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class TraitAttribute : Attribute
    {
        public TraitAttribute(string name, string value)
        {
        }
    }

    public static class Skip
    {
        public static void If(bool condition)
        {
        }
    }

    public static class Assert
    {
        public static T Throws<T>(Action action) where T : Exception
        {
            return NUnit.Framework.Assert.Throws<T>(new TestDelegate(action));
        }

        public static Task<T> ThrowsAsync<T>(Func<Task> action) where T : Exception
        {
            return Task.FromResult(Throws<T>(() => action().GetAwaiter().GetResult()));
        }

        public static T Throws<T>(Func<object> action) where T : Exception
        {
            return NUnit.Framework.Assert.Throws<T>(() => action());
        }

        public static void True(bool value)
        {
            NUnit.Framework.Assert.IsTrue(value);
        }

        public static void False(bool value)
        {
            NUnit.Framework.Assert.IsFalse(value);
        }

        public static void Null(object expected)
        {
            NUnit.Framework.Assert.IsNull(expected);
        }

        public static T IsType<T>(object o)
        {
            NUnit.Framework.Assert.AreEqual(typeof(T), o.GetType());
            return (T)o;
        }

        public static void Equal<T>(T expected, T actual)
        {
            NUnit.Framework.Assert.AreEqual(expected, actual);
        }

        public static void NotEqual<T>(T expected, T actual)
        {
            NUnit.Framework.Assert.AreNotEqual(expected, actual);
        }

        public static void NotEmpty<T>(ICollection<T> source)
        {
            NUnit.Framework.Assert.IsTrue(source.Count != 0);
        }

        public static void NotNull(object value)
        {
            NUnit.Framework.Assert.IsNotNull(value);
        }

        public static void Same(object expected, object actual)
        {
            NUnit.Framework.Assert.AreSame(expected, actual);
        }
    }

    [Serializable]
    public class AssertFailedException : NUnit.Framework.AssertionException
    {
        public AssertFailedException()
            : base("")
        {
        }

        public AssertFailedException(string message) : base(message)
        {
        }

        public AssertFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

namespace Xunit.Abstractions
{
    public interface ITestOutputHelper
    {
        void WriteLine(String message);
        void WriteLine(String format, params Object[] args);
    }
}
