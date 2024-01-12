// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace MessagePack.Internal
{
    // This code is used in generated code so must be public.

    // RuntimeTypeHandle can embed directly by OpCodes.Ldtoken
    // It does not implements IEquatable<T>(but GetHashCode and Equals is implemented) so needs this to avoid boxing.
    public class RuntimeTypeHandleEqualityComparer : IEqualityComparer<RuntimeTypeHandle>
    {
        public static readonly IEqualityComparer<RuntimeTypeHandle> Default = new RuntimeTypeHandleEqualityComparer();

        private RuntimeTypeHandleEqualityComparer()
        {
        }

        public bool Equals(RuntimeTypeHandle x, RuntimeTypeHandle y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(RuntimeTypeHandle obj)
        {
            return obj.GetHashCode();
        }
    }
}
