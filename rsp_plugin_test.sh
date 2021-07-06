#!/bin/bash

dotnet build --configuration=Release ./cor64.sln

cd src/externals/rsp

rm -rf build
mkdir build
cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
make
cp rsp.so ../../../../bin/rsp.so
cd ..

cd ../../..


cd bin
DISPLAY=:0 ./RunN64