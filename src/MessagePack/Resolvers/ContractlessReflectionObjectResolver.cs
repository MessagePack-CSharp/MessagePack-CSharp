#if NETSTANDARD

using MessagePack.Formatters;
using MessagePack.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Resolvers
{
    // MEMO: Not completely implemented.

    //public static class ContractlessReflectionObjectResolver
    //{
    //    // TODO:CamelCase Option? AllowPrivate?
    //    public static readonly IFormatterResolver Default = new DefaultResolver();
    //    public static readonly IFormatterResolver Contractless = new ContractlessResolver();
    //    public static readonly IFormatterResolver ContractlessForceStringKey = new ContractlessForceStringResolver();

    //    class DefaultResolver : IFormatterResolver
    //    {
    //        const bool ForceStringKey = false;
    //        const bool Contractless = false;
    //        const bool AllowPrivate = false;

    //        public IMessagePackFormatter<T> GetFormatter<T>()
    //        {
    //            return Cache<T>.formatter;
    //        }

    //        static class Cache<T>
    //        {
    //            public static readonly IMessagePackFormatter<T> formatter;

    //            static Cache()
    //            {
    //                var metaInfo = ObjectSerializationInfo.CreateOrNull(typeof(T), ForceStringKey, Contractless, AllowPrivate);
    //                if (metaInfo != null)
    //                {
    //                    formatter = new ReflectionObjectFormatter<T>(metaInfo);
    //                }
    //            }
    //        }
    //    }

    //    class ContractlessResolver : IFormatterResolver
    //    {
    //        const bool ForceStringKey = false;
    //        const bool Contractless = true;
    //        const bool AllowPrivate = false;

    //        public IMessagePackFormatter<T> GetFormatter<T>()
    //        {
    //            return Cache<T>.formatter;
    //        }

    //        static class Cache<T>
    //        {
    //            public static readonly IMessagePackFormatter<T> formatter;

    //            static Cache()
    //            {
    //                var metaInfo = ObjectSerializationInfo.CreateOrNull(typeof(T), ForceStringKey, Contractless, AllowPrivate);
    //                if (metaInfo != null)
    //                {
    //                    formatter = new ReflectionObjectFormatter<T>(metaInfo);
    //                }
    //            }
    //        }
    //    }

    //    class ContractlessForceStringResolver : IFormatterResolver
    //    {
    //        const bool ForceStringKey = true;
    //        const bool Contractless = true;
    //        const bool AllowPrivate = false;

    //        public IMessagePackFormatter<T> GetFormatter<T>()
    //        {
    //            return Cache<T>.formatter;
    //        }

    //        static class Cache<T>
    //        {
    //            public static readonly IMessagePackFormatter<T> formatter;

    //            static Cache()
    //            {
    //                var metaInfo = ObjectSerializationInfo.CreateOrNull(typeof(T), ForceStringKey, Contractless, AllowPrivate);
    //                if (metaInfo != null)
    //                {
    //                    formatter = new ReflectionObjectFormatter<T>(metaInfo);
    //                }
    //            }
    //        }
    //    }

    //}


    //public class ReflectionObjectFormatter<T> : IMessagePackFormatter<T>
    //{
    //    readonly ObjectSerializationInfo metaInfo;

    //    // for write
    //    readonly byte[][] writeMemberNames;
    //    readonly ObjectSerializationInfo.EmittableMember[] writeMembers;

    //    // for read
    //    readonly int[] constructorParameterIndexes;
    //    readonly AutomataDictionary mapMemberDictionary;
    //    readonly ObjectSerializationInfo.EmittableMember[] readMembers;


    //    internal ReflectionObjectFormatter(ObjectSerializationInfo metaInfo)
    //    {
    //        this.metaInfo = metaInfo;

    //        // for write
    //        {
    //            var memberNameList = new List<byte[]>(metaInfo.Members.Length);
    //            var emmitableMemberList = new List<ObjectSerializationInfo.EmittableMember>(metaInfo.Members.Length);
    //            foreach (var item in metaInfo.Members)
    //            {
    //                if (item.IsWritable)
    //                {
    //                    emmitableMemberList.Add(item);
    //                    memberNameList.Add(Encoding.UTF8.GetBytes(item.Name));
    //                }
    //            }
    //            this.writeMemberNames = memberNameList.ToArray();
    //            this.writeMembers = emmitableMemberList.ToArray();
    //        }
    //        // for read
    //        {
    //            var automata = new AutomataDictionary();
    //            var emmitableMemberList = new List<ObjectSerializationInfo.EmittableMember>(metaInfo.Members.Length);
    //            int index = 0;
    //            foreach (var item in metaInfo.Members)
    //            {
    //                if (item.IsReadable)
    //                {
    //                    emmitableMemberList.Add(item);
    //                    automata.Add(item.Name, index++);
    //                }
    //            }
    //            this.readMembers = emmitableMemberList.ToArray();
    //            this.mapMemberDictionary = automata;
    //        }
    //    }

    //    public int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver)
    //    {
    //        // reduce generic method size, avoid write code in <T> type.
    //        if (metaInfo.IsIntKey)
    //        {
    //            return ReflectionObjectFormatterHelper.WriteArraySerialize(metaInfo, writeMembers, ref bytes, offset, value, formatterResolver);
    //        }
    //        else
    //        {
    //            return ReflectionObjectFormatterHelper.WriteMapSerialize(metaInfo, writeMembers, writeMemberNames, ref bytes, offset, value, formatterResolver);
    //        }
    //    }

    //    public T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
    //    {
    //        return (T)ReflectionObjectFormatterHelper.Deserialize(metaInfo, readMembers, constructorParameterIndexes, mapMemberDictionary, bytes, offset, formatterResolver, out readSize);
    //    }
    //}

    //internal static class ReflectionObjectFormatterHelper
    //{
    //    internal static int WriteArraySerialize(ObjectSerializationInfo metaInfo, ObjectSerializationInfo.EmittableMember[] writeMembers, ref byte[] bytes, int offset, object value, IFormatterResolver formatterResolver)
    //    {
    //        var startOffset = offset;

    //        offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, writeMembers.Length);
    //        foreach (var item in metaInfo.Members)
    //        {
    //            if (item == null)
    //            {
    //                offset += MessagePackBinary.WriteNil(ref bytes, offset);
    //            }
    //            else
    //            {
    //                var memberValue = item.ReflectionLoadValue(value);
    //                offset += MessagePackSerializer.NonGeneric.Serialize(item.Type, ref bytes, offset, memberValue, formatterResolver);
    //            }
    //        }

    //        return offset - startOffset;
    //    }

    //    internal static int WriteMapSerialize(ObjectSerializationInfo metaInfo, ObjectSerializationInfo.EmittableMember[] writeMembers, byte[][] memberNames, ref byte[] bytes, int offset, object value, IFormatterResolver formatterResolver)
    //    {
    //        var startOffset = offset;

    //        offset += MessagePackBinary.WriteMapHeader(ref bytes, offset, writeMembers.Length);

    //        for (int i = 0; i < writeMembers.Length; i++)
    //        {
    //            offset += MessagePackBinary.WriteStringBytes(ref bytes, offset, memberNames[i]);
    //            var memberValue = writeMembers[i].ReflectionLoadValue(value);
    //            offset += MessagePackSerializer.NonGeneric.Serialize(writeMembers[i].Type, ref bytes, offset, memberValue, formatterResolver);
    //        }

    //        return offset - startOffset;
    //    }

    //    internal static object Deserialize(ObjectSerializationInfo metaInfo, ObjectSerializationInfo.EmittableMember[] readMembers, int[] constructorParameterIndexes, AutomataDictionary mapMemberDictionary, byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
    //    {
    //        var startOffset = offset;
    //        object[] parameters = null;

    //        var headerType = MessagePackBinary.GetMessagePackType(bytes, offset);
    //        if (headerType == MessagePackType.Nil)
    //        {
    //            readSize = 1;
    //            return null;
    //        }
    //        else if (headerType == MessagePackType.Array)
    //        {
    //            var arraySize = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
    //            offset += readSize;

    //            // ReadValues
    //            parameters = new object[arraySize];
    //            for (int i = 0; i < arraySize; i++)
    //            {
    //                var info = readMembers[i];
    //                if (info != null)
    //                {
    //                    parameters[i] = MessagePackSerializer.NonGeneric.Deserialize(info.Type, bytes, offset, formatterResolver, out readSize);
    //                    offset += readSize;
    //                }
    //                else
    //                {
    //                    offset += MessagePackBinary.ReadNextBlock(bytes, offset);
    //                }
    //            }
    //        }
    //        else if (headerType == MessagePackType.Map)
    //        {
    //            var mapSize = MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
    //            offset += readSize;

    //            // ReadValues
    //            parameters = new object[mapSize];
    //            for (int i = 0; i < mapSize; i++)
    //            {
    //                var rawPropName = MessagePackBinary.ReadStringSegment(bytes, offset, out readSize);
    //                offset += readSize;

    //                int index;
    //                if (mapMemberDictionary.TryGetValue(rawPropName.Array, rawPropName.Offset, rawPropName.Count, out index))
    //                {
    //                    var info = readMembers[index];
    //                    parameters[index] = MessagePackSerializer.NonGeneric.Deserialize(info.Type, bytes, offset, formatterResolver, out readSize);
    //                    offset += readSize;
    //                }
    //                else
    //                {
    //                    offset += MessagePackBinary.ReadNextBlock(bytes, offset);
    //                }
    //            }
    //        }
    //        else
    //        {
    //            throw new InvalidOperationException("Invalid MessagePackType:" + MessagePackCode.ToFormatName(bytes[offset]));
    //        }

    //        // CreateObject
    //        object result = null;
    //        if (constructorParameterIndexes.Length == 0)
    //        {
    //            result = Activator.CreateInstance(metaInfo.Type);
    //        }
    //        else
    //        {
    //            var args = new object[constructorParameterIndexes.Length];
    //            for (int i = 0; i < constructorParameterIndexes.Length; i++)
    //            {
    //                args[i] = parameters[constructorParameterIndexes[i]];
    //            }

    //            result = Activator.CreateInstance(metaInfo.Type, args);
    //        }

    //        // SetMembers
    //        for (int i = 0; i < readMembers.Length; i++)
    //        {
    //            var info = readMembers[i];
    //            if (info != null)
    //            {
    //                info.ReflectionStoreValue(result, parameters[i]);
    //            }
    //        }

    //        readSize = offset - startOffset;
    //        return result;
    //    }
    //}
}

#endif