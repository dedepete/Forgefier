@ECHO OFF

ECHO # Executing "post_build.bat"...

SETLOCAL ENABLEDELAYEDEXPANSION
SET CONFIGURATION=%~1
SET TARGETDIR=..\..\output\%CONFIGURATION%

PUSHD ..\..
SET ROOTDIR=%CD%
POPD

ECHO # Copying DLLs into "\ForgefierData\dll\"...

CD "%TARGETDIR%"
XCOPY /Y "*.dll" "%CD%\ForgefierData\dll\"
ATTRIB +r *.exe*
DEL /Q *.*
ATTRIB -r *.exe*

ECHO # Finished executing "post_build.bat".

GOTO :EOF