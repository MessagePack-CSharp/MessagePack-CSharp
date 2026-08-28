// Repacks the MessagePack-CSharp v3 oracle under a non-colliding assembly name.
//
// The tests and benchmarks byte-compare this repo's serializer against the REAL v3
// runtime in the same process. Both cannot be named MessagePack.dll (one output file
// per directory, one assembly per simple name in the default AssemblyLoadContext), and
// the product owns that name — so the ORACLE gets renamed instead: assembly name and
// module name become MessagePackV3, and the strong-name signature (now invalid) is
// stripped. Its references (MessagePack.Annotations, Microsoft.NET.StringTools) are
// untouched, so the attributes keep the SAME identity the test sources compile against.
//
// Regenerate after an oracle version bump (the package must be in the NuGet cache —
// restoring any project that once referenced it, or `dotnet add package`, gets it there):
//   dotnet run --project tools/V3OracleRepack -- ^
//     %USERPROFILE%\.nuget\packages\messagepack\3.1.8\lib\net9.0\MessagePack.dll ^
//     tools\oracle\MessagePackV3.dll MessagePackV3
using Mono.Cecil;

if (args.Length != 3)
{
    Console.Error.WriteLine("usage: V3OracleRepack <input.dll> <output.dll> <newAssemblyName>");
    return 1;
}
var input = args[0];
var output = args[1];
var newName = args[2];

using var assembly = AssemblyDefinition.ReadAssembly(input);
Console.WriteLine($"input : {assembly.Name.FullName}");
assembly.Name.Name = newName;
assembly.Name.PublicKey = [];
assembly.Name.HasPublicKey = false;
assembly.MainModule.Name = Path.GetFileName(output);
assembly.MainModule.Attributes &= ~ModuleAttributes.StrongNameSigned;

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
assembly.Write(output);
Console.WriteLine($"output: {assembly.Name.FullName} -> {output}");
return 0;
