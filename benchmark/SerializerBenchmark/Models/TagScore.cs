extern alias oldmsgpack;
extern alias newmsgpack;
using ProtoBuf;

namespace Benchmark.Models
{
    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class TagScore : IGenericEquality<TagScore>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public ShallowUser user { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public int? score { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public int? post_count { get; set; }

        public bool Equals(TagScore obj)
        {
            return
                post_count.TrueEquals(obj.post_count) &&
                score.TrueEquals(obj.score) &&
                user.TrueEquals(obj.user);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                post_count.TrueEquals((int?) obj.post_count) &&
                score.TrueEquals((int?) obj.score) &&
                (user == null && obj.user == null || user.EqualsDynamic(obj.user));
        }
    }
}