﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias newmsgpack;
extern alias oldmsgpack;

using System;
using ProtoBuf;

namespace Benchmark.Models
{
    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class TagWiki : IGenericEquality<TagWiki>
    {
        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public string tag_name { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public string body { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public string excerpt { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public DateTime? body_last_edit_date { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public DateTime? excerpt_last_edit_date { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public ShallowUser last_body_editor { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public ShallowUser last_excerpt_editor { get; set; }

        public bool Equals(TagWiki obj)
        {
            return
                this.body.TrueEqualsString(obj.body) &&
                this.body_last_edit_date.TrueEquals(obj.body_last_edit_date) &&
                this.excerpt.TrueEqualsString(obj.excerpt) &&
                this.excerpt_last_edit_date.TrueEquals(obj.excerpt_last_edit_date) &&
                this.last_body_editor.TrueEquals(obj.last_body_editor) &&
                this.last_excerpt_editor.TrueEquals(obj.last_excerpt_editor) &&
                this.tag_name.TrueEqualsString(obj.tag_name);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.body.TrueEqualsString((string)obj.body) &&
                this.body_last_edit_date.TrueEquals((DateTime?)obj.body_last_edit_date) &&
                this.excerpt.TrueEqualsString((string)obj.excerpt) &&
                this.excerpt_last_edit_date.TrueEquals((DateTime?)obj.excerpt_last_edit_date) &&
                ((this.last_body_editor == null && obj.last_body_editor == null) ||
                 this.last_body_editor.EqualsDynamic(obj.last_body_editor)) &&
                ((this.last_excerpt_editor == null && obj.last_excerpt_editor == null) ||
                 this.last_excerpt_editor.EqualsDynamic(obj.last_excerpt_editor)) &&
                this.tag_name.TrueEqualsString((string)obj.tag_name);
        }
    }
}
