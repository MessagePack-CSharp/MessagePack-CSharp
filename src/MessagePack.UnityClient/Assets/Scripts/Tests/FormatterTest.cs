﻿using System;
using RuntimeUnitTestToolkit;
using SharedData;

namespace MessagePack.UnityClient.Tests
{
    public class FormatterTest
    {
        private readonly MessagePackSerializer serializer = new MessagePackSerializer(MsgPackUnsafeDefaultResolver.Instance);

        private T Convert<T>(T value)
        {
            return this.serializer.Deserialize<T>(this.serializer.Serialize(value));
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