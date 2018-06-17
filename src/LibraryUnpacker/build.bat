SET JDK=D:\Program Files\Java\jdk-10.0.1

ECHO # JDK location: "%JDK%".

ECHO # Building LibraryUnpacker...

"%JDK%\bin\javac.exe" -d "./build" -classpath "./classpath/xz-1.8.jar" ./src/LibraryUnpacker.java
CD build
"%JDK%\bin\jar.exe" -c -f LibraryUnpacker.jar *

MOVE /Y LibraryUnpacker.jar ..
CD ..
RD /Q /S build

ECHO # Finished executing "build.bat".

GOTO :EOF