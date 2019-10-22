:: Link in the binaries we build or restore, that Unity expects inside its Assets directory.
@SETLOCAL
@if "%CONFIG%"=="" SET CONFIG=Release
@IF NOT EXIST "%~dp0..\..\bin\MessagePack\%CONFIG%\netstandard2.0\publish" (
    dotnet publish "%~dp0..\MessagePack" -c release -f netstandard2.0
    IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%
)

@pushd %~dp0

mklink ".\Assets\Microsoft.VisualStudio.Validation.dll" "..\..\..\bin\MessagePack\%CONFIG%\netstandard2.0\publish\Microsoft.VisualStudio.Validation.dll"
mklink ".\Assets\Microsoft.VisualStudio.Threading.dll" "..\..\..\bin\MessagePack\%CONFIG%\netstandard2.0\publish\Microsoft.VisualStudio.Threading.dll"
mklink ".\Assets\Nerdbank.Streams.dll" "..\..\..\bin\MessagePack\%CONFIG%\netstandard2.0\publish\Nerdbank.Streams.dll"
mklink ".\Assets\System.IO.Pipelines.dll" "..\..\..\bin\MessagePack\%CONFIG%\netstandard2.0\publish\System.IO.Pipelines.dll"
mklink ".\Assets\System.Buffers.dll" "..\..\..\bin\MessagePack\%CONFIG%\netstandard2.0\publish\System.Buffers.dll"
mklink ".\Assets\System.Memory.dll" "..\..\..\bin\MessagePack\%CONFIG%\netstandard2.0\publish\System.Memory.dll"
mklink ".\Assets\System.Runtime.CompilerServices.Unsafe.dll" "..\..\..\bin\MessagePack\%CONFIG%\netstandard2.0\publish\System.Runtime.CompilerServices.Unsafe.dll"
mklink ".\Assets\System.Threading.Tasks.Extensions.dll" "..\..\..\bin\MessagePack\%CONFIG%\netstandard2.0\publish\System.Threading.Tasks.Extensions.dll"

@popd
