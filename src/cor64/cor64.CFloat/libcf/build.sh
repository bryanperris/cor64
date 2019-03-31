#!/bin/bash
rm -rf b
mkdir b
cd b
cmake .. -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX=../../../../..
make
make install
cd ..
rm -rf b
