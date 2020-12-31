#!/bin/bash

DIR=$(pwd)/src/RunN64/TestRoms

rm -rf $DIR
mkdir $DIR

find $1 -name \*.N64 -exec cp -v {} $DIR \;