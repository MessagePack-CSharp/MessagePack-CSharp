// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MessagePackCompiler.CodeAnalysis;

namespace MessagePackCompiler.Generator;

internal partial struct DeserializeHelperRecursiveTemplate
{
    public DeserializeHelperRecursiveTemplate(int tabCount, int keyLength, IEnumerable<MemberInfoTuple> memberCollection, bool canOverwrite, CancellationToken cancellationToken)
    {
        TabCount = tabCount;
        KeyLength = keyLength;
        Members = memberCollection.ToArray();
        CanOverwrite = canOverwrite;
        CancellationToken = cancellationToken;
    }

    public int TabCount { get; }

    public int KeyLength { get; }

    public MemberInfoTuple[] Members { get; }

    public bool CanOverwrite { get; }

    public CancellationToken CancellationToken { get; }
}
