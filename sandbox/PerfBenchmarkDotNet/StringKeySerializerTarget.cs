// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias newmsgpack;
extern alias oldmsgpack;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1312 // Variable names should begin with lower-case letter

namespace PerfBenchmarkDotNet
{
    [oldmsgpack::MessagePack.MessagePackObject(true)]
    [newmsgpack::MessagePack.MessagePackObject(true)]
    public class StringKeySerializerTarget
    {
        public int MyProperty1 { get; set; }

        public int MyProperty2 { get; set; }

        public int MyProperty3 { get; set; }

        public int MyProperty4 { get; set; }

        public int MyProperty5 { get; set; }

        public int MyProperty6 { get; set; }

        public int MyProperty7 { get; set; }

        public int MyProperty8 { get; set; }

        public int MyProperty9 { get; set; }
    }
}

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace

