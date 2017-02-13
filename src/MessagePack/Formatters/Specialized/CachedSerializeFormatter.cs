using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Formatters.Specialized
{
    //public class CachedSerializeFormatter<T> : IMessagePackFormatter<T>
    //    where T : class
    //{
    //    object gate = new object();
    //    byte[] cacheBuffer = null;

    //    public int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver typeResolver)
    //    {

    //        lock (gate)
    //        {
    //            if (cacheBuffer != null)
    //            {
    //                Buffer.BlockCopy(cacheBuffer, 0, bytes, offset, cacheBuffer.Length);
    //            }
    //            else
    //            {

    //            }
    //        }
    //        throw new NotImplementedException();
    //    }

    //    public T Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int byteSize)
    //    {
    //        throw new NotImplementedException();


    //        //var formatter = typeResolver.GetFormatter<T>();
    //        //if (formatter == this)
    //        //{
    //        //    // use DefaultResolver to avoid circular reference.
    //        //    throw new NotImplementedException();
    //        //}
    //        //else
    //        //{
    //        //    var huga = formatter.Deserialize(bytes, offset, typeResolver, out byteSize);
    //        //}
    //    }
    //}

    public class BooleanFormatter : IMessagePackFormatter<bool>
    {
        public int Serialize(ref byte[] bytes, int offset, bool value, IFormatterResolver typeResolver)
        {
            return MessagePackBinary.WriteBoolean(ref bytes, offset, value);
        }

        public bool Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            return MessagePackBinary.ReadBoolean(bytes, offset, out readSize);
        }
    }
}
