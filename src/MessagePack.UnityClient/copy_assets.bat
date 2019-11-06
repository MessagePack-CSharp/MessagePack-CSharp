:: Link in the binaries we build or restore, that Unity expects inside its Assets directory.
@SETLOCAL
@if "%BUILDCONFIGURATION%"=="" SET BUILDCONFIGURATION=Release
@IF NOT EXIST "%~dp0..\..\bin\MessagePack\%BUILDCONFIGURATION%\netstandard2.0\publish" (
    dotnet publish "%~dp0..\MessagePack" -c release -f netstandard2.0
    IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%
)

@pushd %~dp0

echo F | xcopy "..\..\bin\MessagePack\release\netstandard2.0\publish\System.Buffers.dll" ".\Assets\Plugins\System.Buffers.dll" /Y /I 
echo F | xcopy "..\..\bin\MessagePack\release\netstandard2.0\publish\System.Memory.dll" ".\Assets\Plugins\System.Memory.dll" /Y /I 
echo F | xcopy "..\..\bin\MessagePack\release\netstandard2.0\publish\System.Runtime.CompilerServices.Unsafe.dll" ".\Assets\Plugins\System.Runtime.CompilerServices.Unsafe.dll" /Y /I 
echo F | xcopy "..\..\bin\MessagePack\release\netstandard2.0\publish\System.Threading.Tasks.Extensions.dll" ".\Assets\Plugins\System.Threading.Tasks.Extensions.dll" /Y /I 

@popd
