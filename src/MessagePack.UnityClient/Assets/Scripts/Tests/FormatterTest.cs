using RuntimeUnitTestToolkit;
using SharedData;
using System;

namespace MessagePack.UnityClient.Tests
{
    public class FormatterTest
    {
        T Convert<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value));
        }

        public void PrimitiveFormatterTest()
        {
            Convert(Int32.MaxValue).Is(Int32.MaxValue);
            Convert(Double.MinValue).Is(Double.MinValue);
            Convert(DateTime.MinValue.ToUniversalTime()).Is(DateTime.MinValue.ToUniversalTime());
        }


        public void EnumFormatterTest()
        {
            Convert(UShortEnum.A).Is(UShortEnum.A);
            Convert(IntEnum.A).Is(IntEnum.A);
        }
    }
}