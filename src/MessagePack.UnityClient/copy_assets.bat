:: Link in the binaries we build or restore, that Unity expects inside its Assets directory.
@SETLOCAL
@if "%CONFIG%"=="" SET CONFIG=Release
@IF NOT EXIST "%~dp0..\..\bin\MessagePack\%CONFIG%\netstandard2.0\publish" (
    dotnet publish "%~dp0..\MessagePack" -c release -f netstandard2.0
    IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%
)

@pushd %~dp0

echo F | xcopy "..\..\bin\MessagePack\release\netstandard2.0\publish\Microsoft.VisualStudio.Validation.dll" ".\Assets\Plugins\Microsoft.VisualStudio.Validation.dll" /Y /I 
echo F | xcopy "..\..\bin\MessagePack\release\netstandard2.0\publish\Microsoft.VisualStudio.Threading.dll" ".\Assets\Plugins\Microsoft.VisualStudio.Threading.dll" /Y /I 
echo F | xcopy "..\..\bin\MessagePack\release\netstandard2.0\publish\Nerdbank.Streams.dll" ".\Assets\Plugins\Nerdbank.Streams.dll" /Y /I 
echo F | xcopy "..\..\bin\MessagePack\release\netstandard2.0\publish\System.IO.Pipelines.dll" ".\Assets\Plugins\System.IO.Pipelines.dll" /Y /I 
echo F | xcopy "..\..\bin\MessagePack\release\netstandard2.0\publish\System.Buffers.dll" ".\Assets\Plugins\System.Buffers.dll" /Y /I 
echo F | xcopy "..\..\bin\MessagePack\release\netstandard2.0\publish\System.Memory.dll" ".\Assets\Plugins\System.Memory.dll" /Y /I 
echo F | xcopy "..\..\bin\MessagePack\release\netstandard2.0\publish\System.Runtime.CompilerServices.Unsafe.dll" ".\Assets\Plugins\System.Runtime.CompilerServices.Unsafe.dll" /Y /I 
echo F | xcopy "..\..\bin\MessagePack\release\netstandard2.0\publish\System.Threading.Tasks.Extensions.dll" ".\Assets\Plugins\System.Threading.Tasks.Extensions.dll" /Y /I 

@popd
