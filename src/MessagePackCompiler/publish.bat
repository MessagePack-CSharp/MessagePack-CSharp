dotnet publish MessagePackCompiler.csproj -c Release --self-contained -r win-x64 -o ./bin/MessagePackCompiler/win-x64
dotnet publish MessagePackCompiler.csproj -c Release --self-contained -r linux-x64 -o ./bin/MessagePackCompiler/linux-x64
dotnet publish MessagePackCompiler.csproj -c Release --self-contained -r osx-x64 -o ./bin/MessagePackCompiler/osx-x64