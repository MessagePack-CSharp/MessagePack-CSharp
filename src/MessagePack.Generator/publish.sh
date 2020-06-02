#!/bin/bash
dotnet publish MessagePack.Generator.csproj -c Release --self-contained -r win-x64 -o ./bin/MessagePack.Generator/win
dotnet publish MessagePack.Generator.csproj -c Release --self-contained -r osx-x64 -o ./bin/MessagePack.Generator/osx
#dotnet publish MessagePack.Generator.csproj -c Release --self-contained -r linux-x64 -o ./bin/MessagePack.Generator/linux
read -t 5 -n1 -r -p "Press any key to continue..."; echo