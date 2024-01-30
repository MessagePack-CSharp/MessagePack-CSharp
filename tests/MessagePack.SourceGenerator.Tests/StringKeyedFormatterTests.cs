// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CSharpSourceGeneratorVerifier<MessagePack.SourceGenerator.MessagePackGenerator>;

public class StringKeyedFormatterTests
{
    private readonly ITestOutputHelper testOutputHelper;

    public StringKeyedFormatterTests(ITestOutputHelper testOutputHelper)
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
    [MessagePackObject(true)]
    public class MyMessagePackObject
    {
        public int A { get; set; }
        public string B { get; set; }
    }
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task PropertiesGetterOnlyMixed()
    {
        string testSource = """
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
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task PropertiesGetterOnlyIgnore()
    {
        string testSource = """
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
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task PropertiesGetterOnlyDefaultValue()
    {
        string testSource = """
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject(true)]
    public class MyMessagePackObject
    {
        public int A { get; } = 123;
        public string B { get; } = "foobar";
    }
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task PropertiesGetterSetterWithDefaultValue()
    {
        string testSource = """
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject(true)]
    public class MyMessagePackObject
    {
        public int A { get; set; } = 123;
        public string B { get; set; } = "foobar";
    }
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task PropertiesGetterSetterWithDefaultValueInputPartially()
    {
        string testSource = """
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject(true)]
    public class MyMessagePackObject
    {
        public int A { get; set; } = 123;
        public string B { get; set; } = "foobar";
    }
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task PropertiesGetterOnlyWithParameterizedConstructor()
    {
        string testSource = """
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
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task PropertiesGetterOnlyWithParameterizedConstructorPartially()
    {
        string testSource = """
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
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task PropertiesGetterOnlyWithParameterizedConstructorDefaultValue()
    {
        string testSource = """
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject(true)]
    public class MyMessagePackObject
    {
        public int A { get; } = 12345;
        public string B { get; } = "some";

        public MyMessagePackObject(string b)
        {
            B = b;
        }
    }
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task PropertiesGetterSetterWithParameterizedConstructor()
    {
        string testSource = """
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
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task PropertiesGetterSetterWithParameterizedConstructorPartially()
    {
        string testSource = """
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
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task PropertiesGetterSetterWithParameterizedConstructorDoNotUseSetter()
    {
        string testSource = """
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
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }

    [Fact]
    public async Task PropertiesGetterSetterWithParameterizedConstructorAndDefaultValue()
    {
        string testSource = """
using System;
using System.Collections.Generic;
using MessagePack;

namespace TempProject
{
    [MessagePackObject(true)]
    public class MyMessagePackObject
    {
        public int A { get; set; }
        public string B { get; set; } = "foobar";

        public MyMessagePackObject(int a)
        {
            A = a;
        }
    }
}
""";
        await VerifyCS.Test.RunDefaultAsync(this.testOutputHelper, testSource);
    }
}
