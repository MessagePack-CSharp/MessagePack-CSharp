// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Generator.CodeAnalysis;

internal class ResolverRegisterInfoComparer : IComparer<IResolverRegisterInfo>
{
    internal static readonly ResolverRegisterInfoComparer Default = new();

    private ResolverRegisterInfoComparer()
    {
    }

    public int Compare(IResolverRegisterInfo x, IResolverRegisterInfo y) => StringComparer.Ordinal.Compare(x.FullName, y.FullName);
}
