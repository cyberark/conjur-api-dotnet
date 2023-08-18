#!/bin/sh -xe

cp -a /src /build
git config --global --add safe.directory /build
cd /build
git clean -fdx || :

dotnet restore

# test
dotnet build
dotnet test --logger:"junit;LogFileName=/build/TestResults.xml"

# build
dotnet build api-dotnet.sln --configuration Release

docker/nuget.sh
