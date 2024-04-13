using VerifyCS = CSharpSourceGeneratorVerifier<MessagePack.SourceGenerator.MessagePackGenerator>;

public class CustomStringKeyFormatterTest
{
    private readonly ITestOutputHelper testOutputHelper;

    public CustomStringKeyFormatterTest(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task PropertiesGetterSetter()
    {
        string testSource = """
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    public class MyMessagePackObject
    {
        [Key("nn")]
        public int A { get; set; }

        [Key("p")]
        public string B { get; set; }
    }
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task Constructor()
    {
        string testSource = """
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    public class MyMessagePackObject
    {
        [Key("nn")]
        public int A { get; set; }

        [Key("p")]
        public string B { get; set; }

        [SerializationConstructor]
        public MyMessagePackObject(int a, string b)
        {
            A = a;
            B = b;
        }
    }
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task RecordWithPrimaryConstructor()
    {
        string testSource = """
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject]
    public record MyMessagePackRecord([property: Key("p")] string PhoneNumber, [property: Key("c")] int Count);
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }
}
