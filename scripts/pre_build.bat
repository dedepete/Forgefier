@ECHO OFF

ECHO # Executing "pre_build.bat"...

SETLOCAL ENABLEDELAYEDEXPANSION
SET CONFIGURATION=%~1
SET TARGETDIR=..\..\output\%CONFIGURATION%

PUSHD ..\..
SET ROOTDIR=%CD%
POPD

CD "%ROOTDIR%"
CD "src\LibraryUnpacker"
CALL build

ECHO # Copying LibraryUnpacker into "src\Forgefier\Embedded\"...

XCOPY /Y /F LibraryUnpacker.jar "%ROOTDIR%\src\Forgefier\Embedded\"
XCOPY /Y /F .\classpath\xz-1.8.jar "%ROOTDIR%\src\Forgefier\Embedded\"
DEL /Q LibraryUnpacker.jar

ECHO # Finished executing "pre_build.bat".

GOTO :EOF