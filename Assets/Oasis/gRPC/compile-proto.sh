#!/bin/bash

export PATH=$PATH:$HOME/go/bin
export PATH=$PATH:/usr/local/go/bin

SRC_DIR=./
DST_DIR=./
protoc  -I=$SRC_DIR \
	--csharp_out=$DST_DIR \
	--grpc_out=$DST_DIR \
	--plugin=protoc-gen-grpc=grpc.tools.2.45.0/macosx_x64/grpc_csharp_plugin \
        $SRC_DIR/oasis.proto

