using MessagePack.Formatters;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.ReactivePropertyExtension
{
    // [Mode, Value]
    public class ReactivePropertySlimFormatter<T> : IMessagePackFormatter<ReactivePropertySlim<T>>
    {
        public int Serialize(ref byte[] bytes, int offset, ReactivePropertySlim<T> value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;

                offset += MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 2);

                offset += MessagePackBinary.WriteInt32(ref bytes, offset, ReactivePropertySlimModeMapper.ToReactivePropertySlimModeInt(value));
                offset += formatterResolver.GetFormatterWithVerify<T>().Serialize(ref bytes, offset, value.Value, formatterResolver);

                return offset - startOffset;
            }
        }

        public ReactivePropertySlim<T> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                var startOffset = offset;

                var length = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
                offset += readSize;
                if (length != 2) throw new InvalidOperationException("Invalid ReactivePropertySlim data.");

                var mode = (ReactivePropertyMode)MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                offset += readSize;

                var v = formatterResolver.GetFormatterWithVerify<T>().Deserialize(bytes, offset, formatterResolver, out readSize);
                offset += readSize;

                readSize = offset - startOffset;

                return new ReactivePropertySlim<T>(v, mode);
            }

        }
    }

    public static class ReactivePropertySlimModeMapper
    {
        internal static int ToReactivePropertySlimModeInt<T>(global::Reactive.Bindings.ReactivePropertySlim<T> reactiveProperty)
        {
            var mode = ReactivePropertyMode.None;
            if (reactiveProperty.IsDistinctUntilChanged)
            {
                mode |= ReactivePropertyMode.DistinctUntilChanged;
            }
            if (reactiveProperty.IsRaiseLatestValueOnSubscribe)
            {
                mode |= ReactivePropertyMode.RaiseLatestValueOnSubscribe;
            }
            return (int)mode;
        }
    }
}
