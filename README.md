# Forgefier

A small tool, which can install (almost) any Minecraft Forge version into Minecraft Launcher.

## Supported Forge versions

- Any Minecraft Forge version above Minecraft 1.5.2 with installer;
- Any Minecraft Forge version for Minecraft 1.5.2 and below (`legacy` builds).

## Requirements

- Not being a slow-witted;
- **.NET Framework 4.6.1**;
- **Java Runtime Environment 8** and above;
- Established **Internet connection** during the installation.

## Known Issues

- Some legacy versions should be patched with `ModLoader` before Forge installation.

## To do

- Command line arguments.

## Building

1. Run `git submodule init` and `git submodule update` commands within project's root directory to get contents of submodules.
2. Run `nuget restore` command within project's root directory to restore NuGet packages.
3. Download and Install Java Development Kit if needed.
4. Go to `src\LibraryUnpacker` and change JDK path in `build.bat` to your JDK installation path.
5. That's it! Now you can open the project in Visual Studio.

## Third-party assemblies and code

- [JSON.NET](http://james.newtonking.com/json);
- [DotNetZip](https://github.com/haf/DotNetZip.Semverd);
- [Fody](https://github.com/Fody/Fody);
- [Fody.Costura](https://github.com/Fody/Costura);
- [Forge Installer](https://github.com/MinecraftForge/Installer/).

## License

Forgefier is licensed under MIT License, but LibraryUnpacker code was taken from [MinecraftForge/Installer](https://github.com/MinecraftForge/Installer/blob/2228c90908ea51c417dea631b9807618c6746f89/src/main/java/net/minecraftforge/installer/DownloadUtils.java) repository. IDGAF if it is licensed. Feel free to open a Pull Request.

```no-highlight
MIT License

Copyright (c) 2018 Igor Popov

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```