using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MessagePack.Unity
{
    public class UnityResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new UnityResolver();

        UnityResolver()
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
                formatter = (IMessagePackFormatter<T>)UnityResolveryResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }

    internal static class UnityResolveryResolverGetFormatterHelper
    {
        static readonly Dictionary<Type, object> formatterMap = new Dictionary<Type, object>()
        {
            {typeof(Vector2), new Vector2Formatter()},
            {typeof(Vector3), new Vector3Formatter()},
            {typeof(Vector4), new Vector4Formatter()},
            {typeof(Quaternion), new QuaternionFormatter()},
            {typeof(Color), new ColorFormatter()},
            {typeof(Bounds), new BoundsFormatter()},
            {typeof(Rect), new RectFormatter()},
            {typeof(Vector2?), new StaticNullableFormatter<Vector2>(new Vector2Formatter())},
            {typeof(Vector3?), new StaticNullableFormatter<Vector3>(new Vector3Formatter())},
            {typeof(Vector4?), new StaticNullableFormatter<Vector4>(new Vector4Formatter())},
            {typeof(Quaternion?),new StaticNullableFormatter<Quaternion>(new QuaternionFormatter())},
            {typeof(Color?),new StaticNullableFormatter<Color>(new ColorFormatter())},
            {typeof(Bounds?),new StaticNullableFormatter<Bounds>(new BoundsFormatter())},
            {typeof(Rect?),new StaticNullableFormatter<Rect>(new RectFormatter())},
        };

        internal static object GetFormatter(Type t)
        {
            object formatter;
            if (formatterMap.TryGetValue(t, out formatter))
            {
                return formatter;
            }

            return null;
        }
    }
}