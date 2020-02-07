// ChainingAssertion for Unity
// https://github.com/neuecc/ChainingAssertion

using System;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

// namespace Xunit
//{
public static partial class AssertExtensions
{
    /// <summary>Assert.AreEqual.</summary>
    public static void Is<T>(this T actual, T expected, string message = "")
    {
        NUnit.Framework.Assert.AreEqual(expected, actual, message);
    }

    /// <summary>Assert.IsTrue(predicate(value))</summary>
    public static void Is<T>(this T value, Func<T, bool> predicate, string message = "")
    {
        NUnit.Framework.Assert.IsTrue(predicate(value), message);
    }

    /// <summary>CollectionAssert.AreEqual</summary>
    public static void Is<T>(this IEnumerable<T> actual, params T[] expected)
    {
        IsCollection(actual, expected.AsEnumerable());
    }

    /// <summary>CollectionAssert.AreEqual</summary>
    public static void IsCollection<T>(this IEnumerable<T> actual, params T[] expected)
    {
        IsCollection(actual, expected.AsEnumerable());
    }

    /// <summary>CollectionAssert.AreEqual</summary>
    public static void IsCollection<T>(this IEnumerable<T> actual, IEnumerable<T> expected, string message = "")
    {
        CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray(), message);
    }

    /// <summary>CollectionAssert.AreEqual</summary>
    public static void IsCollection<T>(this IEnumerable<T> actual, IEnumerable<T> expected, IEqualityComparer<T> comparer, string message = "")
    {
        IsCollection(actual, expected, comparer.Equals, message);
    }

    /// <summary>CollectionAssert.AreEqual</summary>
    public static void IsCollection<T>(this IEnumerable<T> actual, IEnumerable<T> expected, Func<T, T, bool> equalityComparison, string message = "")
    {
        CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray(), new ComparisonComparer<T>(equalityComparison), message);
    }

    /// <summary>Assert.AreNotEqual</summary>
    public static void IsNot<T>(this T actual, T notExpected, string message = "")
    {
        NUnit.Framework.Assert.AreNotEqual(notExpected, actual, message);
    }

    /// <summary>CollectionAssert.AreNotEqual</summary>
    public static void IsNotCollection<T>(this IEnumerable<T> actual, params T[] notExpected)
    {
        IsNotCollection(actual, notExpected.AsEnumerable());
    }

    /// <summary>CollectionAssert.AreNotEqual</summary>
    public static void IsNotCollection<T>(this IEnumerable<T> actual, IEnumerable<T> notExpected, string message = "")
    {
        CollectionAssert.AreNotEqual(notExpected.ToArray(), actual.ToArray(), message);
    }

    /// <summary>CollectionAssert.AreNotEqual</summary>
    public static void IsNotCollection<T>(this IEnumerable<T> actual, IEnumerable<T> notExpected, IEqualityComparer<T> comparer, string message = "")
    {
        IsNotCollection(actual, notExpected, comparer.Equals, message);
    }

    /// <summary>CollectionAssert.AreNotEqual</summary>
    public static void IsNotCollection<T>(this IEnumerable<T> actual, IEnumerable<T> notExpected, Func<T, T, bool> equalityComparison, string message = "")
    {
        CollectionAssert.AreNotEqual(notExpected.ToArray(), actual.ToArray(), new ComparisonComparer<T>(equalityComparison), message);
    }

    /// <summary>Asset Collefction is empty?</summary>
    public static void IsEmpty<T>(this IEnumerable<T> source)
    {
        source.Any().IsFalse();
    }

    /// <summary>Assert.IsNull</summary>
    public static void IsNull<T>(this T value, string message = "")
    {
        NUnit.Framework.Assert.IsNull(value, message);
    }

    /// <summary>Assert.IsNotNull</summary>
    public static void IsNotNull<T>(this T value, string message = "")
    {
        NUnit.Framework.Assert.IsNotNull(value, message);
    }

    /// <summary>Is(true)</summary>
    public static void IsTrue(this bool value, string message = "")
    {
        value.Is(true, message);
    }

    /// <summary>Is(false)</summary>
    public static void IsFalse(this bool value, string message = "")
    {
        value.Is(false, message);
    }

    /// <summary>Assert.AreSame</summary>
    public static void IsSameReferenceAs<T>(this T actual, T expected, string message = "")
    {
        NUnit.Framework.Assert.AreSame(expected, actual, message);
    }

    /// <summary>Assert.AreNotSame</summary>
    public static void IsNotSameReferenceAs<T>(this T actual, T notExpected, string message = "")
    {
        NUnit.Framework.Assert.AreNotSame(notExpected, actual, message);
    }

    /// <summary>Assert.IsInstanceOf</summary>
    public static TExpected IsInstanceOf<TExpected>(this object value, string message = "")
    {
        NUnit.Framework.Assert.IsInstanceOf<TExpected>(value, message);
        return (TExpected)value;
    }

    /// <summary>Assert.IsNotInstanceOf</summary>
    public static void IsNotInstanceOf<TWrong>(this object value, string message = "")
    {
        NUnit.Framework.Assert.IsNotInstanceOf<TWrong>(value, message);
    }

    /// <summary>EqualityComparison to IComparer Converter for CollectionAssert</summary>
    private class ComparisonComparer<T> : IComparer
    {
        readonly Func<T, T, bool> comparison;

        public ComparisonComparer(Func<T, T, bool> comparison)
        {
            this.comparison = comparison;
        }

        public int Compare(object x, object y)
        {
            return (this.comparison != null)
                ? this.comparison((T)x, (T)y) ? 0 : -1
                : object.Equals(x, y) ? 0 : -1;
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
            throw new NUnit.Framework.AssertionException("actual is null" + message);
        }

        if (expected == null)
        {
            throw new NUnit.Framework.AssertionException("actual is not null" + message);
        }

        if (actual.GetType() != expected.GetType())
        {
            var msg = string.Format(
                "expected type is {0} but actual type is {1}{2}",
                expected.GetType().Name,
                actual.GetType().Name,
                message);
            throw new NUnit.Framework.AssertionException(msg);
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
            throw new NUnit.Framework.AssertionException(msg);
        }
    }

    /// <summary>Assert by deep recursive value equality compare.</summary>
    public static void IsNotStructuralEqual(this object actual, object expected, string message = "")
    {
        message = string.IsNullOrEmpty(message) ? string.Empty : ", " + message;
        if (object.ReferenceEquals(actual, expected))
        {
            throw new NUnit.Framework.AssertionException("actual is same reference" + message);
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
            throw new NUnit.Framework.AssertionException("is structural equal" + message);
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

        foreach (var mi in fields.Cast<MemberInfo>().Concat(properties))
        {
            IEnumerable<string> concatNames = names.Concat(new[] { (string)mi.Name });

            object lv = null;
            object rv = null;
            if (mi is PropertyInfo pi)
            {
                lv = pi.GetValue(left);
                rv = pi.GetValue(right);
            }
            else if (mi is FieldInfo fi)
            {
                lv = fi.GetValue(left);
                rv = fi.GetValue(right);
            }

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
}
//}
