:: Link in the binaries we build or restore, that Unity expects inside its Assets directory.
@SETLOCAL
@if "%CONFIG%"=="" SET CONFIG=Release
@IF NOT EXIST "%~dp0..\..\bin\MessagePack\%CONFIG%\net47" (
    dotnet build src\MessagePack -c release -f net47
    IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%
)

@pushd %~dp0

mklink ".\Assets\Microsoft.VisualStudio.Validation.dll" "..\..\..\bin\MessagePack\%CONFIG%\net47\Microsoft.VisualStudio.Validation.dll"
mklink ".\Assets\Microsoft.VisualStudio.Threading.dll" "..\..\..\bin\MessagePack\%CONFIG%\net47\Microsoft.VisualStudio.Threading.dll"
mklink ".\Assets\Nerdbank.Streams.dll" "..\..\..\bin\MessagePack\%CONFIG%\net47\Nerdbank.Streams.dll"
mklink ".\Assets\System.IO.Pipelines.dll" "..\..\..\bin\MessagePack\%CONFIG%\net47\System.IO.Pipelines.dll"
mklink ".\Assets\System.Buffers.dll" "..\..\..\bin\MessagePack\%CONFIG%\net47\System.Buffers.dll"
mklink ".\Assets\System.Memory.dll" "..\..\..\bin\MessagePack\%CONFIG%\net47\System.Memory.dll"
mklink ".\Assets\System.Runtime.CompilerServices.Unsafe.dll" "..\..\..\bin\MessagePack\%CONFIG%\net47\System.Runtime.CompilerServices.Unsafe.dll"
mklink ".\Assets\System.Threading.Tasks.Extensions.dll" "..\..\..\bin\MessagePack\%CONFIG%\net47\System.Threading.Tasks.Extensions.dll"

@popd
