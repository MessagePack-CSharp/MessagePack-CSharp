:: Link in the binaries we build or restore, that Unity expects inside its Assets directory.
@SETLOCAL
@if "%BUILDCONFIGURATION%"=="" SET BUILDCONFIGURATION=Release
@IF NOT EXIST "%~dp0..\..\bin\MessagePack\%BUILDCONFIGURATION%\netstandard2.0\publish" (
    dotnet publish "%~dp0..\MessagePack" -c release -f netstandard2.0
    IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%
)

@pushd %~dp0

echo F | xcopy "..\..\bin\MessagePack\release\netstandard2.0\publish\System.Runtime.CompilerServices.Unsafe.dll" ".\Assets\Plugins\System.Runtime.CompilerServices.Unsafe.dll" /Y /I
echo F | xcopy "..\..\bin\MessagePack\release\netstandard2.0\publish\Microsoft.NET.StringTools.dll" ".\Assets\Plugins\Microsoft.NET.StringTools.dll" /Y /I

@popd
