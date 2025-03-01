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
            { typeof(bool2), new global::MessagePack.Unity.Mathematics.Bool2Formatter() },
            { typeof(bool3), new global::MessagePack.Unity.Mathematics.Bool3Formatter() },
            { typeof(double2), new global::MessagePack.Unity.Mathematics.Double2Formatter() },
            { typeof(double3), new global::MessagePack.Unity.Mathematics.Double3Formatter() },
            { typeof(float2), new global::MessagePack.Unity.Mathematics.Float2Formatter() },
            { typeof(float3), new global::MessagePack.Unity.Mathematics.Float3Formatter() },
            { typeof(int2), new global::MessagePack.Unity.Mathematics.Int2Formatter() },
            { typeof(int3), new global::MessagePack.Unity.Mathematics.Int3Formatter() },
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
