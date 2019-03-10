extern alias oldmsgpack;
extern alias newmsgpack;
using ProtoBuf;

namespace Benchmark.Models
{
    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class WritePermission : IGenericEquality<WritePermission>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? user_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public string object_type { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public bool? can_add { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public bool? can_edit { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public bool? can_delete { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public int? max_daily_actions { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public int? min_seconds_between_actions { get; set; }

        public bool Equals(WritePermission obj)
        {
            return
                can_add.TrueEquals(obj.can_add) &&
                can_delete.TrueEquals(obj.can_delete) &&
                can_edit.TrueEquals(obj.can_edit) &&
                max_daily_actions.TrueEquals(obj.max_daily_actions) &&
                min_seconds_between_actions.TrueEquals(obj.min_seconds_between_actions) &&
                object_type.TrueEqualsString(obj.object_type) &&
                user_id.TrueEquals(obj.user_id);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                can_add.TrueEquals((bool?) obj.can_add) &&
                can_delete.TrueEquals((bool?) obj.can_delete) &&
                can_edit.TrueEquals((bool?) obj.can_edit) &&
                max_daily_actions.TrueEquals((int?) obj.max_daily_actions) &&
                min_seconds_between_actions.TrueEquals((int?) obj.min_seconds_between_actions) &&
                object_type.TrueEqualsString((string) obj.object_type) &&
                user_id.TrueEquals((int?) obj.user_id);
        }
    }
}