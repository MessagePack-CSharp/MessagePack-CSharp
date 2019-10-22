// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias newmsgpack;

#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace GeneratedFormatter
{
    using System;
    using System.Buffers;
    using System.Text;
    using newmsgpack::MessagePack;
    using newmsgpack::MessagePack.Formatters;
    using newmsgpack::MessagePack.Internal;
    using PerfBenchmarkDotNet;

    namespace MessagePack.Formatters
    {
        public sealed class StringKeySerializerTargetFormatter_ByteArrayStringHashTable : IMessagePackFormatter<StringKeySerializerTarget>
        {
            private readonly ByteArrayStringHashTable keyMapping;

            private readonly byte[][] stringByteKeys;

            public StringKeySerializerTargetFormatter_ByteArrayStringHashTable()
            {
                this.keyMapping = new ByteArrayStringHashTable(9)
                {
                    {
                        "MyProperty1",
                        0
                    },
                    {
                        "MyProperty2",
                        1
                    },
                    {
                        "MyProperty3",
                        2
                    },
                    {
                        "MyProperty4",
                        3
                    },
                    {
                        "MyProperty5",
                        4
                    },
                    {
                        "MyProperty6",
                        5
                    },
                    {
                        "MyProperty7",
                        6
                    },
                    {
                        "MyProperty8",
                        7
                    },
                    {
                        "MyProperty9",
                        8
                    },
                };
                this.stringByteKeys = new byte[][]
                {
                Encoding.UTF8.GetBytes("MyProperty1"),
                Encoding.UTF8.GetBytes("MyProperty2"),
                Encoding.UTF8.GetBytes("MyProperty3"),
                Encoding.UTF8.GetBytes("MyProperty4"),
                Encoding.UTF8.GetBytes("MyProperty5"),
                Encoding.UTF8.GetBytes("MyProperty6"),
                Encoding.UTF8.GetBytes("MyProperty7"),
                Encoding.UTF8.GetBytes("MyProperty8"),
                Encoding.UTF8.GetBytes("MyProperty9"),
                };
            }

            public void Serialize(ref MessagePackWriter writer, StringKeySerializerTarget stringKeySerializerTarget, MessagePackSerializerOptions options)
            {
                if (stringKeySerializerTarget == null)
                {
                    writer.WriteNil();
                    return;
                }

                writer.WriteMapHeader(9);
                writer.WriteString(this.stringByteKeys[0]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty1);
                writer.WriteString(this.stringByteKeys[1]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty2);
                writer.WriteString(this.stringByteKeys[2]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty3);
                writer.WriteString(this.stringByteKeys[3]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty4);
                writer.WriteString(this.stringByteKeys[4]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty5);
                writer.WriteString(this.stringByteKeys[5]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty6);
                writer.WriteString(this.stringByteKeys[6]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty7);
                writer.WriteString(this.stringByteKeys[7]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty8);
                writer.WriteString(this.stringByteKeys[8]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty9);
            }

            public StringKeySerializerTarget Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                if (reader.TryReadNil())
                {
                    return null;
                }

                int num3 = reader.ReadMapHeader();
                int myProperty = 0;
                int myProperty2 = 0;
                int myProperty3 = 0;
                int myProperty4 = 0;
                int myProperty5 = 0;
                int myProperty6 = 0;
                int myProperty7 = 0;
                int myProperty8 = 0;
                int myProperty9 = 0;
                for (int i = 0; i < num3; i++)
                {
                    int num4;
                    bool arg_47_0 = this.keyMapping.TryGetValue(reader.ReadStringSegment().Value, out num4);
                    if (!arg_47_0)
                    {
                        reader.Skip();
                    }
                    else
                    {
                        switch (num4)
                        {
                            case 0:
                                myProperty = reader.ReadInt32();
                                break;
                            case 1:
                                myProperty2 = reader.ReadInt32();
                                break;
                            case 2:
                                myProperty3 = reader.ReadInt32();
                                break;
                            case 3:
                                myProperty4 = reader.ReadInt32();
                                break;
                            case 4:
                                myProperty5 = reader.ReadInt32();
                                break;
                            case 5:
                                myProperty6 = reader.ReadInt32();
                                break;
                            case 6:
                                myProperty7 = reader.ReadInt32();
                                break;
                            case 7:
                                myProperty8 = reader.ReadInt32();
                                break;
                            case 8:
                                myProperty9 = reader.ReadInt32();
                                break;
                            default:
                                reader.Skip();
                                break;
                        }
                    }
                }

                return new StringKeySerializerTarget
                {
                    MyProperty1 = myProperty,
                    MyProperty2 = myProperty2,
                    MyProperty3 = myProperty3,
                    MyProperty4 = myProperty4,
                    MyProperty5 = myProperty5,
                    MyProperty6 = myProperty6,
                    MyProperty7 = myProperty7,
                    MyProperty8 = myProperty8,
                    MyProperty9 = myProperty9,
                };
            }
        }

        public sealed class StringKeySerializerTargetFormatter_AutomataLookup : IMessagePackFormatter<StringKeySerializerTarget>
        {
            private readonly AutomataDictionary keyMapping;

            private readonly byte[][] stringByteKeys;

            public StringKeySerializerTargetFormatter_AutomataLookup()
            {
                this.keyMapping = new AutomataDictionary()
                {
                    {
                        "MyProperty1",
                        0
                    },
                    {
                        "MyProperty2",
                        1
                    },
                    {
                        "MyProperty3",
                        2
                    },
                    {
                        "MyProperty4",
                        3
                    },
                    {
                        "MyProperty5",
                        4
                    },
                    {
                        "MyProperty6",
                        5
                    },
                    {
                        "MyProperty7",
                        6
                    },
                    {
                        "MyProperty8",
                        7
                    },
                    {
                        "MyProperty9",
                        8
                    },
                };
                this.stringByteKeys = new byte[][]
                {
                Encoding.UTF8.GetBytes("MyProperty1"),
                Encoding.UTF8.GetBytes("MyProperty2"),
                Encoding.UTF8.GetBytes("MyProperty3"),
                Encoding.UTF8.GetBytes("MyProperty4"),
                Encoding.UTF8.GetBytes("MyProperty5"),
                Encoding.UTF8.GetBytes("MyProperty6"),
                Encoding.UTF8.GetBytes("MyProperty7"),
                Encoding.UTF8.GetBytes("MyProperty8"),
                Encoding.UTF8.GetBytes("MyProperty9"),
                };
            }

            public void Serialize(ref MessagePackWriter writer, StringKeySerializerTarget stringKeySerializerTarget, MessagePackSerializerOptions options)
            {
                if (stringKeySerializerTarget == null)
                {
                    writer.WriteNil();
                    return;
                }

                writer.WriteMapHeader(9);
                writer.WriteString(this.stringByteKeys[0]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty1);
                writer.WriteString(this.stringByteKeys[1]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty2);
                writer.WriteString(this.stringByteKeys[2]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty3);
                writer.WriteString(this.stringByteKeys[3]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty4);
                writer.WriteString(this.stringByteKeys[4]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty5);
                writer.WriteString(this.stringByteKeys[5]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty6);
                writer.WriteString(this.stringByteKeys[6]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty7);
                writer.WriteString(this.stringByteKeys[7]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty8);
                writer.WriteString(this.stringByteKeys[8]);
                writer.WriteInt32(stringKeySerializerTarget.MyProperty9);
            }

            public StringKeySerializerTarget Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                if (reader.TryReadNil())
                {
                    return null;
                }

                int num3 = reader.ReadMapHeader();
                int myProperty = 0;
                int myProperty2 = 0;
                int myProperty3 = 0;
                int myProperty4 = 0;
                int myProperty5 = 0;
                int myProperty6 = 0;
                int myProperty7 = 0;
                int myProperty8 = 0;
                int myProperty9 = 0;
                for (int i = 0; i < num3; i++)
                {
                    int num4;
                    ReadOnlySequence<byte> segment = reader.ReadStringSegment().Value;
                    bool arg_47_0 = this.keyMapping.TryGetValue(segment, out num4);
                    if (!arg_47_0)
                    {
                        reader.Skip();
                    }
                    else
                    {
                        switch (num4)
                        {
                            case 0:
                                myProperty = reader.ReadInt32();
                                break;
                            case 1:
                                myProperty2 = reader.ReadInt32();
                                break;
                            case 2:
                                myProperty3 = reader.ReadInt32();
                                break;
                            case 3:
                                myProperty4 = reader.ReadInt32();
                                break;
                            case 4:
                                myProperty5 = reader.ReadInt32();
                                break;
                            case 5:
                                myProperty6 = reader.ReadInt32();
                                break;
                            case 6:
                                myProperty7 = reader.ReadInt32();
                                break;
                            case 7:
                                myProperty8 = reader.ReadInt32();
                                break;
                            case 8:
                                myProperty9 = reader.ReadInt32();
                                break;
                            default:
                                reader.Skip();
                                break;
                        }
                    }
                }

                return new StringKeySerializerTarget
                {
                    MyProperty1 = myProperty,
                    MyProperty2 = myProperty2,
                    MyProperty3 = myProperty3,
                    MyProperty4 = myProperty4,
                    MyProperty5 = myProperty5,
                    MyProperty6 = myProperty6,
                    MyProperty7 = myProperty7,
                    MyProperty8 = myProperty8,
                    MyProperty9 = myProperty9,
                };
            }
        }

        public sealed class StringKeySerializerTargetFormatter_MpcGeneratedAutomata : newmsgpack::MessagePack.Formatters.IMessagePackFormatter<StringKeySerializerTarget>
        {
            private readonly newmsgpack::MessagePack.Internal.AutomataDictionary keyMapping;
            private readonly byte[][] stringByteKeys;

            public StringKeySerializerTargetFormatter_MpcGeneratedAutomata()
            {
                this.keyMapping = new newmsgpack::MessagePack.Internal.AutomataDictionary()
            {
                { "MyProperty1", 0 },
                { "MyProperty2", 1 },
                { "MyProperty3", 2 },
                { "MyProperty4", 3 },
                { "MyProperty5", 4 },
                { "MyProperty6", 5 },
                { "MyProperty7", 6 },
                { "MyProperty8", 7 },
                { "MyProperty9", 8 },
            };

                this.stringByteKeys = new byte[][]
                {
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty1"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty2"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty3"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty4"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty5"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty6"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty7"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty8"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty9"),
                };
            }

            public void Serialize(ref MessagePackWriter writer, global::PerfBenchmarkDotNet.StringKeySerializerTarget value, newmsgpack::MessagePack.MessagePackSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public global::PerfBenchmarkDotNet.StringKeySerializerTarget Deserialize(ref MessagePackReader reader, newmsgpack::MessagePack.MessagePackSerializerOptions options)
            {
                if (reader.TryReadNil())
                {
                    return null;
                }

                var length = reader.ReadMapHeader();

                var __MyProperty1__ = default(int);
                var __MyProperty2__ = default(int);
                var __MyProperty3__ = default(int);
                var __MyProperty4__ = default(int);
                var __MyProperty5__ = default(int);
                var __MyProperty6__ = default(int);
                var __MyProperty7__ = default(int);
                var __MyProperty8__ = default(int);
                var __MyProperty9__ = default(int);

                for (int i = 0; i < length; i++)
                {
                    ReadOnlySequence<byte> stringKey = reader.ReadStringSegment().Value;
                    int key;
                    if (!this.keyMapping.TryGetValue(stringKey, out key))
                    {
                        reader.Skip();
                        continue;
                    }

                    switch (key)
                    {
                        case 0:
                            __MyProperty1__ = reader.ReadInt32();
                            break;
                        case 1:
                            __MyProperty2__ = reader.ReadInt32();
                            break;
                        case 2:
                            __MyProperty3__ = reader.ReadInt32();
                            break;
                        case 3:
                            __MyProperty4__ = reader.ReadInt32();
                            break;
                        case 4:
                            __MyProperty5__ = reader.ReadInt32();
                            break;
                        case 5:
                            __MyProperty6__ = reader.ReadInt32();
                            break;
                        case 6:
                            __MyProperty7__ = reader.ReadInt32();
                            break;
                        case 7:
                            __MyProperty8__ = reader.ReadInt32();
                            break;
                        case 8:
                            __MyProperty9__ = reader.ReadInt32();
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }

                var ____result = new global::PerfBenchmarkDotNet.StringKeySerializerTarget();
                ____result.MyProperty1 = __MyProperty1__;
                ____result.MyProperty2 = __MyProperty2__;
                ____result.MyProperty3 = __MyProperty3__;
                ____result.MyProperty4 = __MyProperty4__;
                ____result.MyProperty5 = __MyProperty5__;
                ____result.MyProperty6 = __MyProperty6__;
                ____result.MyProperty7 = __MyProperty7__;
                ____result.MyProperty8 = __MyProperty8__;
                ____result.MyProperty9 = __MyProperty9__;
                return ____result;
            }
        }

        public sealed class StringKeySerializerTargetFormatter_MpcGeneratedDictionary : newmsgpack::MessagePack.Formatters.IMessagePackFormatter<StringKeySerializerTarget>
        {
            private readonly newmsgpack::MessagePack.Internal.ByteArrayStringHashTable keyMapping;
            private readonly byte[][] stringByteKeys;

            public StringKeySerializerTargetFormatter_MpcGeneratedDictionary()
            {
                this.keyMapping = new newmsgpack::MessagePack.Internal.ByteArrayStringHashTable(9)
            {
                { "MyProperty1", 0 },
                { "MyProperty2", 1 },
                { "MyProperty3", 2 },
                { "MyProperty4", 3 },
                { "MyProperty5", 4 },
                { "MyProperty6", 5 },
                { "MyProperty7", 6 },
                { "MyProperty8", 7 },
                { "MyProperty9", 8 },
            };

                this.stringByteKeys = new byte[][]
                {
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty1"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty2"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty3"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty4"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty5"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty6"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty7"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty8"),
                global::System.Text.Encoding.UTF8.GetBytes("MyProperty9"),
                };
            }

            public void Serialize(ref MessagePackWriter writer, global::PerfBenchmarkDotNet.StringKeySerializerTarget value, newmsgpack::MessagePack.MessagePackSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public global::PerfBenchmarkDotNet.StringKeySerializerTarget Deserialize(ref MessagePackReader reader, newmsgpack::MessagePack.MessagePackSerializerOptions options)
            {
                if (reader.TryReadNil())
                {
                    return null;
                }

                var length = reader.ReadMapHeader();

                var __MyProperty1__ = default(int);
                var __MyProperty2__ = default(int);
                var __MyProperty3__ = default(int);
                var __MyProperty4__ = default(int);
                var __MyProperty5__ = default(int);
                var __MyProperty6__ = default(int);
                var __MyProperty7__ = default(int);
                var __MyProperty8__ = default(int);
                var __MyProperty9__ = default(int);

                for (int i = 0; i < length; i++)
                {
                    ReadOnlySequence<byte> stringKey = reader.ReadStringSegment().Value;
                    int key;
                    if (!this.keyMapping.TryGetValue(stringKey, out key))
                    {
                        reader.Skip();
                        continue;
                    }

                    switch (key)
                    {
                        case 0:
                            __MyProperty1__ = reader.ReadInt32();
                            break;
                        case 1:
                            __MyProperty2__ = reader.ReadInt32();
                            break;
                        case 2:
                            __MyProperty3__ = reader.ReadInt32();
                            break;
                        case 3:
                            __MyProperty4__ = reader.ReadInt32();
                            break;
                        case 4:
                            __MyProperty5__ = reader.ReadInt32();
                            break;
                        case 5:
                            __MyProperty6__ = reader.ReadInt32();
                            break;
                        case 6:
                            __MyProperty7__ = reader.ReadInt32();
                            break;
                        case 7:
                            __MyProperty8__ = reader.ReadInt32();
                            break;
                        case 8:
                            __MyProperty9__ = reader.ReadInt32();
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }

                var ____result = new global::PerfBenchmarkDotNet.StringKeySerializerTarget();
                ____result.MyProperty1 = __MyProperty1__;
                ____result.MyProperty2 = __MyProperty2__;
                ____result.MyProperty3 = __MyProperty3__;
                ____result.MyProperty4 = __MyProperty4__;
                ____result.MyProperty5 = __MyProperty5__;
                ____result.MyProperty6 = __MyProperty6__;
                ____result.MyProperty7 = __MyProperty7__;
                ____result.MyProperty8 = __MyProperty8__;
                ____result.MyProperty9 = __MyProperty9__;
                return ____result;
            }
        }
    }
}

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace

