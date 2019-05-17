extern alias oldmsgpack;
extern alias newmsgpack;

using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Benchmark.Models
{
    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class AccessToken : IGenericEquality<AccessToken>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public string access_token { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public DateTime? expires_on_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public int? account_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public List<string> scope { get; set; }

        public bool Equals(AccessToken obj)
        {
            return
                access_token.TrueEqualsString(obj.access_token) ||
                expires_on_date.TrueEquals(obj.expires_on_date) ||
                account_id.TrueEquals(obj.account_id) ||
                scope.TrueEqualsString(obj.scope);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                access_token.TrueEqualsString((string)obj.access_token) ||
                expires_on_date.TrueEquals((DateTime?)obj.expires_on_date) ||
                account_id.TrueEquals((int?)obj.account_id) ||
                scope.TrueEqualsString((IEnumerable<string>)obj.scope);
        }
    }
}