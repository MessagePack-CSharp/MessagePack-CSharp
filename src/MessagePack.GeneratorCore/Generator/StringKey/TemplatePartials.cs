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

internal partial struct DeserializeHelperTemplate
{
    public DeserializeHelperTemplate(ObjectSerializationInfo info, bool canOverwrite, CancellationToken cancellationToken)
    {
        CanOverwrite = canOverwrite;
        CancellationToken = cancellationToken;
        MemberInfoTupleGroupCollection = info.Members.Select(member =>
        {
            var value = false;
            foreach (var parameter in info.ConstructorParameters)
            {
                if (parameter.Equals(member))
                {
                    value = true;
                    break;
                }
            }

            return new MemberInfoTuple(member, value);
        }).GroupBy(member => member.Binary.Length);
    }

    public IEnumerable<IGrouping<int, MemberInfoTuple>> MemberInfoTupleGroupCollection { get; }

    public bool CanOverwrite { get; }

    public CancellationToken CancellationToken { get; }
}
