﻿extern alias oldmsgpack;
extern alias newmsgpack;
using System.Collections.Generic;
using ProtoBuf;

namespace Benchmark.Models
{
    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class FlagOption : IGenericEquality<FlagOption>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? option_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public bool? requires_comment { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public bool? requires_site { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public bool? requires_question_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string title { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public string description { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public List<FlagOption> sub_options { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public bool? has_flagged { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public int? count { get; set; }

        public bool Equals(FlagOption obj)
        {
            return
                count.TrueEquals(obj.count) &&
                description.TrueEqualsString(obj.description) &&
                has_flagged.TrueEquals(obj.has_flagged) &&
                option_id.TrueEquals(obj.option_id) &&
                requires_comment.TrueEquals(obj.requires_comment) &&
                requires_question_id.TrueEquals(obj.requires_question_id) &&
                requires_site.TrueEquals(obj.requires_site) &&
                sub_options.TrueEqualsList(obj.sub_options) &&
                title.TrueEqualsString(obj.title);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                count.TrueEquals((int?) obj.count) &&
                description.TrueEqualsString((string) obj.description) &&
                has_flagged.TrueEquals((bool?) obj.has_flagged) &&
                option_id.TrueEquals((int?) obj.option_id) &&
                requires_comment.TrueEquals((bool?) obj.requires_comment) &&
                requires_question_id.TrueEquals((bool?) obj.requires_question_id) &&
                requires_site.TrueEquals((bool?) obj.requires_site) &&
                sub_options.TrueEqualsListDynamic((IEnumerable<dynamic>) obj.sub_options) &&
                title.TrueEqualsString((string) obj.title);
        }
    }
}