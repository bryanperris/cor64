#!/bin/bash

ROOT=$(pwd)
BIN=$ROOT/bin

dotnet restore genlibs/genlibs.sln
dotnet build genlibs/genlibs.sln

cd $ROOT/src/externals/rsp
rm -rf build
mkdir build
cd build
cmake ..
make
cp rsp.so $BIN

cd $ROOT/src/externals/glide64
rm -rf build
mkdir build
cd build
cmake -DCOR64=ON ../src
make -j4
cp plugin/Release/cor64-GLideN64.so $BIN

cd $ROOT/src/externals
export LD_LIBRARY_PATH=$BIN
# export LD_DEBUG=libs
dotnet run --project ../genlibs