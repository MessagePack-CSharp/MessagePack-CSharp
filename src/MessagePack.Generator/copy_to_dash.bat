@echo off
xcopy .\bin\  ..\..\..\server-dash\ /S /E /Y
xcopy .\bin\  ..\..\..\client-dash\ /S /E /Y
timeout /t 5