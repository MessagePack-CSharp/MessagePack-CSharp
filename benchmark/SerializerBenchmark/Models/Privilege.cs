extern alias oldmsgpack;
extern alias newmsgpack;
using ProtoBuf;

namespace Benchmark.Models
{
    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class Privilege : IGenericEquality<Privilege>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public string short_description { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public string description { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public int? reputation { get; set; }

        public bool Equals(Privilege obj)
        {
            return
                description.TrueEqualsString(obj.description) &&
                reputation.TrueEquals(obj.reputation) &&
                short_description.TrueEqualsString(obj.short_description);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                description.TrueEqualsString((string) obj.description) &&
                reputation.TrueEquals((int?) obj.reputation) &&
                short_description.TrueEqualsString((string) obj.short_description);
        }
    }
}