# cor64
C# based N64 Emulator

This is a work-in-progress freetime project

[ Debugging ]
 * When hex dumping a rom, the program code (after the IPL) always starts 0x1000 in the file normally
 
[ Development ]

What you need
* NodeJS
* .NET Framework / Mono
* .NET Core 3.x or higher
* For Linux: GLFW devel libraries

Restore NPM packages
$ npm install

Install gulp command
$ npm -g install gulp-cli

Build and Run
$ gulp run

Run Unit Tests
$ dotnet test -v q

[ References ] 

 * byuu's Bass Assembler: https://github.com/ARM9/bass
 * N64 Test Roms: https://github.com/PeterLemon/N64
 * Project64: https://github.com/project64/project64
 * Mupen64: https://github.com/mupen64plus/mupen64plus-core
 * Cent64: https://github.com/n64dev/cen64

![Alt text](.github/mandelbrot.png?raw=true "Mandelbrot Test")
![Alt text](.github/fputest1.png?raw=true "FPU Add Test")
![Alt text](.github/testscreen.png?raw=true "Test Screenshot")
