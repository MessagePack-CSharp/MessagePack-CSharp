dotnet run -f "net6.0" --project "$PSScriptRoot/../../src/MessagePack.Generator/MessagePack.Generator.csproj" -- -i "$PSScriptRoot/../SharedData/SharedData.csproj" -o "$PSScriptRoot/Generated.cs"
