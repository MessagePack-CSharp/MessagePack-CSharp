﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Data.Common;

namespace MessagePack
{
#if MESSAGEPACK_INTERNAL
    internal
#else
    public
#endif
    struct Nil : IEquatable<Nil>
    {
        public static readonly Nil Default = default(Nil);

        public static bool operator ==(Nil left, Nil right) => true;

        public static bool operator !=(Nil left, Nil right) => false;

        public override bool Equals(object? obj)
        {
            return obj is Nil;
        }

        public bool Equals(Nil other)
        {
            return true;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return "()";
        }
    }
}
