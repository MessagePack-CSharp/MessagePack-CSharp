using SharedData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class StreamStrictTest
    {
        private MessagePackSerializer serializer = new MessagePackSerializer();

        static void SerializeWithLengthPrefixExt<T>(Stream stream, T data, IFormatterResolver resolver)
        {
            const int ExtTypeCode = 111; // sample ext code

            byte[] buffer = null;
            var formatter = resolver.GetFormatter<T>();
            var dataSize = formatter.Serialize(ref buffer, 0, data, resolver);

            var headerLength = MessagePackBinary.GetExtensionFormatHeaderLength(dataSize);

            MessagePackBinary.EnsureCapacity(ref buffer, 0, headerLength);
            Buffer.BlockCopy(buffer, 0, buffer, headerLength, dataSize);
            MessagePackBinary.WriteExtensionFormatHeader(ref buffer, 0, ExtTypeCode, dataSize);

            stream.Write(buffer, 0, dataSize + headerLength);
        }

        static T DeserializeWithLengthPrefixExt<T>(Stream stream, IFormatterResolver resolver)
        {
            const int ExtTypeCode = 111; // sample ext code

            var header = MessagePackBinary.ReadExtensionFormatHeader(stream);
            if (header.TypeCode == ExtTypeCode)
            {
                // memo, read fully
                var buffer = new byte[1024];
                stream.Read(buffer, 0, (int)header.Length);

                return resolver.GetFormatter<T>().Deserialize(buffer, 0, resolver, out var _);
            }
            else
            {
                throw new Exception();
            }
        }

        T DeserializeWithLengthPrefixExt2<T>(Stream stream, IFormatterResolver resolver)
        {
            const int ExtTypeCode = 111; // sample ext code

            var header = MessagePackBinary.ReadExtensionFormat(stream);
            if (header.TypeCode == ExtTypeCode)
            {
                return serializer.Deserialize<T>(header.Data, resolver);
            }
            else
            {
                throw new Exception();
            }
        }

        [Fact]
        public void Deserialize()
        {
            var testData = new SharedData.MyClass { MyProperty1 = int.MaxValue, MyProperty2 = int.MaxValue, MyProperty3 = int.MaxValue };

            var ms = new MemoryStream();

            SerializeWithLengthPrefixExt(ms, testData, Resolvers.ContractlessStandardResolver.Instance);

            ms.Position = 0;
            var data = DeserializeWithLengthPrefixExt<MyClass>(ms, Resolvers.ContractlessStandardResolver.Instance);
        }

        [Fact]
        public void Deserialize2()
        {
            var testData = new SharedData.MyClass { MyProperty1 = int.MaxValue, MyProperty2 = 99, MyProperty3 = 1341 };

            Stream ms = new MemoryStream();

            SerializeWithLengthPrefixExt(ms, testData, Resolvers.ContractlessStandardResolver.Instance);

            ms.Position = 0;

            ms = new FixedSizeReadStream(ms, 2);

            var data = DeserializeWithLengthPrefixExt2<MyClass>(ms, Resolvers.ContractlessStandardResolver.Instance);
        }
    }

    public class FixedSizeReadStream : Stream
    {
        readonly Stream stream;
        readonly int readSize;

        public FixedSizeReadStream(Stream stream, int readSize)
        {
            this.stream = stream;
            this.readSize = readSize;
        }

        public override bool CanRead => throw new NotImplementedException();

        public override bool CanSeek => throw new NotImplementedException();

        public override bool CanWrite => throw new NotImplementedException();

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return stream.Read(buffer, offset, Math.Min(readSize, count));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
