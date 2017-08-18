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
        static void SerializeWithLengthPrefixExt<T>(Stream stream, T data, IFormatterResolver resolver)
        {
            const int ExtTypeCode = 111; // sample ext code

            byte[] buffer = null;
            var formatter = resolver.GetFormatter<T>();
            var dataSize = formatter.Serialize(ref buffer, 0, data, resolver);

            var headerLength =MessagePackBinary.GetExtensionFormatHeaderLength(dataSize);

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

        [Fact]
        public void Deserialize()
        {
            var testData = new SharedData.MyClass { MyProperty1 = int.MaxValue, MyProperty2 = int.MaxValue, MyProperty3 = int.MaxValue };

            var ms = new MemoryStream();

            SerializeWithLengthPrefixExt(ms, testData, Resolvers.ContractlessStandardResolver.Instance);

            ms.Position = 0;
            var data = DeserializeWithLengthPrefixExt<MyClass>(ms, Resolvers.ContractlessStandardResolver.Instance);

        }
    }
}
