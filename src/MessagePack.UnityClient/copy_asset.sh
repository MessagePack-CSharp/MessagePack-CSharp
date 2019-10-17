@SETLOCAL
@if "%CONFIG%"=="" SET CONFIG=Release
@IF NOT EXIST "%~dp0..\..\bin\MessagePack\%CONFIG%\netstandard2.0\publish" (
    dotnet publish "%~dp0..\MessagePack" -c release -f netstandard2.0
    IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%
)

@pushd %~dp0

cp "..\..\bin\MessagePack\release\netstandard2.0\publish\Microsoft.VisualStudio.Validation.dll" ".\Assets\Plugins\Microsoft.VisualStudio.Validation.dll"
cp "..\..\bin\MessagePack\release\netstandard2.0\publish\Microsoft.VisualStudio.Threading.dll" ".\Assets\Plugins\Microsoft.VisualStudio.Threading.dll"
cp "..\..\bin\MessagePack\release\netstandard2.0\publish\Nerdbank.Streams.dll" ".\Assets\Plugins\Nerdbank.Streams.dll"
cp "..\..\bin\MessagePack\release\netstandard2.0\publish\System.IO.Pipelines.dll" ".\Assets\Plugins\System.IO.Pipelines.dll"
cp "..\..\bin\MessagePack\release\netstandard2.0\publish\System.Buffers.dll" ".\Assets\Plugins\System.Buffers.dll"
cp "..\..\bin\MessagePack\release\netstandard2.0\publish\System.Memory.dll" ".\Assets\Plugins\System.Memory.dll"
cp "..\..\bin\MessagePack\release\netstandard2.0\publish\System.Runtime.CompilerServices.Unsafe.dll" ".\Assets\Plugins\System.Runtime.CompilerServices.Unsafe.dll"
cp "..\..\bin\MessagePack\release\netstandard2.0\publish\System.Threading.Tasks.Extensions.dll" ".\Assets\Plugins\System.Threading.Tasks.Extensions.dll"

@popd