// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias newmsgpack;
extern alias oldmsgpack;

using ProtoBuf;
using ZeroFormatter;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1312 // Variable names should begin with lower-case letter

namespace PerfBenchmarkDotNet
{
    [newmsgpack::MessagePack.MessagePackObject]
    [ProtoContract]
    [ZeroFormattable]
    public class IntKeySerializerTarget
    {
        [newmsgpack::MessagePack.Key(0)]
        [Index(0)]
        [ProtoMember(1)]
        public virtual int MyProperty1 { get; set; }

        [newmsgpack::MessagePack.Key(1)]
        [Index(1)]
        [ProtoMember(2)]
        public virtual int MyProperty2 { get; set; }

        [newmsgpack::MessagePack.Key(2)]
        [Index(2)]
        [ProtoMember(3)]
        public virtual int MyProperty3 { get; set; }

        [newmsgpack::MessagePack.Key(3)]
        [Index(3)]
        [ProtoMember(4)]
        public virtual int MyProperty4 { get; set; }

        [newmsgpack::MessagePack.Key(4)]
        [Index(4)]
        [ProtoMember(5)]
        public virtual int MyProperty5 { get; set; }

        [newmsgpack::MessagePack.Key(5)]
        [Index(5)]
        [ProtoMember(6)]
        public virtual int MyProperty6 { get; set; }

        [newmsgpack::MessagePack.Key(6)]
        [Index(6)]
        [ProtoMember(7)]
        public virtual int MyProperty7 { get; set; }

        [ProtoMember(8)]
        [newmsgpack::MessagePack.Key(7)]
        [Index(7)]
        public virtual int MyProperty8 { get; set; }

        [ProtoMember(9)]
        [newmsgpack::MessagePack.Key(8)]
        [Index(8)]
        public virtual int MyProperty9 { get; set; }
    }
}

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace

