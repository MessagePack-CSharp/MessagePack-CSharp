// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.SourceGenerator.CodeAnalysis;

public class ResolverRegisterInfoComparer : IComparer<ResolverRegisterInfo>
{
    public static readonly ResolverRegisterInfoComparer Default = new();

    private ResolverRegisterInfoComparer()
    {
    }

    public int Compare(ResolverRegisterInfo x, ResolverRegisterInfo y) => x.DataType.CompareTo(y.DataType);
}
