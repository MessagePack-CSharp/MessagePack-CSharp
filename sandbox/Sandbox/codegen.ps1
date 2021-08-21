dotnet run -f "net5.0" -p "$PSScriptRoot/../../src/MessagePack.Generator/MessagePack.Generator.csproj" -- -i "$PSScriptRoot/../SharedData/SharedData.csproj" -o "$PSScriptRoot/Generated.cs"
