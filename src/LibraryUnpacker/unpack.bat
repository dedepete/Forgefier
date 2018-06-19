:: .pack.xz unpacker: dolboeb edition. Unpacks and decompresses .jar.pack.xz into normal JAR.
:: C# does not have any good unpack200 implementation. C# sucks.
:: Original code can be found here:
:: https://github.com/MinecraftForge/Installer/blob/2228c90908ea51c417dea631b9807618c6746f89/src/main/java/net/minecraftforge/installer/DownloadUtils.java

@ECHO OFF
java -cp "xz-1.8.jar;LibraryUnpacker.jar" ru.dedepete.forgefier.LibraryUnpacker %1 %2