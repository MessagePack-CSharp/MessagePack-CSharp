@echo off
setlocal

IF NOT EXIST "%~dp0Assets/Microsoft.VisualStudio.Threading.dll" CALL "%~dp0link_assets.bat"

IF "%Build_ArtifactStagingDirectory%"=="" (
    SET LogDirectory=%~dp0..\..\bin\build_logs
) ELSE (
    SET LogDirectory=%Build_ArtifactStagingDirectory%\build_logs
)
IF NOT EXIST "%LogDirectory%" MD "%LogDirectory%"

ECHO Log files will be created under "%LogDirectory%"

echo on
start "Unity PACKAGE" /wait "c:\Program Files\unity\editor\Unity.exe" -batchmode -quit -nographics -silent-crashes -noUpm -buildTarget standalone -projectPath "%~dp0\" -executeMethod PackageExport.Export -logfile "%LogDirectory%\unity_package.log"
@IF ERRORLEVEL 1 (
    ECHO Package build FAILED
    exit /b %ERRORLEVEL%
)

start "Unity TESTS" /wait "c:\Program Files\unity\editor\Unity.exe" -batchmode -nographics -silent-crashes -noUpm -buildTarget standalone -projectPath "%~dp0\" -runEditorTests -editorTestsResultFile "%LogDirectory%\test-results\unity_results.xml" -logfile "%LogDirectory%\unity_tests.log"
@IF ERRORLEVEL 1 (
    ECHO Tests FAILED
    exit /b %ERRORLEVEL%
)

::start "Unity IL2CPP" /wait "c:\Program Files\unity\editor\Unity.exe" -batchmode -quit -nographics -silent-crashes -noUpm -buildTarget standalone -projectPath "%~dp0\" -buildWindows64Player "%~dp0..\..\bin\win64\MessagePack.exe" -logfile "%LogDirectory%\unity_il2cpp.log"
@IF ERRORLEVEL 1 (
    ECHO IL2CPP build FAILED
    exit /b %ERRORLEVEL%
)
