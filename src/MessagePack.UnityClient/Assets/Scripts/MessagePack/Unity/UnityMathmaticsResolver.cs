// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#if UNITY_MATHEMATICS_SUPPORT
#pragma warning disable SA1402 // File may only contain a single type

using System;
using System.Collections.Generic;
using MessagePack.Formatters;
using Unity.Mathematics;

namespace MessagePack.Unity
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
            { typeof(bool2), new Bool2Formatter() },
            { typeof(bool3), new Bool3Formatter() },
            { typeof(double2), new Double2Formatter() },
            { typeof(double3), new Double3Formatter() },
            { typeof(float2), new Float2Formatter() },
            { typeof(float3), new Float3Formatter() },
            { typeof(int2), new Int2Formatter() },
            { typeof(int3), new Int3Formatter() },
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
#endif
