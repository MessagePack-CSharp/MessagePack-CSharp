namespace MessagePack
{
    public struct ExtensionHeader
    {
        public sbyte TypeCode { get; private set; }
        public uint Length { get; private set; }

        public ExtensionHeader(sbyte typeCode, uint length)
        {
            TypeCode = typeCode;
            Length = length;
        }

        public ExtensionHeader(sbyte typeCode, int length)
        {
            TypeCode = typeCode;
            Length = (uint)length;
        }
    }
}
