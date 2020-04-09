// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias oldmsgpack;
extern alias newmsgpack;

using System;
using ProtoBuf;

namespace Benchmark.Models
{
    public enum EventType : byte
    {
        question_posted = 1,
        answer_posted = 2,
        comment_posted = 3,
        post_edited = 4,
        user_created = 5,
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class Event : IGenericEquality<Event>
    {
        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public EventType? event_type { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public int? event_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public DateTime? creation_date { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string excerpt { get; set; }

        public bool Equals(Event obj)
        {
            return
                this.creation_date.TrueEquals(obj.creation_date) &&
                this.event_id.TrueEquals(obj.event_id) &&
                this.event_type.TrueEquals(obj.event_type) &&
                this.excerpt.TrueEqualsString(obj.excerpt) &&
                this.link.TrueEqualsString(obj.link);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.creation_date.TrueEquals((DateTime?)obj.creation_date) &&
                this.event_id.TrueEquals((int?)obj.event_id) &&
                this.event_type.TrueEquals((EventType?)obj.event_type) &&
                this.excerpt.TrueEqualsString((string)obj.excerpt) &&
                this.link.TrueEqualsString((string)obj.link);
        }
    }
}
