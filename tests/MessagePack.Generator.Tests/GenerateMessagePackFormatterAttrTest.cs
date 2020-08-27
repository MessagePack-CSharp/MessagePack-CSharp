// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MessagePack.Generator.Tests
{
    public class GenerateMessagePackFormatterAttrTest
    {
        private readonly ITestOutputHelper testOutputHelper;

        public GenerateMessagePackFormatterAttrTest(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task CanGenerateMessagePackFormatterAttr()
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

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
            ";
            tempWorkarea.AddFileToProject("MyMessagePackObject.cs", contents);

            var compiler = new MessagePackCompiler.CodeGenerator(testOutputHelper.WriteLine, CancellationToken.None);

            // can compile(does not throw MessagePackGeneratorResolveFailedException : Serialization Object must mark MessagePackObjectAttribute. type: global::TempProject.MyMessagePackObject)
            await compiler.GenerateFileAsync(
                tempWorkarea.CsProjectPath,
                tempWorkarea.OutputDirectory,
                string.Empty,
                "TempProjectResolver",
                "TempProject.Generated",
                false,
                string.Empty);
        }
    }


}
