@echo off
pushd "%~dp0"
setlocal EnableDelayedExpansion

for /f "tokens=3,*" %%e in ('git ls-files -s ^| findstr /R /C:"^120000"') do (
     call :processFirstLine %%f
)
REM pause
goto :eof

:processFirstLine
@echo.
@echo FILE:    %1

dir "%~f1" | find "<SYMLINK>" >NUL && (
  @echo FILE already is a symlink
  goto :eof
)

for /f "usebackq tokens=*" %%l in ("%~f1") do (
  @echo LINK TO: %%l

  del "%~f1"
  if not !ERRORLEVEL! == 0 (
    @echo FAILED: del
    goto :eof
  )

  setlocal
  call :expandRelative linkto "%1" "%%l"
  mklink "%~f1" "!linkto!"
  endlocal
  if not !ERRORLEVEL! == 0 (
    @echo FAILED: mklink
    @echo reverting deletion...
    git checkout -- "%~f1"
    goto :eof
  )

  git update-index --assume-unchanged "%1"
  if not !ERRORLEVEL! == 0 (
    @echo FAILED: git update-index --assume-unchanged
    goto :eof
  )
  @echo SUCCESS
  goto :eof
)
goto :eof

:: param1 = result variable
:: param2 = reference path from which relative will be resolved
:: param3 = relative path
:expandRelative
  pushd .
  cd "%~dp2"
  set %1=%~f3
  popd
goto :eof
