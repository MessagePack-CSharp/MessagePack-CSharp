extern alias oldmsgpack;
extern alias newmsgpack;
using System;
using ProtoBuf;

namespace Benchmark.Models
{
    public enum InboxItemType
    {
        comment = 1,
        chat_message = 2,
        new_answer = 3,
        careers_message = 4,
        careers_invitations = 5,
        meta_question = 6,
        post_notice = 7,
        moderator_message = 8
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class InboxItem : IGenericEquality<InboxItem>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public InboxItemType? item_type { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public int? question_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public int? answer_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public int? comment_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string title { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public DateTime? creation_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public bool? is_unread { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public Info.Site site { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public string body { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public string link { get; set; }

        public bool Equals(InboxItem obj)
        {
            return
                answer_id.TrueEquals(obj.answer_id) &&
                body.TrueEqualsString(obj.body) &&
                comment_id.TrueEquals(obj.comment_id) &&
                creation_date.TrueEquals(obj.creation_date) &&
                is_unread.TrueEquals(obj.is_unread) &&
                item_type.TrueEquals(obj.item_type) &&
                link.TrueEqualsString(obj.link) &&
                question_id.TrueEquals(obj.question_id) &&
                site.TrueEquals(obj.site) &&
                title.TrueEqualsString(obj.title);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                answer_id.TrueEquals((int?) obj.answer_id) &&
                body.TrueEqualsString((string) obj.body) &&
                comment_id.TrueEquals((int?) obj.comment_id) &&
                creation_date.TrueEquals((DateTime?) obj.creation_date) &&
                is_unread.TrueEquals((bool?) obj.is_unread) &&
                item_type.TrueEquals((InboxItemType?) obj.item_type) &&
                link.TrueEqualsString((string) obj.link) &&
                question_id.TrueEquals((int?) obj.question_id) &&
                (site == null && obj.site == null || site.EqualsDynamic(obj.site)) &&
                title.TrueEqualsString((string) obj.title);
        }
    }
}