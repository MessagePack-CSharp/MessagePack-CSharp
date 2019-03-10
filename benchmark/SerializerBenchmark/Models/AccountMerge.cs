extern alias oldmsgpack;
extern alias newmsgpack;
using System;
using ProtoBuf;

namespace Benchmark.Models
{
    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class AccountMerge : IGenericEquality<AccountMerge>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? old_account_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public int? new_account_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public DateTime? merge_date { get; set; }

        public bool Equals(AccountMerge obj)
        {
            return
                old_account_id.TrueEquals(obj.old_account_id) &&
                new_account_id.TrueEquals(obj.new_account_id) &&
                merge_date.TrueEquals(obj.merge_date);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                old_account_id.TrueEquals((int?) obj.old_account_id) &&
                new_account_id.TrueEquals((int?) obj.new_account_id) &&
                merge_date.TrueEquals((DateTime?) obj.merge_date);
        }
    }
}