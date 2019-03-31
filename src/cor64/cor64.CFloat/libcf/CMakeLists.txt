cmake_minimum_required(VERSION 2.8.9)
project (cf)
add_library(cf SHARED cfloat.c)

install(TARGETS cf 
        CONFIGURATIONS Debug Release 
        DESTINATION bin)