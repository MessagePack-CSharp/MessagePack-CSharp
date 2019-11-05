dotnet publish %~dp0MessagePack.UniversalCodeGenerator.csproj -c Release --self-contained -r win-x64
dotnet publish %~dp0MessagePack.UniversalCodeGenerator.csproj -c Release --self-contained -r linux-x64
dotnet publish %~dp0MessagePack.UniversalCodeGenerator.csproj -c Release --self-contained -r osx-x64