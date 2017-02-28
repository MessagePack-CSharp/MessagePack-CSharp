libz inject-dll --assembly src\MessagePack.CodeGenerator\bin\Release\mpc.exe --include src\MessagePack.CodeGenerator\bin\Release\*.dll --move
move src\MessagePack.CodeGenerator\bin\Release\mpc.exe nuget\mpc.exe
move src\MessagePack.CodeGenerator\bin\Release\mpc.exe.config nuget\mpc.exe.config