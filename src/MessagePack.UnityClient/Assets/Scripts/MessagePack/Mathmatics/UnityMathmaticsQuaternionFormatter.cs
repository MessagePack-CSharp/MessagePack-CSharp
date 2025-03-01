// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

#nullable enable
#pragma warning disable SA1312 // variable naming
#pragma warning disable SA1402 // one type per file
#pragma warning disable SA1513 // ClosingBraceMustBeFollowedByBlankLine
#pragma warning disable SA1516 // ElementsMustBeSeparatedByBlankLine
#pragma warning disable SA1649 // file name matches type name

namespace MessagePack.Unity
{
    public sealed class QuaternionFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.quaternion>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.quaternion value, global::MessagePack.MessagePackSerializerOptions options)
        {
            var formatter = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>();
            formatter.Serialize(ref writer, value.value, options);
        }

        public global::Unity.Mathematics.quaternion Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            var formatter = options.Resolver.GetFormatterWithVerify<global::Unity.Mathematics.float4>();
            return formatter.Deserialize(ref reader, options);
        }
    }
}
