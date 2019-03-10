extern alias oldmsgpack;
extern alias newmsgpack;
using ProtoBuf;

namespace Benchmark.Models
{
    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class Error : IGenericEquality<Error>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? error_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public string error_name { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public string description { get; set; }

        public bool Equals(Error obj)
        {
            return
                error_id.TrueEquals(obj.error_id) &&
                error_name.TrueEqualsString(obj.error_name) &&
                description.TrueEqualsString(obj.description);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                error_id.TrueEquals((int?) obj.error_id) &&
                error_name.TrueEqualsString((string) obj.error_name) &&
                description.TrueEqualsString((string) obj.description);
        }
    }
}