#!/bin/bash

os=linux_x64
grpcVersion=1.21.0
packagePath=~/.nuget/packages/grpc.tools/${grpcVersion}/tools/${os}/
zeebeVersion='0.18.0'
protoFile=gateway.proto
gwProtoPath=./
genPath=Client/Impl/proto

# go to root
echo -e "cd ../\n"
cd ../

# restore packages
echo -e "nuget restore Zeebe.sln\n"
nuget restore Zeebe.sln

# get gatway proto file
echo -e "curl -X GET https://raw.githubusercontent.com/zeebe-io/zeebe/${zeebeVersion}/gateway-protocol/src/main/proto/gateway.proto > ${protoFile}\n"
curl -X GET https://raw.githubusercontent.com/zeebe-io/zeebe/${zeebeVersion}/gateway-protocol/src/main/proto/${protoFile} > ${protoFile}


# generate gRPC
echo "${packagePath}/protoc \
  -I/usr/include/ \
  -I${gwProtoPath} \
  --csharp_out ${genPath} \
  --grpc_out ${genPath} \
  ${gwProtoPath}/${protoFile} \
  --plugin=\"protoc-gen-grpc=${packagePath}/grpc_csharp_plugin\""
echo -e "\n"

${packagePath}/protoc \
  -I/usr/include/ \
  -I${gwProtoPath} \
  --csharp_out ${genPath} \
  --grpc_out ${genPath} \
  ${gwProtoPath}/${protoFile} \
  --plugin="protoc-gen-grpc=${packagePath}/grpc_csharp_plugin"

# clean up
echo "rm ${protoFile}"
rm ${protoFile}
