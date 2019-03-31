@echo off
rmdir /Q /S b > NUL
mkdir b
cd b
cmake .. -DCMAKE_INSTALL_PREFIX=../../../../.. -G "Visual Studio 15 2017 Win64"
cmake --build . --target --config Release
cmake --build . --target install
cd ..
rmdir /Q /S b > NUL