// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace MessagePack.SourceGenerator.Tests;

internal static class TestUtilities
{
    internal static string WrapTestSource(string source, ContainerKind containerKind)
    {
        StringBuilder testSource = new();
        testSource.AppendLine("using MessagePack;");
        switch (containerKind)
        {
            case ContainerKind.Namespace:
                testSource.AppendLine("namespace MyTestNamespace {");
                break;
            case ContainerKind.NestingClass:
                testSource.AppendLine("public class ContainingClass {");
                break;
        }

        testSource.AppendLine(source);

        if (containerKind != ContainerKind.None)
        {
            testSource.AppendLine("}");
        }

        return testSource.ToString();
    }
}
