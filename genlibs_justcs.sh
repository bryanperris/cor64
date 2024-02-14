#!/bin/bash

ROOT=$(pwd)
BIN=$ROOT/bin

dotnet restore genlibs/genlibs.sln
dotnet build genlibs/genlibs.sln

cd $ROOT/src/externals
export LD_LIBRARY_PATH=$BIN
# export LD_DEBUG=libs
dotnet run --project ../genlibs