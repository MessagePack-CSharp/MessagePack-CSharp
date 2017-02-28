REM currently libz can't work on netstandard dll?
REM libz inject-dll --assembly src\MessagePack.CodeGenerator\bin\Release\mpc.exe --include src\MessagePack.CodeGenerator\bin\Release\*.dll --move
REM move src\MessagePack.CodeGenerator\bin\Release\mpc.exe nuget\mpc.exe
REM move src\MessagePack.CodeGenerator\bin\Release\mpc.exe.config nuget\mpc.exe.config

copy src\MessagePack.CodeGenerator\bin\Release\*.dll nuget\tools\
copy src\MessagePack.CodeGenerator\bin\Release\mpc.exe nuget\tools\mpc.exe
copy src\MessagePack.CodeGenerator\bin\Release\mpc.exe.config nuget\tools\mpc.exe.config