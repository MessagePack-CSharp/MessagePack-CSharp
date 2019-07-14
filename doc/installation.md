# Installation

This library is distributed via NuGet package and with special [support for Unity](doc/unity.md).

## NuGet packages

We target .NET Standard 2.0 with special optimizations for .NET Core 2.1+.

    Install-Package MessagePack

Install the optional C# analyzer to get warnings for coding mistakes and code fixes to save you time:

    Install-Package MessagePackAnalyzer

Extension Packages (learn more in our [extensions section](doc/extensions.md)):

    Install-Package MessagePack.ImmutableCollection
    Install-Package MessagePack.ReactiveProperty
    Install-Package MessagePack.UnityShims
    Install-Package MessagePack.AspNetCoreMvcFormatter

## Unity

For Unity, download from [releases](https://github.com/neuecc/MessagePack-CSharp/releases) page, providing `.unitypackage`. Unity IL2CPP or Xamarin AOT Environment, check the [pre-code generation section](doc/aot.md).
