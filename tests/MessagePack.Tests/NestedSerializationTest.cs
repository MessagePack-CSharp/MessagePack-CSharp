// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !(MESSAGEPACK_FORCE_AOT || ENABLE_IL2CPP)
#define DYNAMIC_GENERATION
#endif

#if DYNAMIC_GENERATION

namespace MessagePack.Tests
{
    public class NestedSerializationTest
    {
        [MessagePackObject]
        public class SerializeOnSerializeClass
        {
            [IgnoreMember] private string key;

            [Key(0)]
            public byte[] SerializedKey
            {
                get { return MessagePackSerializer.Serialize(key); }
                set { key = MessagePackSerializer.Deserialize<string>(value); }
            }

            public SerializeOnSerializeClass()
            {
            }

            public SerializeOnSerializeClass(string key)
            {
                this.key = key;
            }
        }

        [Fact]
        public void SerializeAndDeserialize()
        {
            var testData = new SerializeOnSerializeClass("teststring");

            var data = MessagePackSerializer.Serialize(testData);
            var restored = MessagePackSerializer.Deserialize<SerializeOnSerializeClass>(data);

            restored.SerializedKey.Is(testData.SerializedKey);
        }
    }
}

#endif
