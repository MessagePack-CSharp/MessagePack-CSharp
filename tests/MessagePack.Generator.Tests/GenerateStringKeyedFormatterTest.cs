// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
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
    public class GenerateStringKeyedFormatterTest
    {
        private readonly ITestOutputHelper testOutputHelper;

        public GenerateStringKeyedFormatterTest(ITestOutputHelper testOutputHelper)
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
    [MessagePackObject(true)]
    public class MyMessagePackObject
    {
        public int A { get; set; }
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

                // Build `{ }`.
                var seq = new Sequence<byte>();
                var writer = new MessagePackWriter(seq);
                writer.WriteMapHeader(0);
                writer.Flush();

                // Verify deserialization
                dynamic result = MessagePackSerializer.Deserialize(mpoType, seq, options);
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
        public async Task PropertiesGetterOnlyMixed()
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject(true)]
    public class MyMessagePackObject
    {
        public int A { get; }
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

                // Build `{ "A": -1, "B": "foobar" }`.
                var seq = new Sequence<byte>();
                var writer = new MessagePackWriter(seq);
                writer.WriteMapHeader(2);
                writer.Write("A");
                writer.Write(-1);
                writer.Write("B");
                writer.Write("foobar");
                writer.Flush();

                // Verify deserialization
                dynamic result = MessagePackSerializer.Deserialize(mpoType, seq, options);
                ((int)result.A).Should().Be(0); // default
                ((string)result.B).Should().Be("foobar"); // from input
            });
        }

        [Fact]
        public async Task PropertiesGetterOnlyIgnore()
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject(true)]
    public class MyMessagePackObject
    {
        public int A { get; }
        public string B { get; }
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

                // Build `{ "A": -1, "B": "foobar" }`.
                var seq = new Sequence<byte>();
                var writer = new MessagePackWriter(seq);
                writer.WriteMapHeader(2);
                writer.Write("A");
                writer.Write(-1);
                writer.Write("B");
                writer.Write("foobar");
                writer.Flush();

                // Verify deserialization
                dynamic result = MessagePackSerializer.Deserialize(mpoType, seq, options);
                ((int)result.A).Should().Be(0);
                ((string)result.B).Should().BeNull();
            });
        }

        [Fact]
        public async Task PropertiesGetterOnlyDefaultValue()
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject(true)]
    public class MyMessagePackObject
    {
        public int A { get; } = 123;
        public string B { get; } = ""foobar"";
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

                // Build `{ }`.
                var seq = new Sequence<byte>();
                var writer = new MessagePackWriter(seq);
                writer.WriteMapHeader(0);
                writer.Flush();

                // Verify deserialization
                // The deserialized object has default values.
                dynamic result = MessagePackSerializer.Deserialize(mpoType, seq, options);
                ((int)result.A).Should().Be(123);
                ((string)result.B).Should().Be("foobar");
            });
        }

        [Fact]
        public async Task PropertiesGetterSetterWithDefaultValue()
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject(true)]
    public class MyMessagePackObject
    {
        public int A { get; set; } = 123;
        public string B { get; set; } = ""foobar"";
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

                // Build an empty data.
                var seq = new Sequence<byte>();
                var writer = new MessagePackWriter(seq);
                writer.WriteMapHeader(0);
                writer.Flush();

                // Verify deserialization
                // The deserialized object has default values.
                dynamic result = MessagePackSerializer.Deserialize(mpoType, seq, options);
                ((int)result.A).Should().Be(123);
                ((string)result.B).Should().Be("foobar");

                // Verify round trip serialization/deserialization.
                result.A = 456;
                result.B = "baz";

                var serialized = MessagePackSerializer.Serialize(mpoType, (object)result, options);
                dynamic result2 = MessagePackSerializer.Deserialize(mpoType, serialized, options);
                ((int)result2.A).Should().Be(456);
                ((string)result2.B).Should().Be("baz");
            });
        }

        [Fact]
        public async Task PropertiesGetterSetterWithDefaultValueInputPartially()
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject(true)]
    public class MyMessagePackObject
    {
        public int A { get; set; } = 123;
        public string B { get; set; } = ""foobar"";
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

                // Build `{ "A": -1 }`
                var seq = new Sequence<byte>();
                var writer = new MessagePackWriter(seq);
                writer.WriteMapHeader(1);
                writer.Write("A");
                writer.Write(-1);
                writer.Flush();

                // Verify deserialization
                // The deserialized object has default value and should preserve it.
                dynamic result = MessagePackSerializer.Deserialize(mpoType, seq, options);
                ((int)result.A).Should().Be(-1); // from input
                ((string)result.B).Should().Be("foobar"); // default value

                // Verify round trip serialization/deserialization.
                result.A = 456;
                result.B = "baz";

                var serialized = MessagePackSerializer.Serialize(mpoType, (object)result, options);
                dynamic result2 = MessagePackSerializer.Deserialize(mpoType, serialized, options);
                ((int)result2.A).Should().Be(456);
                ((string)result2.B).Should().Be("baz");
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
    [MessagePackObject(true)]
    public class MyMessagePackObject
    {
        public int A { get; }
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

                // Build `{ "A": -1, "B": "foobar" }`
                var seq = new Sequence<byte>();
                var writer = new MessagePackWriter(seq);
                writer.WriteMapHeader(2);
                writer.Write("A");
                writer.Write(-1);
                writer.Write("B");
                writer.Write("foobar");
                writer.Flush();

                // Verify deserialization
                dynamic result = MessagePackSerializer.Deserialize(mpoType, seq, options);
                ((int)result.A).Should().Be(-1); // from input
                ((string)result.B).Should().Be("foobar"); // from input

                // Verify serialization
                var serialized = MessagePackSerializer.Serialize(mpoType, (object)result, options);
                serialized.Should().BeEquivalentTo(seq.AsReadOnlySequence.ToArray());
            });
        }

        [Fact]
        public async Task PropertiesGetterOnlyWithParameterizedConstructorPartially()
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject(true)]
    public class MyMessagePackObject
    {
        public int A { get; }
        public string B { get; }

        public MyMessagePackObject(string b)
        {
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

                // Build `{ "A": -1, "B": "foobar" }`
                var seq = new Sequence<byte>();
                var writer = new MessagePackWriter(seq);
                writer.WriteMapHeader(2);
                writer.Write("A");
                writer.Write(-1);
                writer.Write("B");
                writer.Write("foobar");
                writer.Flush();

                // Verify deserialization
                dynamic result = MessagePackSerializer.Deserialize(mpoType, seq, options);
                ((int)result.A).Should().Be(0); // default value
                ((string)result.B).Should().Be("foobar"); // from input
            });
        }

        [Fact]
        public async Task PropertiesGetterOnlyWithParameterizedConstructorDefaultValue()
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject(true)]
    public class MyMessagePackObject
    {
        public int A { get; } = 12345;
        public string B { get; } = ""some"";

        public MyMessagePackObject(string b)
        {
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

                // Build `{ "A": -1, "B": "foobar" }`
                var seq = new Sequence<byte>();
                var writer = new MessagePackWriter(seq);
                writer.WriteMapHeader(2);
                writer.Write("A");
                writer.Write(-1);
                writer.Write("B");
                writer.Write("foobar");
                writer.Flush();

                // Verify deserialization
                dynamic result = MessagePackSerializer.Deserialize(mpoType, seq, options);
                ((int)result.A).Should().Be(12345); // default value
                ((string)result.B).Should().Be("foobar"); // from input
            });
        }

        [Fact]
        public async Task PropertiesGetterSetterWithParameterizedConstructor()
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject(true)]
    public class MyMessagePackObject
    {
        public int A { get; set; }
        public string B { get; set; }

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

                // Build `{ "A": -1, "B": "foobar" }`
                var seq = new Sequence<byte>();
                var writer = new MessagePackWriter(seq);
                writer.WriteMapHeader(2);
                writer.Write("A");
                writer.Write(-1);
                writer.Write("B");
                writer.Write("foobar");
                writer.Flush();

                // Verify deserialization
                dynamic result = MessagePackSerializer.Deserialize(mpoType, seq, options);
                ((int)result.A).Should().Be(-1); // from input
                ((string)result.B).Should().Be("foobar"); // from input

                // Verify serialization
                var serialized = MessagePackSerializer.Serialize(mpoType, (object)result, options);
                serialized.Should().BeEquivalentTo(seq.AsReadOnlySequence.ToArray());
            });
        }

        [Fact]
        public async Task PropertiesGetterSetterWithParameterizedConstructorPartially()
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject(true)]
    public class MyMessagePackObject
    {
        public int A { get; set; }
        public string B { get; set; }

        public MyMessagePackObject(int a)
        {
            A = a;
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

                // Build `{ "A": -1, "B": "foobar" }`
                var seq = new Sequence<byte>();
                var writer = new MessagePackWriter(seq);
                writer.WriteMapHeader(2);
                writer.Write("A");
                writer.Write(-1);
                writer.Write("B");
                writer.Write("foobar");
                writer.Flush();

                // Verify deserialization
                dynamic result = MessagePackSerializer.Deserialize(mpoType, seq, options);
                ((int)result.A).Should().Be(-1); // from ctor
                ((string)result.B).Should().Be("foobar"); // from setter

                // Verify serialization
                var serialized = MessagePackSerializer.Serialize(mpoType, (object)result, options);
                serialized.Should().BeEquivalentTo(seq.AsReadOnlySequence.ToArray());
            });
        }

        [Fact]
        public async Task PropertiesGetterSetterWithParameterizedConstructorDoNotUseSetter()
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject(true)]
    public class MyMessagePackObject
    {
        public int A { get; set; }

        public MyMessagePackObject(int a)
        {
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

                // Build `{ "A": -1 }`
                var seq = new Sequence<byte>();
                var writer = new MessagePackWriter(seq);
                writer.WriteMapHeader(1);
                writer.Write("A");
                writer.Write(-1);
                writer.Flush();

                // Verify deserialization
                dynamic result = MessagePackSerializer.Deserialize(mpoType, seq, options);
                ((int)result.A).Should().Be(0);
            });
        }

        [Fact]
        public async Task PropertiesGetterSetterWithParameterizedConstructorAndDefaultValue()
        {
            using var tempWorkarea = TemporaryProjectWorkarea.Create();
            var contents = @"
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject(true)]
    public class MyMessagePackObject
    {
        public int A { get; set; }
        public string B { get; set; } = ""foobar"";

        public MyMessagePackObject(int a)
        {
            A = a;
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

                // Build `{ "A": -1 }`
                var seq = new Sequence<byte>();
                var writer = new MessagePackWriter(seq);
                writer.WriteMapHeader(1);
                writer.Write("A");
                writer.Write(-1);
                writer.Flush();

                // Verify deserialization
                dynamic result = MessagePackSerializer.Deserialize(mpoType, seq, options);
                ((int)result.A).Should().Be(-1); // from ctor
                ((string)result.B).Should().Be("foobar"); // default value
            });
        }
    }
}
