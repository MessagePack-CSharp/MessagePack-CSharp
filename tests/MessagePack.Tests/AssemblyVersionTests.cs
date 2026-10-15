// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using MessagePack;
using Xunit;

public class AssemblyVersionTests
{
    [Fact]
    public void AssemblyVersionMatchesMajorAndMinorOfFileVersion()
    {
        Assembly assembly = typeof(MessagePackSerializer).Assembly;
        Version assemblyVersion = assembly.GetName().Version!;
        Version fileVersion = Version.Parse(assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version);

        Assert.Equal(new Version(fileVersion.Major, fileVersion.Minor, 0, 0), assemblyVersion);
    }
}
