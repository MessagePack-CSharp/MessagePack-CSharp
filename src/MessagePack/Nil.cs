using System;
using System.Buffers;

namespace MessagePack
{
#if MESSAGEPACK_INTERNAL
    internal
#else
    public
#endif
    struct Nil : IEquatable<Nil>
    {
        public static readonly Nil Default = new Nil();

        public override bool Equals(object obj)
        {
            return obj is Nil;
        }

        public bool Equals(Nil other)
        {
            return true;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return "()";
        }
    }
}
