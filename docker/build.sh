#!/bin/sh -xe

cp -a /src /build
cd /build
git clean -fdx || :

cp -a /packages .
nuget restore

# test
xbuild
nunit-console test/test.csproj

# build
cd conjur-api
xbuild \
  /property:DelaySign=true \
  /property:KeyOriginatorFile=/src/conjur-sn.pub \
  /property:configuration=Release
