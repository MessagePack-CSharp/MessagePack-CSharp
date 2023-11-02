// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Nerdbank.Streams;
using Xunit;

namespace MessagePack.Tests
{
    public class PrimitivelikeFormatterTest
    {
#if !(MESSAGEPACK_FORCE_AOT || ENABLE_IL2CPP)
        [Fact]
        public void CanResolve()
        {
            var ex = Assert.Throws<MessagePackSerializationException>(() =>
            {
                MessagePackSerializer.Serialize(new MyDateTimeResolverTest() { MyProperty1 = DateTime.Now }, PrimitivelikeResolver.Options);
            });

            Assert.IsType<NotImplementedException>(ex.InnerException);
        }
#endif

        [Fact]
        public void NativeDateTime()
        {
            var referenceContext = new MsgPack.Serialization.SerializationContext(MsgPack.PackerCompatibilityOptions.Classic)
            {
                DefaultDateTimeConversionMethod = MsgPack.Serialization.DateTimeConversionMethod.Native,
            };

            DateTime now = DateTime.Now;

            MsgPack.Serialization.MessagePackSerializer<DateTime> serializer = referenceContext.GetSerializer<DateTime>();

            var a = MessagePackSerializer.Serialize(now, NativeDateTimeResolver.Options);
            var b = serializer.PackSingleObject(now);

            a.Is(b);

            DateTime dt1 = MessagePackSerializer.Deserialize<DateTime>(a, NativeDateTimeResolver.Options);
            DateTime dt2 = serializer.UnpackSingleObject(b);

            dt1.Is(dt2);
        }

        [Fact]
        public void OldSpecString()
        {
            var referenceContext = new MsgPack.Serialization.SerializationContext(MsgPack.PackerCompatibilityOptions.Classic)
            {
                DefaultDateTimeConversionMethod = MsgPack.Serialization.DateTimeConversionMethod.Native,
            };

            var data = "あいうえおabcdefgかきくけこあいうえおabcdefgかきくけこあいうえおabcdefgかきくけこあいうえおabcdefgかきくけこ"; // Japanese

            MsgPack.Serialization.MessagePackSerializer<string> serializer = referenceContext.GetSerializer<string>();

            using (var sequence = new Sequence<byte>())
            {
                var oldSpecWriter = new MessagePackWriter(sequence) { OldSpec = true };
                MessagePackSerializer.Serialize(ref oldSpecWriter, data);
                oldSpecWriter.Flush();
                var a = sequence.AsReadOnlySequence.ToArray();
                var b = serializer.PackSingleObject(data);

                a.Is(b);

                var oldSpecReader = new MessagePackReader(sequence.AsReadOnlySequence);
                var r1 = MessagePackSerializer.Deserialize<string>(ref oldSpecReader);
                var r2 = serializer.UnpackSingleObject(b);

                r1.Is(r2);
            }
        }

        [Fact]
        public void OldSpecBinary()
        {
            var referenceContext = new MsgPack.Serialization.SerializationContext(MsgPack.PackerCompatibilityOptions.Classic)
            {
                DefaultDateTimeConversionMethod = MsgPack.Serialization.DateTimeConversionMethod.Native,
            };

            var data = Enumerable.Range(0, 10000).Select(x => (byte)1).ToArray();

            MsgPack.Serialization.MessagePackSerializer<byte[]> serializer = referenceContext.GetSerializer<byte[]>();

            using (var sequence = new Sequence<byte>())
            {
                var oldSpecWriter = new MessagePackWriter(sequence) { OldSpec = true };
                MessagePackSerializer.Serialize(ref oldSpecWriter, data);
                oldSpecWriter.Flush();
                var a = sequence.AsReadOnlySequence.ToArray();
                var b = serializer.PackSingleObject(data);

                a.Is(b);

                var oldSpecReader = new MessagePackReader(sequence.AsReadOnlySequence);
                var r1 = MessagePackSerializer.Deserialize<byte[]>(ref oldSpecReader);
                var r2 = serializer.UnpackSingleObject(b);

                r1.Is(r2);
            }
        }

        public class PrimitivelikeResolver : IFormatterResolver
        {
            /// <summary>
            /// An <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
            /// </summary>
            public static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard.WithResolver(new PrimitivelikeResolver());

            public IMessagePackFormatter<T> GetFormatter<T>()
            {
                if (typeof(T) == typeof(DateTime))
                {
                    return (IMessagePackFormatter<T>)new DummyDateTimeFormatter();
                }

                return StandardResolver.Instance.GetFormatter<T>();
            }
        }

        public class DummyDateTimeFormatter : IMessagePackFormatter<DateTime>
        {
            public DateTime Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public void Serialize(ref MessagePackWriter writer, DateTime value, MessagePackSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }

        [MessagePackObject]
        public class MyDateTimeResolverTest
        {
            [Key(0)]
            public DateTime MyProperty1 { get; set; }
        }
    }
}
