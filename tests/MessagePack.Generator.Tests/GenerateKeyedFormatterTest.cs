// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MessagePack.Resolvers;
using Microsoft.CodeAnalysis;
using Nerdbank.Streams;
using Xunit;
using Xunit.Abstractions;

namespace MessagePack.Generator.Tests
{
    public class GenerateKeyedFormatterTest
    {
        private readonly ITestOutputHelper testOutputHelper;

        public GenerateKeyedFormatterTest(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task PropertiesGetterSetter()
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject(false)]
    public class MyMessagePackObject
    {
        [Key(0)]
        public int A { get; set; }
        [Key(1)]
        public string B { get; set; }
    }
}
            ";
            tempWorkarea.AddFileToTargetProject("MyMessagePackObject.cs", contents);

            var compiler = new MessagePackCompiler.CodeGenerator(testOutputHelper.WriteLine, CancellationToken.None);
            await compiler.GenerateFileAsync(
                tempWorkarea.GetOutputCompilation().Compilation,
                tempWorkarea.OutputDirectory,
                "TempProjectResolver",
                "TempProject.Generated",
                false,
                string.Empty,
                Array.Empty<string>());

            var compilation = tempWorkarea.GetOutputCompilation();
            compilation.Compilation.GetDiagnostics().Should().NotContain(x => x.Severity == DiagnosticSeverity.Error);

            // Run tests with the generated resolver/formatter assembly.
            compilation.ExecuteWithGeneratedAssembly((ctx, assembly) =>
            {
                var mpoType = assembly.GetType("TempProject.MyMessagePackObject");
                var options = MessagePackSerializerOptions.Standard
                    .WithResolver(CompositeResolver.Create(
                        StandardResolver.Instance,
                        TestUtilities.GetResolverInstance(assembly, "TempProject.Generated.Resolvers.TempProjectResolver")));

                // Build `[]`
                var seq = new Sequence<byte>();
                var writer = new MessagePackWriter(seq);
                writer.WriteArrayHeader(0);
                writer.Flush();

                dynamic result = MessagePackSerializer.Deserialize(mpoType, seq, options);

                // The deserialized object has default values.
                ((int)result.A).Should().Be(0);
                ((string)result.B).Should().BeNull();

                // Verify round trip serialization/deserialization.
                result.A = 123;
                result.B = "foobar";

                var serialized = MessagePackSerializer.Serialize(mpoType, (object)result, options);
                dynamic result2 = MessagePackSerializer.Deserialize(mpoType, serialized, options);
                ((int)result2.A).Should().Be(123);
                ((string)result2.B).Should().Be("foobar");
            });
        }

        [Fact]
        public async Task PropertiesGetterOnlyWithParameterizedConstructor()
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject(false)]
    public class MyMessagePackObject
    {
        [Key(0)]
        public int A { get; }
        [Key(1)]
        public string B { get; }

        public MyMessagePackObject(int a, string b)
        {
            A = a;
            B = b;
        }
    }
}
            ";
            tempWorkarea.AddFileToTargetProject("MyMessagePackObject.cs", contents);

            var compiler = new MessagePackCompiler.CodeGenerator(testOutputHelper.WriteLine, CancellationToken.None);
            await compiler.GenerateFileAsync(
                tempWorkarea.GetOutputCompilation().Compilation,
                tempWorkarea.OutputDirectory,
                "TempProjectResolver",
                "TempProject.Generated",
                false,
                string.Empty,
                Array.Empty<string>());

            var compilation = tempWorkarea.GetOutputCompilation();
            compilation.Compilation.GetDiagnostics().Should().NotContain(x => x.Severity == DiagnosticSeverity.Error);

            // Run tests with the generated resolver/formatter assembly.
            compilation.ExecuteWithGeneratedAssembly((ctx, assembly) =>
            {
                var mpoType = assembly.GetType("TempProject.MyMessagePackObject");
                var options = MessagePackSerializerOptions.Standard
                    .WithResolver(CompositeResolver.Create(
                        StandardResolver.Instance,
                        TestUtilities.GetResolverInstance(assembly, "TempProject.Generated.Resolvers.TempProjectResolver")));

                // Build `[-1, "foobar"]`
                var seq = new Sequence<byte>();
                var writer = new MessagePackWriter(seq);
                writer.WriteArrayHeader(2);
                writer.Write(-1);
                writer.Write("foobar");
                writer.Flush();

                // Verify deserialization
                dynamic result = MessagePackSerializer.Deserialize(mpoType, seq, options);
                ((int)result.A).Should().Be(-1);
                ((string)result.B).Should().Be("foobar");

                // Verify serialization
                var serialized = MessagePackSerializer.Serialize(mpoType, (object)result, options);
                serialized.Should().BeEquivalentTo(seq.AsReadOnlySequence.ToArray());
            });
        }
    }
}
