#!/bin/bash

dotnet build ./genlibs.sln

cd src/externals
dotnet run -p ../genlibs