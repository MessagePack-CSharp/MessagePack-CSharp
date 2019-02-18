# Updating Generated.cs

Generated.cs is a generated file and should not be edited manually.
To update Generated.cs, follow these steps:

	dotnet build src\MessagePack -f netstandard1.6
	dotnet run -p src\MessagePack.UniversalCodeGenerator -- -i sandbox\SharedData\SharedData.csproj -o sandbox\sandbox\Generated.cs

The first step is because `sandbox\SharedProject` references it and MPC doesn't build project references during code gen.
