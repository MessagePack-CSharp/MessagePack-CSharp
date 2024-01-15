// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CSharpSourceGeneratorVerifier;

public class MessagePackFormatterAttributeTests
{
    private readonly ITestOutputHelper testOutputHelper;

    public MessagePackFormatterAttributeTests(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task CanGenerateMessagePackFormatterAttr()
    {
        string testSource = """
using System;
using System.Collections.Generic;
using MessagePack;
using MessagePack.Formatters;

namespace TempProject
{
    [MessagePackFormatter(typeof(MyFormatter))]
    public class MyMessagePackObject
    {
        public int Foo { get; set; }

        public class MyFormatter : IMessagePackFormatter<MyMessagePackObject>
        {
            public MyMessagePackObject Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public void Serialize(ref MessagePackWriter writer, MyMessagePackObject value, MessagePackSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }
    }

    [MessagePackObject]
    public class Bar
    {
        [Key(0)]
        public MyMessagePackObject Baz { get; set; }
    }
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }
}
