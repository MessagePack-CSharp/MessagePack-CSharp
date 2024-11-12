# Unity instruction

Dependencies are managed by [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity).

Before launching Unity, you need to run the following command in this directory to restore the dependent libraries:

> dotnet tool restore

> dotnet nugetforunity restore

Also, you need to execute the `dotnet build` command in the root directory where the `.sln` file is located; since the MessagePack library for development is referenced from `bin\MessagePack\Debug\netstandard2.1` and `bin\MessagePack.SourceGenerator\Debug\netstandard2.0`, you need to generate the DLL with a debug build beforehand.

Also refer to `bin\...\Debug\...\` directory contains `package.json`. This is a file needed for referencing on Unity and should not be deleted.