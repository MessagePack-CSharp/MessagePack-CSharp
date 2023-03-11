// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System;
using System.Collections.Generic;
using MessagePack.Formatters;
using UnityEngine;

namespace MessagePack.Unity
{
    public class UnityResolver : IFormatterResolver
    {
        public static readonly UnityResolver Instance = new UnityResolver();

        private UnityResolver()
        {
        }

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter;

            static FormatterCache()
            {
                Formatter = (IMessagePackFormatter<T>?)UnityResolveryResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }

    internal static class UnityResolveryResolverGetFormatterHelper
    {
        private static readonly Dictionary<Type, object> FormatterMap = new Dictionary<Type, object>()
        {
            // standard
            { typeof(Vector2), new Vector2Formatter() },
            { typeof(Vector3), new Vector3Formatter() },
            { typeof(Vector4), new Vector4Formatter() },
            { typeof(Quaternion), new QuaternionFormatter() },
            { typeof(Color), new ColorFormatter() },
            { typeof(Bounds), new BoundsFormatter() },
            { typeof(Rect), new RectFormatter() },
            { typeof(Vector2?), new StaticNullableFormatter<Vector2>(new Vector2Formatter()) },
            { typeof(Vector3?), new StaticNullableFormatter<Vector3>(new Vector3Formatter()) },
            { typeof(Vector4?), new StaticNullableFormatter<Vector4>(new Vector4Formatter()) },
            { typeof(Quaternion?), new StaticNullableFormatter<Quaternion>(new QuaternionFormatter()) },
            { typeof(Color?), new StaticNullableFormatter<Color>(new ColorFormatter()) },
            { typeof(Bounds?), new StaticNullableFormatter<Bounds>(new BoundsFormatter()) },
            { typeof(Rect?), new StaticNullableFormatter<Rect>(new RectFormatter()) },

            // standard + array
            { typeof(Vector2[]), new ArrayFormatter<Vector2>() },
            { typeof(Vector3[]), new ArrayFormatter<Vector3>() },
            { typeof(Vector4[]), new ArrayFormatter<Vector4>() },
            { typeof(Quaternion[]), new ArrayFormatter<Quaternion>() },
            { typeof(Color[]), new ArrayFormatter<Color>() },
            { typeof(Bounds[]), new ArrayFormatter<Bounds>() },
            { typeof(Rect[]), new ArrayFormatter<Rect>() },
            { typeof(Vector2?[]), new ArrayFormatter<Vector2?>() },
            { typeof(Vector3?[]), new ArrayFormatter<Vector3?>() },
            { typeof(Vector4?[]), new ArrayFormatter<Vector4?>() },
            { typeof(Quaternion?[]), new ArrayFormatter<Quaternion?>() },
            { typeof(Color?[]), new ArrayFormatter<Color?>() },
            { typeof(Bounds?[]), new ArrayFormatter<Bounds?>() },
            { typeof(Rect?[]), new ArrayFormatter<Rect?>() },

            // standard + list
            { typeof(List<Vector2>), new ListFormatter<Vector2>() },
            { typeof(List<Vector3>), new ListFormatter<Vector3>() },
            { typeof(List<Vector4>), new ListFormatter<Vector4>() },
            { typeof(List<Quaternion>), new ListFormatter<Quaternion>() },
            { typeof(List<Color>), new ListFormatter<Color>() },
            { typeof(List<Bounds>), new ListFormatter<Bounds>() },
            { typeof(List<Rect>), new ListFormatter<Rect>() },
            { typeof(List<Vector2?>), new ListFormatter<Vector2?>() },
            { typeof(List<Vector3?>), new ListFormatter<Vector3?>() },
            { typeof(List<Vector4?>), new ListFormatter<Vector4?>() },
            { typeof(List<Quaternion?>), new ListFormatter<Quaternion?>() },
            { typeof(List<Color?>), new ListFormatter<Color?>() },
            { typeof(List<Bounds?>), new ListFormatter<Bounds?>() },
            { typeof(List<Rect?>), new ListFormatter<Rect?>() },

            // new
            { typeof(AnimationCurve),     new AnimationCurveFormatter() },
            { typeof(RectOffset),         new RectOffsetFormatter() },
            { typeof(Gradient),           new GradientFormatter() },
            { typeof(WrapMode),           new WrapModeFormatter() },
            { typeof(GradientMode),       new GradientModeFormatter() },
            { typeof(Keyframe),           new KeyframeFormatter() },
            { typeof(Matrix4x4),          new Matrix4x4Formatter() },
            { typeof(GradientColorKey),   new GradientColorKeyFormatter() },
            { typeof(GradientAlphaKey),   new GradientAlphaKeyFormatter() },
            { typeof(Color32),            new Color32Formatter() },
            { typeof(LayerMask),          new LayerMaskFormatter() },
            { typeof(WrapMode?),          new StaticNullableFormatter<WrapMode>(new WrapModeFormatter()) },
            { typeof(GradientMode?),      new StaticNullableFormatter<GradientMode>(new GradientModeFormatter()) },
            { typeof(Keyframe?),          new StaticNullableFormatter<Keyframe>(new KeyframeFormatter()) },
            { typeof(Matrix4x4?),         new StaticNullableFormatter<Matrix4x4>(new Matrix4x4Formatter()) },
            { typeof(GradientColorKey?),  new StaticNullableFormatter<GradientColorKey>(new GradientColorKeyFormatter()) },
            { typeof(GradientAlphaKey?),  new StaticNullableFormatter<GradientAlphaKey>(new GradientAlphaKeyFormatter()) },
            { typeof(Color32?),           new StaticNullableFormatter<Color32>(new Color32Formatter()) },
            { typeof(LayerMask?),         new StaticNullableFormatter<LayerMask>(new LayerMaskFormatter()) },

            // new + array
            { typeof(AnimationCurve[]),     new ArrayFormatter<AnimationCurve>() },
            { typeof(RectOffset[]),         new ArrayFormatter<RectOffset>() },
            { typeof(Gradient[]),           new ArrayFormatter<Gradient>() },
            { typeof(WrapMode[]),           new ArrayFormatter<WrapMode>() },
            { typeof(GradientMode[]),       new ArrayFormatter<GradientMode>() },
            { typeof(Keyframe[]),           new ArrayFormatter<Keyframe>() },
            { typeof(Matrix4x4[]),          new ArrayFormatter<Matrix4x4>() },
            { typeof(GradientColorKey[]),   new ArrayFormatter<GradientColorKey>() },
            { typeof(GradientAlphaKey[]),   new ArrayFormatter<GradientAlphaKey>() },
            { typeof(Color32[]),            new ArrayFormatter<Color32>() },
            { typeof(LayerMask[]),          new ArrayFormatter<LayerMask>() },
            { typeof(WrapMode?[]),          new ArrayFormatter<WrapMode?>() },
            { typeof(GradientMode?[]),      new ArrayFormatter<GradientMode?>() },
            { typeof(Keyframe?[]),          new ArrayFormatter<Keyframe?>() },
            { typeof(Matrix4x4?[]),         new ArrayFormatter<Matrix4x4?>() },
            { typeof(GradientColorKey?[]),  new ArrayFormatter<GradientColorKey?>() },
            { typeof(GradientAlphaKey?[]),  new ArrayFormatter<GradientAlphaKey?>() },
            { typeof(Color32?[]),           new ArrayFormatter<Color32?>() },
            { typeof(LayerMask?[]),         new ArrayFormatter<LayerMask?>() },

            // new + list
            { typeof(List<AnimationCurve>),     new ListFormatter<AnimationCurve>() },
            { typeof(List<RectOffset>),         new ListFormatter<RectOffset>() },
            { typeof(List<Gradient>),           new ListFormatter<Gradient>() },
            { typeof(List<WrapMode>),           new ListFormatter<WrapMode>() },
            { typeof(List<GradientMode>),       new ListFormatter<GradientMode>() },
            { typeof(List<Keyframe>),           new ListFormatter<Keyframe>() },
            { typeof(List<Matrix4x4>),          new ListFormatter<Matrix4x4>() },
            { typeof(List<GradientColorKey>),   new ListFormatter<GradientColorKey>() },
            { typeof(List<GradientAlphaKey>),   new ListFormatter<GradientAlphaKey>() },
            { typeof(List<Color32>),            new ListFormatter<Color32>() },
            { typeof(List<LayerMask>),          new ListFormatter<LayerMask>() },
            { typeof(List<WrapMode?>),          new ListFormatter<WrapMode?>() },
            { typeof(List<GradientMode?>),      new ListFormatter<GradientMode?>() },
            { typeof(List<Keyframe?>),          new ListFormatter<Keyframe?>() },
            { typeof(List<Matrix4x4?>),         new ListFormatter<Matrix4x4?>() },
            { typeof(List<GradientColorKey?>),  new ListFormatter<GradientColorKey?>() },
            { typeof(List<GradientAlphaKey?>),  new ListFormatter<GradientAlphaKey?>() },
            { typeof(List<Color32?>),           new ListFormatter<Color32?>() },
            { typeof(List<LayerMask?>),         new ListFormatter<LayerMask?>() },

            // unity 2017.2
#if UNITY_2017_2_OR_NEWER

            {typeof(Vector2Int),         new Vector2IntFormatter()},
            {typeof(Vector3Int),         new Vector3IntFormatter()},
            {typeof(RangeInt),           new RangeIntFormatter()},
            {typeof(RectInt),            new RectIntFormatter()},
            {typeof(BoundsInt),          new BoundsIntFormatter()},
            {typeof(Vector2Int?),        new StaticNullableFormatter<Vector2Int>(new Vector2IntFormatter())},
            {typeof(Vector3Int?),        new StaticNullableFormatter<Vector3Int>(new Vector3IntFormatter())},
            {typeof(RangeInt?),          new StaticNullableFormatter<RangeInt>(new RangeIntFormatter())},
            {typeof(RectInt?),           new StaticNullableFormatter<RectInt>(new RectIntFormatter())},
            {typeof(BoundsInt?),         new StaticNullableFormatter<BoundsInt>(new BoundsIntFormatter())},
            // unity 2017.2 + array
            {typeof(Vector2Int[]),       new ArrayFormatter<Vector2Int>()},
            {typeof(Vector3Int[]),       new ArrayFormatter<Vector3Int>()},
            {typeof(RangeInt[]),         new ArrayFormatter<RangeInt>()},
            {typeof(RectInt[]),          new ArrayFormatter<RectInt>()},
            {typeof(BoundsInt[]),        new ArrayFormatter<BoundsInt>()},
            {typeof(Vector2Int?[]),      new ArrayFormatter<Vector2Int?>()},
            {typeof(Vector3Int?[]),      new ArrayFormatter<Vector3Int?>()},
            {typeof(RangeInt?[]),        new ArrayFormatter<RangeInt?>()},
            {typeof(RectInt?[]),         new ArrayFormatter<RectInt?>()},
            {typeof(BoundsInt?[]),       new ArrayFormatter<BoundsInt?>()},
            // unity 2017.2 + list
            {typeof(List<Vector2Int>),       new ListFormatter<Vector2Int>()},
            {typeof(List<Vector3Int>),       new ListFormatter<Vector3Int>()},
            {typeof(List<RangeInt>),         new ListFormatter<RangeInt>()},
            {typeof(List<RectInt>),          new ListFormatter<RectInt>()},
            {typeof(List<BoundsInt>),        new ListFormatter<BoundsInt>()},
            {typeof(List<Vector2Int?>),      new ListFormatter<Vector2Int?>()},
            {typeof(List<Vector3Int?>),      new ListFormatter<Vector3Int?>()},
            {typeof(List<RangeInt?>),        new ListFormatter<RangeInt?>()},
            {typeof(List<RectInt?>),         new ListFormatter<RectInt?>()},
            {typeof(List<BoundsInt?>),       new ListFormatter<BoundsInt?>()},

#endif
        };

        internal static object? GetFormatter(Type t)
        {
            object formatter;
            if (FormatterMap.TryGetValue(t, out formatter))
            {
                return formatter;
            }

            return null;
        }
    }
}
