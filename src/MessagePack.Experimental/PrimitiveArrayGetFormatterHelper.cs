// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

#pragma warning disable CS0436 // The same name of the primary package

using System;

namespace MessagePack.Resolvers
{
    internal static class PrimitiveArrayGetFormatterHelper
    {
        internal static object? GetFormatter(Type t)
        {
            if (!t.IsArray)
            {
                return null;
            }

            if (t == typeof(SByte[]))
            {
                return Formatters.SByteArrayFormatter.Instance;
            }

            if (t == typeof(Int16[]))
            {
                return Formatters.Int16ArrayFormatter.Instance;
            }

            if (t == typeof(Int32[]))
            {
                return Formatters.Int32ArrayFormatter.Instance;
            }

            if (t == typeof(Single[]))
            {
                return Formatters.SingleArrayFormatter.Instance;
            }

            if (t == typeof(Double[]))
            {
                return Formatters.DoubleArrayFormatter.Instance;
            }

            if (t == typeof(Boolean[]))
            {
                return Formatters.BooleanArrayFormatter.Instance;
            }

            return null;
        }
    }
}
