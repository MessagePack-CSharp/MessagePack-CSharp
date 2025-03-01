// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#nullable enable
#pragma warning disable SA1312 // variable naming
#pragma warning disable SA1402 // one type per file
#pragma warning disable SA1513 // ClosingBraceMustBeFollowedByBlankLine
#pragma warning disable SA1516 // ElementsMustBeSeparatedByBlankLine
#pragma warning disable SA1649 // file name matches type name

using System;
using System.Collections.Generic;
using MessagePack.Formatters;
using Unity.Mathematics;

namespace MessagePack.Unity.Mathematics
{
    public class UnityMathematicsResolver : IFormatterResolver
    {
        public static readonly UnityMathematicsResolver Instance = new UnityMathematicsResolver();

        private UnityMathematicsResolver()
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
                Formatter = (IMessagePackFormatter<T>?)UnityResolveryMathematicsResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }

    internal static class UnityResolveryMathematicsResolverGetFormatterHelper
    {
        private static readonly Dictionary<Type, object> FormatterMap = new Dictionary<Type, object>()
        {
            // standard
            { typeof(bool2), new global::MessagePack.Unity.Mathematics.Bool2Formatter() },
            { typeof(bool3), new global::MessagePack.Unity.Mathematics.Bool3Formatter() },
            { typeof(bool4), new global::MessagePack.Unity.Mathematics.Bool4Formatter() },
            { typeof(double2), new global::MessagePack.Unity.Mathematics.Double2Formatter() },
            { typeof(double3), new global::MessagePack.Unity.Mathematics.Double3Formatter() },
            { typeof(double4), new global::MessagePack.Unity.Mathematics.Double4Formatter() },
            { typeof(float2), new global::MessagePack.Unity.Mathematics.Float2Formatter() },
            { typeof(float3), new global::MessagePack.Unity.Mathematics.Float3Formatter() },
            { typeof(float4), new global::MessagePack.Unity.Mathematics.Float4Formatter() },
            { typeof(int2), new global::MessagePack.Unity.Mathematics.Int2Formatter() },
            { typeof(int3), new global::MessagePack.Unity.Mathematics.Int3Formatter() },
            { typeof(int4), new global::MessagePack.Unity.Mathematics.Int4Formatter() },
            { typeof(quaternion), new global::MessagePack.Unity.Mathematics.QuaternionFormatter() },

            // nullable
            { typeof(bool2?), new StaticNullableFormatter<bool2>(new global::MessagePack.Unity.Mathematics.Bool2Formatter()) },
            { typeof(bool3?), new StaticNullableFormatter<bool3>(new global::MessagePack.Unity.Mathematics.Bool3Formatter()) },
            { typeof(bool4?), new StaticNullableFormatter<bool4>(new global::MessagePack.Unity.Mathematics.Bool4Formatter()) },
            { typeof(double2?), new StaticNullableFormatter<double2>(new global::MessagePack.Unity.Mathematics.Double2Formatter()) },
            { typeof(double3?), new StaticNullableFormatter<double3>(new global::MessagePack.Unity.Mathematics.Double3Formatter()) },
            { typeof(double4?), new StaticNullableFormatter<double4>(new global::MessagePack.Unity.Mathematics.Double4Formatter()) },
            { typeof(float2?), new StaticNullableFormatter<float2>(new global::MessagePack.Unity.Mathematics.Float2Formatter()) },
            { typeof(float3?), new StaticNullableFormatter<float3>(new global::MessagePack.Unity.Mathematics.Float3Formatter()) },
            { typeof(float4?), new StaticNullableFormatter<float4>(new global::MessagePack.Unity.Mathematics.Float4Formatter()) },
            { typeof(int2?), new StaticNullableFormatter<int2>(new global::MessagePack.Unity.Mathematics.Int2Formatter()) },
            { typeof(int3?), new StaticNullableFormatter<int3>(new global::MessagePack.Unity.Mathematics.Int3Formatter()) },
            { typeof(int4?), new StaticNullableFormatter<int4>(new global::MessagePack.Unity.Mathematics.Int4Formatter()) },
            { typeof(quaternion?), new StaticNullableFormatter<quaternion>(new global::MessagePack.Unity.Mathematics.QuaternionFormatter()) },
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
