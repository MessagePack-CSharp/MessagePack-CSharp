// This app is never meant to do anything at runtime. The whole test is the
// `dotnet publish` itself: TrimmerRootAssembly (see csproj) roots the entire
// UltraMessagePack surface so ILC analyzes all of it and reports any
// trim/AOT warnings. Publishing with zero IL warnings is the pass condition.
System.Console.WriteLine("If this published, the AOT compatibility test passed.");
