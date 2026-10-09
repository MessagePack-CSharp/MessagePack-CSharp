// The UnityEngine formatter factory and the chain helper that puts it in front of another factory.
// Compiled both inside Unity (MessagePack.Unity) and in MessagePack.UnityShims; C# 9 only, see UnityFormatters.cs.
#nullable enable
using System;
using System.Collections.Generic;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Unity;
using SerializerFoundation;
using UnityEngine;

// analyzer coverage: a [MessagePackObject] member of these types resolves through this factory, so MsgPack101 must not
// flag it in any assembly that references this one (the attribute is harvested from referenced assemblies)
[assembly: MessagePackKnownType(typeof(Vector2))]
[assembly: MessagePackKnownType(typeof(Vector3))]
[assembly: MessagePackKnownType(typeof(Vector4))]
[assembly: MessagePackKnownType(typeof(Quaternion))]
[assembly: MessagePackKnownType(typeof(Color))]
[assembly: MessagePackKnownType(typeof(Color32))]
[assembly: MessagePackKnownType(typeof(Bounds))]
[assembly: MessagePackKnownType(typeof(Rect))]
[assembly: MessagePackKnownType(typeof(Keyframe))]
[assembly: MessagePackKnownType(typeof(AnimationCurve))]
[assembly: MessagePackKnownType(typeof(Matrix4x4))]
[assembly: MessagePackKnownType(typeof(Gradient))]
[assembly: MessagePackKnownType(typeof(GradientColorKey))]
[assembly: MessagePackKnownType(typeof(GradientAlphaKey))]
[assembly: MessagePackKnownType(typeof(RectOffset))]
[assembly: MessagePackKnownType(typeof(LayerMask))]
[assembly: MessagePackKnownType(typeof(Vector2Int))]
[assembly: MessagePackKnownType(typeof(Vector3Int))]
[assembly: MessagePackKnownType(typeof(RangeInt))]
[assembly: MessagePackKnownType(typeof(RectInt))]
[assembly: MessagePackKnownType(typeof(BoundsInt))]

namespace MessagePack.Unity
{
    /// <summary>
    /// Serves the UnityEngine value types (vectors, quaternion, colors, rects, bounds, keyframes and curves, gradients,
    /// matrices, layer masks and the integer variants) in MessagePack-CSharp v3's array wire form.
    /// Nullable, array and List forms of these types come from the generic tier of the default chain, and the WrapMode /
    /// GradientMode enums from its enum tier, so only the value types themselves live here.
    /// </summary>
    public sealed partial class UnityFormatterFactory : MessagePackFormatterFactory
    {
        public static readonly UnityFormatterFactory Instance = new UnityFormatterFactory();

        public UnityFormatterFactory()
        {
        }

        // One method, two signatures. net9+ overrides the base virtual (constraints inherited), while downlevel has no base
        // member, so the constraints are spelled out; the Type-based dispatch is generated for the partial type.
#if NET9_0_OR_GREATER
        public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
        public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
            where TWriteBuffer : struct, IWriteBuffer
            where TReadBuffer : struct, IReadBuffer
#endif
        {
            // The generic tier of the default chain would serve T? / T[] / List<T> by closing its factories over T through
            // reflection, which IL2CPP has no code for unless the instantiation is reachable statically (the smoke player
            // died on Nullable<Vector3> exactly there). Spelling each wrapper out here, generic over the value type, makes
            // every instantiation a static reference, the same reason v3's UnityResolver listed them one by one.
            return
                Struct<Vector2, Vector2Formatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Struct<Vector3, Vector3Formatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Struct<Vector4, Vector4Formatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Struct<Quaternion, QuaternionFormatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Struct<Color, ColorFormatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Struct<Color32, Color32Formatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Struct<Bounds, BoundsFormatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Struct<Rect, RectFormatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Struct<Keyframe, KeyframeFormatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Class<AnimationCurve, AnimationCurveFormatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Struct<Matrix4x4, Matrix4x4Formatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Class<Gradient, GradientFormatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Struct<GradientColorKey, GradientColorKeyFormatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Struct<GradientAlphaKey, GradientAlphaKeyFormatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Class<RectOffset, RectOffsetFormatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Struct<LayerMask, LayerMaskFormatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Struct<Vector2Int, Vector2IntFormatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Struct<Vector3Int, Vector3IntFormatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Struct<RangeInt, RangeIntFormatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Struct<RectInt, RectIntFormatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type) ??
                Struct<BoundsInt, BoundsIntFormatter<TWriteBuffer, TReadBuffer>, TWriteBuffer, TReadBuffer>(type);
        }

        // T, T?, T[] and List<T> for a value type; the wrapper factories are the default chain's own, closed statically
        static object? Struct<T, TFormatter, TWriteBuffer, TReadBuffer>(Type type)
            where T : struct
            where TFormatter : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>, new()
            where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
            where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
        {
            if (type == typeof(T)) return new TFormatter();
            if (type == typeof(T?)) return new NullableFormatterFactory<T>().CreateFormatter<TWriteBuffer, TReadBuffer>(type);
            if (type == typeof(T[])) return new ArrayFormatterFactory<T>().CreateFormatter<TWriteBuffer, TReadBuffer>(type);
            if (type == typeof(List<T>)) return new ListFormatterFactory<T>().CreateFormatter<TWriteBuffer, TReadBuffer>(type);
            return null;
        }

        // T, T[] and List<T> for a reference type (null is the formatter's own nil)
        static object? Class<T, TFormatter, TWriteBuffer, TReadBuffer>(Type type)
            where T : class
            where TFormatter : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T?>, new()
            where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
            where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
        {
            if (type == typeof(T)) return new TFormatter();
            if (type == typeof(T[])) return new ArrayFormatterFactory<T>().CreateFormatter<TWriteBuffer, TReadBuffer>(type);
            if (type == typeof(List<T>)) return new ListFormatterFactory<T>().CreateFormatter<TWriteBuffer, TReadBuffer>(type);
            return null;
        }
    }

    /// <summary>
    /// Chain composition for <see cref="UnityFormatterFactory"/>, the same shape as the core's
    /// <see cref="MessagePackFormatterFactory.WithContractless"/>: the resolver and its settings stay the caller's.
    /// </summary>
    public static class UnityFormatterFactoryExtensions
    {
        /// <summary>
        /// A chain with <see cref="UnityFormatterFactory"/> in front of <paramref name="tail"/>, e.g.
        /// <c>MessagePackFormatterFactory.Default.WithUnity()</c>, or <c>DefaultAot.WithUnity()</c> under IL2CPP and Native AOT.
        /// </summary>
        public static MessagePackFormatterFactory WithUnity(this MessagePackFormatterFactory tail)
            => MessagePackFormatterFactory.Combine(UnityFormatterFactory.Instance, tail);
    }
}
