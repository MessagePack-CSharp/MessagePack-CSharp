# UnitySandbox instruction

Dependencies are managed by [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity).

Before launching Unity, you need to run the following command in this directory to restore the dependent libraries:

> dotnet nugetforunity restore

Also, since the MessagePack library for development is referenced from `bin\MessagePack\Debug\netstandard2.1`, you need to generate the DLL with a debug build beforehand.