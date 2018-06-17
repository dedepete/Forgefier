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

CD "%ROOTDIR%"
CD "src\LibraryUnpacker"
CALL build

ECHO # Copying LibraryUnpacker into "\ForgefierData\unpacker\"...

COPY /Y LibraryUnpacker.jar "%TARGETDIR%\ForgefierData\unpacker\"
COPY /Y unpack.bat "%TARGETDIR%\ForgefierData\unpacker\"
COPY /Y .\classpath\xz-1.8.jar "%TARGETDIR%\ForgefierData\unpacker\"
DEL /Q LibraryUnpacker.jar

ECHO # Finished executing "post_build.bat".

GOTO :EOF