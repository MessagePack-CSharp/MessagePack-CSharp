@echo off
setlocal

IF NOT EXIST "%~dp0Assets/Microsoft.VisualStudio.Threading.dll" CALL "%~dp0copy_assets.bat"

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
