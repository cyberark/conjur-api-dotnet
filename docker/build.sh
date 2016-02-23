#!/bin/sh -xe

cp -a /src /build
cd /build
git clean -fdx || :

cp -a /packages .
nuget restore
xbuild
nunit-console test/test.csproj
