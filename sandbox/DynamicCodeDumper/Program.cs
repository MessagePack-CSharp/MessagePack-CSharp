// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Internal;
using MessagePack.Resolvers;
using Nerdbank.Streams;
using SharedData;

namespace DynamicCodeDumper
{
    public class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                // var mi = AutomataKeyGen.GetGetKeyMethod();
                // mi.Invoke(null, new[] {

                ////DynamicObjectResolver.Instance.GetFormatter<ArrayOptimizeClass>();
                ////DynamicObjectResolver.Instance.GetFormatter<Empty1>();
                ////DynamicObjectResolver.Instance.GetFormatter<Empty2>();

                ////DynamicObjectResolver.Instance.GetFormatter<NonEmpty1>();
                ////DynamicObjectResolver.Instance.GetFormatter<NonEmpty2>();
                ////DynamicObjectResolver.Instance.GetFormatter<FirstSimpleData>();
                ////DynamicObjectResolver.Instance.GetFormatter<Version0>();
                ////DynamicObjectResolver.Instance.GetFormatter<Version1>();
                ////DynamicObjectResolver.Instance.GetFormatter<Version2>();
                ////DynamicObjectResolver.Instance.GetFormatter<SimpleIntKeyData>();
                ////DynamicObjectResolver.Instance.GetFormatter<SimpleStringKeyData>();
                ////DynamicObjectResolver.Instance.GetFormatter<SimpleStringKeyData2>();
                ////DynamicObjectResolver.Instance.GetFormatter<StringKeySerializerTarget>();
                ////DynamicObjectResolver.Instance.GetFormatter<LongestString>();
                IMessagePackFormatter<MyClass> f = DynamicObjectResolverAllowPrivate.Instance.GetFormatter<MyClass>();
                ////DynamicObjectResolver.Instance.GetFormatter<StringKeySerializerTargetBinary>();
                ////DynamicObjectResolver.Instance.GetFormatter<Callback1>();
                ////DynamicObjectResolver.Instance.GetFormatter<Callback1_2>();
                ////DynamicObjectResolver.Instance.GetFormatter<Callback2>();
                ////DynamicObjectResolver.Instance.GetFormatter<Callback2_2>();

                ////DynamicUnionResolver.Instance.GetFormatter<IHogeMoge>();
                ////DynamicUnionResolver.Instance.GetFormatter<IUnionChecker>();
                ////DynamicUnionResolver.Instance.GetFormatter<IUnionChecker2>();

                ////DynamicUnionResolver.Instance.GetFormatter<RootUnionType>();

                ////DynamicEnumResolver.Instance.GetFormatter<IntEnum>();
                ////DynamicEnumResolver.Instance.GetFormatter<ShortEnum>();

                ////DynamicContractlessObjectResolver.Instance.GetFormatter<ContractlessConstructorCheck>();
                ////DynamicContractlessObjectResolver.Instance.GetFormatter<Contractless2>();

                ////DynamicContractlessObjectResolver.Instance.GetFormatter<EntityBase>();

                using (var sequence = new Sequence<byte>())
                {
                    var sequenceWriter = new MessagePackWriter(sequence);
                    f.Serialize(ref sequenceWriter, new MyClass { MyProperty1 = 100, MyProperty2 = "foo" }, EmptyResolver.Options);
                    sequenceWriter.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                AssemblyBuilder a1 = DynamicObjectResolver.Instance.Save();
                AssemblyBuilder a2 = DynamicUnionResolver.Instance.Save();
                AssemblyBuilder a3 = DynamicEnumResolver.Instance.Save();
                AssemblyBuilder a4 = DynamicContractlessObjectResolver.Instance.Save();
                ////var a5 = AutomataKeyGen.Save();

                ////Verify(a5);
            }
            ////Verify(a1, a2, a3, a4);
        }

        private static void Verify(params AssemblyBuilder[] builders)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\x64\PEVerify.exe");

            foreach (AssemblyBuilder targetDll in builders)
            {
                var psi = new ProcessStartInfo(path, targetDll.GetName().Name + ".dll")
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };

                var p = Process.Start(psi);
                var data = p.StandardOutput.ReadToEnd();
                Console.WriteLine(data);
            }
        }

        private class EmptyResolver : IFormatterResolver
        {
            internal static readonly MessagePackSerializerOptions Options = new MessagePackSerializerOptions(new EmptyResolver());

            public IMessagePackFormatter<T> GetFormatter<T>() => null;
        }
    }

    [MessagePackObject]
    public class MyClass
    {
        [Key(0)]
        [MessagePackFormatter(typeof(Int_x10Formatter))]
        public int MyProperty1 { get; set; }

        [Key(1)]
        [MessagePackFormatter(typeof(String_x2Formatter))]
        public string MyProperty2 { get; set; }

#pragma warning disable SA1306 // Field names should begin with lower-case letter
        [Key(2)]
        private int Foo;
#pragma warning restore SA1306 // Field names should begin with lower-case letter

        public void SetFoo(int f)
        {
            this.Foo = f;
        }

        public int GetFoo()
        {
            return this.Foo;
        }
    }

    public class Int_x10Formatter : IMessagePackFormatter<int>
    {
        public int Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadInt32() * 10;
        }

        public void Serialize(ref MessagePackWriter writer, int value, MessagePackSerializerOptions options)
        {
            writer.WriteInt32(value * 10);
        }
    }

    public class String_x2Formatter : IMessagePackFormatter<string>
    {
        public string Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var s = reader.ReadString();
            return s + s;
        }

        public void Serialize(ref MessagePackWriter writer, string value, MessagePackSerializerOptions options)
        {
            writer.Write(value + value);
        }
    }

    [MessagePackObject(true)]
    public struct Callback2_2 : IMessagePackSerializationCallbackReceiver
    {
        [Key(0)]
        public int X { get; set; }

        public static bool CalledAfter = false;

        public Callback2_2(int x)
            : this(x, () => { }, () => { })
        {
        }

        private Action onBefore;
        private Action onAfter;

        public Callback2_2(int x, Action onBefore, Action onAfter)
        {
            this.X = x;
            this.onBefore = onBefore;
            this.onAfter = onAfter;
        }

        void IMessagePackSerializationCallbackReceiver.OnBeforeSerialize()
        {
            this.onBefore();
        }

        void IMessagePackSerializationCallbackReceiver.OnAfterDeserialize()
        {
            CalledAfter = true;
        }
    }

    [Union(0, typeof(HogeMoge1))]
    [Union(1, typeof(HogeMoge2))]
    public interface IHogeMoge
    {
    }

    public class HogeMoge1 : IHogeMoge
    {
    }

    public class HogeMoge2 : IHogeMoge
    {
    }

    public class EmptyContractless
    {
    }

    [MessagePackObject(true)]
    public class SimpleStringKeyData2
    {
        public int MyProperty1 { get; set; }

        public int MyProperty2 { get; set; }
    }

    [MessagePack.MessagePackObject(true)]
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

    [MessagePack.MessagePackObject(true)]
    public class StringKeySerializerTargetBinary
    {
        public int MyProperty12 { get; set; }

        public int MyProperty6 { get; set; }

        public int MyProperty45 { get; set; }

        public int MyProperty14 { get; set; }

        public int MyProperty7 { get; set; }

        public int MyProperty72 { get; set; }

        public int MyProperty99 { get; set; }

        public int MyProperty88 { get; set; }

        public int MyProperty1 { get; set; }

        public int MyProperty2 { get; set; }

        public int MyProperty5 { get; set; }

        public int MyProperty8 { get; set; }

        public int MyProperty9 { get; set; }

        public int MyProperty70 { get; set; }

        public int MyProperty16 { get; set; }

        public int MyProperty16MyProperty16MyProperty16MyProperty16 { get; set; }

        public int MyProperty23 { get; set; }

        public int MyProperty3 { get; set; }

        public int MyProperty10 { get; set; }

        public int MyProperty67 { get; set; }

        public int MyProperty68 { get; set; }

        public int MyProperty4 { get; set; }

        public int FooPropertyWithBazBaz { get; set; }

        public int MyProperty69 { get; set; }

        public int MyProperty71 { get; set; }
    }

    [MessagePack.MessagePackObject(true)]
    public class LongestString
    {
        public int MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1 { get; set; }

        public int MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty2 { get; set; }

        public int MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty1MyProperty2MyProperty { get; set; }

        public int OAFADFZEWFSDFSDFKSLJFWEFNWOZFUSEWWEFWEWFFFFFFFFFFFFFFZFEWBFOWUEGWHOUDGSOGUDSZNOFRWEUFWGOWHOGHWOG000000000000000000000000000000000000000HOGZ { get; set; }
    }

    [MessagePack.MessagePackObject(true)]
    public class Dup
    {
        public int ABCDEFGH { get; set; }

        public int ABCDEFGHIJKL { get; set; }

        public int ABCDEFGHIJKO { get; set; }
    }

    public class Contractless2
    {
        public int MyProperty { get; set; }

        public string MyProperty2 { get; set; }
    }

    public interface IEntity
    {
        string Name { get; }
    }

    public class Event : IEntity
    {
        public Event(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }

    public class Holder
    {
        public Holder(IEntity entity)
        {
            this.Entity = entity;
        }

        public IEntity Entity { get; }
    }

    public abstract class EntityBase
    {
        public string Name { get; }

        public EntityBase()
        {
        }
    }

    ////public class SimulateDup : MessagePack.Formatters.IMessagePackFormatter<Dup>
    ////{
    ////    public unsafe Dup Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
    ////    {
    ////        if (MessagePackBinary.IsNil(bytes, offset))
    ////        {
    ////            readSize = 1;
    ////            return null;
    ////        }

    ////        var startOffset = offset;

    ////        var len = MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
    ////        offset += readSize;

    ////        int aBCDEFGH = 0;
    ////        int aBCDEFGHIJKL = 0;
    ////        int aBCDEFGHIJKO = 0;

    ////        // ---isStringKey

    ////        ulong key;
    ////        ArraySegment<byte> arraySegment;
    ////        byte* p;
    ////        int rest;

    ////        fixed (byte* buffer = &bytes[0])
    ////        {
    ////            for (int i = 0; i < len; i++)
    ////            {
    ////                arraySegment = MessagePackBinary.ReadStringSegment(bytes, offset, out readSize);
    ////                offset += readSize;

    ////                p = buffer + arraySegment.Offset;
    ////                rest = arraySegment.Count;

    ////                if (rest == 0) goto LOOP_END;

    ////                key = AutomataKeyGen.GetKey(ref p, ref rest);
    ////                if (rest == 0)
    ////                {
    ////                    if (key == 5208208757389214273L)
    ////                    {
    ////                        aBCDEFGH = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
    ////                        goto LOOP_END;
    ////                    }

    ////                    goto READ_NEXT;
    ////                }

    ////                if (key == 5208208757389214273L)
    ////                {
    ////                    key = AutomataKeyGen.GetKey(ref p, ref rest);
    ////                    if (rest == 0)
    ////                    {
    ////                        if (key == 1280002633L)
    ////                        {
    ////                            aBCDEFGHIJKL = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
    ////                            goto LOOP_END;
    ////                        }

    ////                        if (key == 1330334281L)
    ////                        {
    ////                            aBCDEFGHIJKO = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
    ////                            goto LOOP_END;
    ////                        }
    ////                    }
    ////                }

    ////                READ_NEXT:
    ////                readSize = MessagePackBinary.ReadNextBlock(bytes, offset);

    ////                LOOP_END:
    ////                offset += readSize;
    ////                continue;
    ////            }
    ////        }

    ////        // --- end

    ////        return new Dup
    ////        {
    ////            ABCDEFGH = aBCDEFGH,
    ////            ABCDEFGHIJKL = aBCDEFGHIJKL,
    ////            ABCDEFGHIJKO = aBCDEFGHIJKO
    ////        };
    ////    }

    ////    public int Serialize(ref byte[] bytes, int offset, Dup value, IFormatterResolver formatterResolver)
    ////    {
    ////        throw new NotImplementedException();
    ////    }
    ////}
}
