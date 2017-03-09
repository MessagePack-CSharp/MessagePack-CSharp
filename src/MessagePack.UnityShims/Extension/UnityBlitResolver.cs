#if ENABLE_UNSAFE_MSGPACK

using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MessagePack.Unity.Extension
{
    /// <summary>
    /// Special Resolver for Vector2[], Vector3[], Vector4[], Quaternion[], Color[], Bounds[], Rect[]
    /// </summary>
    public class UnityBlitResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new UnityBlitResolver();

        UnityBlitResolver()
        {

        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                formatter = (IMessagePackFormatter<T>)UnityBlitResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }
    /// <summary>
    /// Special Resolver for Vector2[], Vector3[], Vector4[], Quaternion[], Color[], Bounds[], Rect[] + int[], float[], double[]
    /// </summary>
    public class UnityBlitWithPrimitiveArrayResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new UnityBlitWithPrimitiveArrayResolver();

        UnityBlitWithPrimitiveArrayResolver()
        {

        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                formatter = (IMessagePackFormatter<T>)UnityBlitWithPrimitiveResolverGetFormatterHelper.GetFormatter(typeof(T));
                if (formatter == null)
                {
                    formatter = UnityBlitResolver.Instance.GetFormatter<T>();
                }
            }
        }
    }

    internal static class UnityBlitResolverGetFormatterHelper
    {
        static readonly Dictionary<Type, Type> formatterMap = new Dictionary<Type, Type>()
        {
              {typeof(Vector2[]), typeof(Vector2ArrayBlitFormatter)},
              {typeof(Vector3[]), typeof(Vector3ArrayBlitFormatter)},
              {typeof(Vector4[]), typeof(Vector4ArrayBlitFormatter)},
              {typeof(Quaternion[]), typeof(QuaternionArrayBlitFormatter)},
              {typeof(Color[]), typeof(ColorArrayBlitFormatter)},
              {typeof(Bounds[]), typeof(BoundsArrayBlitFormatter)},
              {typeof(Rect[]), typeof(RectArrayBlitFormatter)},
        };

        internal static object GetFormatter(Type t)
        {
            Type formatterType;
            if (formatterMap.TryGetValue(t, out formatterType))
            {
                return Activator.CreateInstance(formatterType);
            }

            return null;
        }
    }

    internal static class UnityBlitWithPrimitiveResolverGetFormatterHelper
    {
        static readonly Dictionary<Type, Type> formatterMap = new Dictionary<Type, Type>()
        {
              {typeof(int[]), typeof(IntArrayBlitFormatter)},
              {typeof(float[]), typeof(FloatArrayBlitFormatter)},
              {typeof(double[]), typeof(DoubleArrayBlitFormatter)},
        };

        internal static object GetFormatter(Type t)
        {
            Type formatterType;
            if (formatterMap.TryGetValue(t, out formatterType))
            {
                return Activator.CreateInstance(formatterType);
            }

            return null;
        }
    }
}

#endif