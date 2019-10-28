// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*--------------------------------------------------------------------------
 * Chaining Assertion
 * ver 1.7.1.0 (Apr. 29th, 2013)
 *
 * created and maintained by neuecc <ils@neue.cc - @neuecc on Twitter>
 * licensed under Microsoft Public License(Ms-PL)
 * http://chainingassertion.codeplex.com/
 *--------------------------------------------------------------------------*/

/* -- Tutorial --
 * | at first, include this file on xUnit.net Project.
 *
 * | three example, "Is" overloads.
 *
 * // This same as Assert.Equal(25, Math.Pow(5, 2))
 * Math.Pow(5, 2).Is(25);
 *
 * // This same as Assert.True("foobar".StartsWith("foo") && "foobar".EndWith("bar"))
 * "foobar".Is(s => s.StartsWith("foo") && s.EndsWith("bar"));
 *
 * // This same as Assert.Equal(Enumerable.Range(1,5).ToArray(), new[]{1, 2, 3, 4, 5}.ToArray())
 * // it is sequence value compare
 * Enumerable.Range(1, 5).Is(1, 2, 3, 4, 5);
 *
 * | CollectionAssert
 * | if you want to use CollectionAssert Methods then use Linq to Objects and Is
 *
 * var array = new[] { 1, 3, 7, 8 };
 * array.Count().Is(4);
 * array.Contains(8).IsTrue(); // IsTrue() == Is(true)
 * array.All(i => i < 5).IsFalse(); // IsFalse() == Is(false)
 * array.Any().Is(true);
 * new int[] { }.Any().Is(false);   // IsEmpty
 * array.OrderBy(x => x).Is(array); // IsOrdered
 *
 * | Other Assertions
 *
 * // Null Assertions
 * Object obj = null;
 * obj.IsNull();             // Assert.Null(obj)
 * new Object().IsNotNull(); // Assert.NotNull(obj)
 *
 * // Not Assertion
 * "foobar".IsNot("fooooooo"); // Assert.NotEqual
 * new[] { "a", "z", "x" }.IsNot("a", "x", "z"); /// Assert.NotEqual
 *
 * // ReferenceEqual Assertion
 * var tuple = Tuple.Create("foo");
 * tuple.IsSameReferenceAs(tuple); // Assert.Same
 * tuple.IsNotSameReferenceAs(Tuple.Create("foo")); // Assert.NotSame
 *
 * // Type Assertion
 * "foobar".IsInstanceOf<string>(); // Assert.IsType
 * (999).IsNotInstanceOf<double>(); // Assert.IsNotType
 *
 * | Advanced Collection Assertion
 *
 * var lower = new[] { "a", "b", "c" };
 * var upper = new[] { "A", "B", "C" };
 *
 * // Comparer CollectionAssert, use IEqualityComparer<T> or Func<T,T,bool> delegate
 * lower.Is(upper, StringComparer.InvariantCultureIgnoreCase);
 * lower.Is(upper, (x, y) => x.ToUpper() == y.ToUpper());
 *
 * // or you can use Linq to Objects - SequenceEqual
 * lower.SequenceEqual(upper, StringComparer.InvariantCultureIgnoreCase).Is(true);
 *
 * | StructuralEqual
 *
 * class MyClass
 * {
 *     public int IntProp { get; set; }
 *     public string StrField;
 * }
 *
 * var mc1 = new MyClass() { IntProp = 10, StrField = "foo" };
 * var mc2 = new MyClass() { IntProp = 10, StrField = "foo" };
 *
 * mc1.IsStructuralEqual(mc2); // deep recursive value equality compare
 *
 * mc1.IntProp = 20;
 * mc1.IsNotStructuralEqual(mc2);
 *
 * | DynamicAccessor
 *
 * // AsDynamic convert to "dynamic" that can call private method/property/field/indexer.
 *
 * // a class and private field/property/method.
 * public class PrivateMock
 * {
 *     private string privateField = "homu";
 *
 *     private string PrivateProperty
 *     {
 *         get { return privateField + privateField; }
 *         set { privateField = value; }
 *     }
 *
 *     private string PrivateMethod(int count)
 *     {
 *         return string.Join("", Enumerable.Repeat(privateField, count));
 *     }
 * }
 *
 * // call private property.
 * var actual = new PrivateMock().AsDynamic().PrivateProperty;
 * Assert.AreEqual("homuhomu", actual);
 *
 * // dynamic can't invoke extension methods.
 * // if you want to invoke "Is" then cast type.
 * (new PrivateMock().AsDynamic().PrivateMethod(3) as string).Is("homuhomuhomu");
 *
 * // set value
 * var mock = new PrivateMock().AsDynamic();
 * mock.PrivateProperty = "mogumogu";
 * (mock.privateField as string).Is("mogumogu");
 *
 * -- more details see project home --*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit.Sdk;

namespace Xunit
{
    #region Extensions

    [System.Diagnostics.DebuggerStepThroughAttribute]
    public static partial class AssertEx
    {
        /// <summary>Assert.Equal, if T is IEnumerable then compare value equality.</summary>
        public static void Is<T>(this T actual, T expected)
        {
            if (expected == null)
            {
                Assert.Null(actual);
                return;
            }

            if (typeof(T) != typeof(string) && typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(typeof(T)))
            {
                Assert.Equal(
                    ((IEnumerable)expected).Cast<object>().ToArray(),
                    ((IEnumerable)actual).Cast<object>().ToArray());
                return;
            }

            Assert.Equal(expected, actual);
        }

        /// <summary>Assert.True(predicate(value)).</summary>
        public static void Is<T>(this T value, Expression<Func<T, bool>> predicate, string message = "")
        {
            var condition = predicate.Compile().Invoke(value);

            var paramName = predicate.Parameters.First().Name;
            string msg = string.Empty;
            try
            {
                var dumper = new ExpressionDumper<T>(value, predicate.Parameters.Single());
                dumper.Visit(predicate);
                var dump = string.Join(", ", dumper.Members.Select(kvp => kvp.Key + " = " + kvp.Value));
                msg = string.Format(
                    "\r\n{0} = {1}\r\n{2}\r\n{3}{4}",
                    paramName,
                    value,
                    dump,
                    predicate,
                    string.IsNullOrEmpty(message) ? string.Empty : ", " + message);
            }
            catch
            {
                msg = string.Format(
                    "{0} = {1}, {2}{3}",
                    paramName,
                    value,
                    predicate,
                    string.IsNullOrEmpty(message) ? string.Empty : ", " + message);
            }

            Assert.True(condition, msg);
        }

        /// <summary>Assert.Equal.</summary>
        public static void Is<T>(this T actual, T expected, IEqualityComparer<T> comparer)
        {
            Assert.Equal(expected, actual, comparer);
        }

        /// <summary>Assert.Equal(sequence value compare).</summary>
        public static void Is<T>(this IEnumerable<T> actual, params T[] expected)
        {
            Is(actual, expected.AsEnumerable());
        }

        /// <summary>Assert.Equal(sequence value compare).</summary>
        public static void Is<T>(this IEnumerable<T> actual, IEnumerable<T> expected)
        {
            Assert.Equal(expected.ToArray(), actual.ToArray());
        }

        /// <summary>Assert.True(actual.SequenceEqual(expected, comparer)).</summary>
        public static void Is<T>(this IEnumerable<T> actual, IEnumerable<T> expected, IEqualityComparer<T> comparer)
        {
            Assert.True(actual.SequenceEqual(expected, comparer));
        }

        /// <summary>Assert.True(actual.SequenceEqual(expected, comparison)).</summary>
        public static void Is<T>(this IEnumerable<T> actual, IEnumerable<T> expected, Func<T, T, bool> equalityComparison)
        {
            Assert.True(actual.SequenceEqual(expected, new EqualityComparer<T>(equalityComparison)));
        }

        /// <summary>Assert.NotEqual, if T is IEnumerable then check value equality.</summary>
        public static void IsNot<T>(this T actual, T notExpected)
        {
            if (typeof(T) != typeof(string) && typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(typeof(T)))
            {
                Assert.NotEqual(
                    ((IEnumerable)actual).Cast<object>().ToArray(),
                    ((IEnumerable)notExpected).Cast<object>().ToArray());
                return;
            }

            Assert.NotEqual(notExpected, actual);
        }

        /// <summary>Assert.NotEqual.</summary>
        public static void IsNot<T>(this T actual, T notExpected, IEqualityComparer<T> comparer)
        {
            Assert.NotEqual(notExpected, actual, comparer);
        }

        /// <summary>Assert.NotEqual(sequence value compare).</summary>
        public static void IsNot<T>(this IEnumerable<T> actual, params T[] notExpected)
        {
            IsNot(actual, notExpected.AsEnumerable());
        }

        /// <summary>Assert.NotEqual(sequence value compare).</summary>
        public static void IsNot<T>(this IEnumerable<T> actual, IEnumerable<T> notExpected)
        {
            Assert.NotEqual(notExpected.ToArray(), actual.ToArray());
        }

        /// <summary>Assert.False(actual.SequenceEqual(notExpected, comparer)).</summary>
        public static void IsNot<T>(this IEnumerable<T> actual, IEnumerable<T> notExpected, IEqualityComparer<T> comparer)
        {
            Assert.False(actual.SequenceEqual(notExpected, comparer));
        }

        /// <summary>Assert.False(actual.SequenceEqual(notExpected, comparison)).</summary>
        public static void IsNot<T>(this IEnumerable<T> actual, IEnumerable<T> notExpected, Func<T, T, bool> equalityComparison)
        {
            Assert.False(actual.SequenceEqual(notExpected, new EqualityComparer<T>(equalityComparison)));
        }

        /// <summary>Assert.Null.</summary>
        public static void IsNull<T>(this T value)
        {
            Assert.Null(value);
        }

        /// <summary>Assert.NotNull.</summary>
        public static void IsNotNull<T>(this T value)
        {
            Assert.NotNull(value);
        }

        /// <summary>Is(true).</summary>
        public static void IsTrue(this bool value)
        {
            value.Is(true);
        }

        /// <summary>Is(false).</summary>
        public static void IsFalse(this bool value)
        {
            value.Is(false);
        }

        /// <summary>Assert.Same.</summary>
        public static void IsSameReferenceAs<T>(this T actual, T expected)
            where T : class
        {
            Assert.Same(expected, actual);
        }

        /// <summary>Assert.NotSame.</summary>
        public static void IsNotSameReferenceAs<T>(this T actual, T notExpected)
            where T : class
        {
            Assert.NotSame(notExpected, actual);
        }

        /// <summary>Assert.IsType.</summary>
        public static TExpected IsInstanceOf<TExpected>(this object value)
        {
            Assert.IsType<TExpected>(value);
            return (TExpected)value;
        }

        /// <summary>Assert.IsNotType.</summary>
        public static void IsNotInstanceOf<TWrong>(this object value)
        {
            Assert.IsNotType<TWrong>(value);
        }

        /// <summary>EqualityComparison to IEqualityComparer Converter for CollectionAssert.</summary>
        private class EqualityComparer<T> : IEqualityComparer<T>
        {
            private readonly Func<T, T, bool> comparison;

            public EqualityComparer(Func<T, T, bool> comparison)
            {
                this.comparison = comparison;
            }

            public bool Equals(T x, T y)
            {
                return (this.comparison != null)
                    ? this.comparison(x, y)
                    : object.Equals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return 0;
            }
        }

        private class ReflectAccessor<T>
        {
            public Func<object> GetValue { get; private set; }

            public Action<object> SetValue { get; private set; }

            public ReflectAccessor(T target, string name)
            {
                FieldInfo field = typeof(T).GetTypeInfo().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    this.GetValue = () => field.GetValue(target);
                    this.SetValue = value => field.SetValue(target, value);
                    return;
                }

                PropertyInfo prop = typeof(T).GetTypeInfo().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null)
                {
                    this.GetValue = () => prop.GetValue(target, null);
                    this.SetValue = value => prop.SetValue(target, value, null);
                    return;
                }

                throw new ArgumentException(string.Format("\"{0}\" not found : Type <{1}>", name, typeof(T).Name));
            }
        }

        #region StructuralEqual

        /// <summary>Assert by deep recursive value equality compare.</summary>
        public static void IsStructuralEqual(this object actual, object expected, string message = "")
        {
            message = string.IsNullOrEmpty(message) ? string.Empty : ", " + message;
            if (object.ReferenceEquals(actual, expected))
            {
                return;
            }

            if (actual == null)
            {
                throw new AssertException("actual is null" + message);
            }

            if (expected == null)
            {
                throw new AssertException("actual is not null" + message);
            }

            if (actual.GetType() != expected.GetType())
            {
                var msg = string.Format(
                    "expected type is {0} but actual type is {1}{2}",
                    expected.GetType().Name,
                    actual.GetType().Name,
                    message);
                throw new AssertException(msg);
            }

            EqualInfo r = StructuralEqual(actual, expected, new[] { actual.GetType().Name }); // root type
            if (!r.IsEquals)
            {
                var msg = string.Format(
                    "is not structural equal, failed at {0}, actual = {1} expected = {2}{3}",
                    string.Join(".", r.Names),
                    r.Left,
                    r.Right,
                    message);
                throw new AssertException(msg);
            }
        }

        /// <summary>Assert by deep recursive value equality compare.</summary>
        public static void IsNotStructuralEqual(this object actual, object expected, string message = "")
        {
            message = string.IsNullOrEmpty(message) ? string.Empty : ", " + message;
            if (object.ReferenceEquals(actual, expected))
            {
                throw new AssertException("actual is same reference" + message);
            }

            if (actual == null)
            {
                return;
            }

            if (expected == null)
            {
                return;
            }

            if (actual.GetType() != expected.GetType())
            {
                return;
            }

            EqualInfo r = StructuralEqual(actual, expected, new[] { actual.GetType().Name }); // root type
            if (r.IsEquals)
            {
                throw new AssertException("is structural equal" + message);
            }
        }

        private static EqualInfo SequenceEqual(IEnumerable leftEnumerable, IEnumerable rightEnumarable, IEnumerable<string> names)
        {
            IEnumerator le = leftEnumerable.GetEnumerator();
            using (le as IDisposable)
            {
                IEnumerator re = rightEnumarable.GetEnumerator();

                using (re as IDisposable)
                {
                    var index = 0;
                    while (true)
                    {
                        object lValue = null;
                        object rValue = null;
                        var lMove = le.MoveNext();
                        var rMove = re.MoveNext();
                        if (lMove)
                        {
                            lValue = le.Current;
                        }

                        if (rMove)
                        {
                            rValue = re.Current;
                        }

                        if (lMove && rMove)
                        {
                            EqualInfo result = StructuralEqual(lValue, rValue, names.Concat(new[] { "[" + index + "]" }));
                            if (!result.IsEquals)
                            {
                                return result;
                            }
                        }

                        if ((lMove == true && rMove == false) || (lMove == false && rMove == true))
                        {
                            return new EqualInfo { IsEquals = false, Left = lValue, Right = rValue, Names = names.Concat(new[] { "[" + index + "]" }) };
                        }

                        if (lMove == false && rMove == false)
                        {
                            break;
                        }

                        index++;
                    }
                }
            }

            return new EqualInfo { IsEquals = true, Left = leftEnumerable, Right = rightEnumarable, Names = names };
        }

        private static EqualInfo StructuralEqual(object left, object right, IEnumerable<string> names)
        {
            // type and basic checks
            if (object.ReferenceEquals(left, right))
            {
                return new EqualInfo { IsEquals = true, Left = left, Right = right, Names = names };
            }

            if (left == null || right == null)
            {
                return new EqualInfo { IsEquals = false, Left = left, Right = right, Names = names };
            }

            Type lType = left.GetType();
            Type rType = right.GetType();
            if (lType != rType)
            {
                return new EqualInfo { IsEquals = false, Left = left, Right = right, Names = names };
            }

            Type type = left.GetType();

            // not object(int, string, etc...)
            if (Type.GetTypeCode(type) != TypeCode.Object)
            {
                return new EqualInfo { IsEquals = left.Equals(right), Left = left, Right = right, Names = names };
            }

            // is sequence
            if (typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type))
            {
                return SequenceEqual((IEnumerable)left, (IEnumerable)right, names);
            }

            // IEquatable<T>
            Type equatable = typeof(IEquatable<>).MakeGenericType(type);
            if (equatable.GetTypeInfo().IsAssignableFrom(type))
            {
                var result = (bool)equatable.GetTypeInfo().GetMethod("Equals").Invoke(left, new[] { right });
                return new EqualInfo { IsEquals = result, Left = left, Right = right, Names = names };
            }

            // is object
            FieldInfo[] fields = left.GetType().GetTypeInfo().GetFields(BindingFlags.Instance | BindingFlags.Public);
            IEnumerable<PropertyInfo> properties = left.GetType().GetTypeInfo().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.GetGetMethod(false) != null && x.GetIndexParameters().Length == 0);
            IEnumerable<MemberInfo> members = fields.Cast<MemberInfo>().Concat(properties);

            foreach (dynamic mi in members)
            {
                IEnumerable<string> concatNames = names.Concat(new[] { (string)mi.Name });

                object lv = mi.GetValue(left);
                object rv = mi.GetValue(right);
                EqualInfo result = StructuralEqual(lv, rv, concatNames);
                if (!result.IsEquals)
                {
                    return result;
                }
            }

            return new EqualInfo { IsEquals = true, Left = left, Right = right, Names = names };
        }

        private class EqualInfo
        {
            public object Left;
            public object Right;
            public bool IsEquals;
            public IEnumerable<string> Names;
        }

        #endregion

        #region ExpressionDumper

        private class ExpressionDumper<T> : ExpressionVisitor
        {
            private ParameterExpression param;
            private T target;

            public Dictionary<string, object> Members { get; private set; }

            public ExpressionDumper(T target, ParameterExpression param)
            {
                this.target = target;
                this.param = param;
                this.Members = new Dictionary<string, object>();
            }

            protected override System.Linq.Expressions.Expression VisitMember(MemberExpression node)
            {
                if (node.Expression == this.param && !this.Members.ContainsKey(node.Member.Name))
                {
                    var accessor = new ReflectAccessor<T>(this.target, node.Member.Name);
                    this.Members.Add(node.Member.Name, accessor.GetValue());
                }

                return base.VisitMember(node);
            }
        }

        #endregion
    }

    #endregion

    public class AssertException : Exception
    {
        public AssertException(string message)
            : base(message)
        {
        }
    }

    public class AssertFailedException : Exception
    {
        public AssertFailedException(string message)
            : base(message)
        {
        }
    }
}
