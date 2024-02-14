# cor64
C# based N64 Emulator

This is a work-in-progress freetime project

### Debugging ###
 * When hex dumping a rom, the program code (after the IPL) always starts 0x1000 in the file normally
 
### Development ###

What you need
* .NET Framework / Mono
* .NET Core 3.x or higher
* For Linux: GLFW devel libraries

Required Global DotNet Tools
* Paket
* nuke.globaltool

Build and Run
$ nuke Run

Run Unit Tests
$ nuke Test [--filter <name>] [--debug-test]

### CppSharp support for Ubuntu 18.04

* Must have dotnet runtime 3.1 insalled

```
git clone https://github.com/InteropAlliance/premake-core/
cd premake-core
make -f Bootstrap.mak linux
cd ..
git clone --recursive https://github.com/mono/CppSharp.git
cd CppSharp
git checkout 1.0.1
cd build
cp -r ../../premake-core/bin/release/* premake/
./build.sh clone_llvm
./build.sh build_llvm
./build.sh package_llvm
./build.sh generate -configuration Release -platform x64
./build.sh -configuration Release -platform x64
```

### GlideN64 Crashing
* Make sure font file `/usr/share/fonts/truetype/freefont/FreeSans.ttf` exists

Now add the generated library to your ld configuration or use `LD_LIBRARY_PATH`

### References ###

 * byuu's Bass Assembler: https://github.com/ARM9/bass
 * N64 Test Roms: https://github.com/PeterLemon/N64
 * Project64: https://github.com/project64/project64
 * cxd4 Rsp: https://github.com/cxd4/rsp
 * Mupen64: https://github.com/mupen64plus/mupen64plus-core
 * Cen64: https://github.com/n64dev/cen64
 * AngryLion RDP: https://github.com/ata4/angrylion-rdp-plus/releases

![Alt text](.github/cubes16bpp.png?raw=true "16BPP RDP Cubes")
![Alt text](.github/mandelbrot.png?raw=true "Mandelbrot Test")
![Alt text](.github/fputest1.png?raw=true "FPU Add Test")
![Alt text](.github/testscreen.png?raw=true "Test Screenshot")
