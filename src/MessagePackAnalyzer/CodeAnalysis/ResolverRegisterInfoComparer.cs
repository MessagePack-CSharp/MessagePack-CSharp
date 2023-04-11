// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePackAnalyzer.CodeAnalysis;

public class ResolverRegisterInfoComparer : IComparer<IResolverRegisterInfo>
{
    public static readonly ResolverRegisterInfoComparer Default = new();

    private ResolverRegisterInfoComparer()
    {
    }

    public int Compare(IResolverRegisterInfo x, IResolverRegisterInfo y) => StringComparer.Ordinal.Compare(x.FullName, y.FullName);
}
