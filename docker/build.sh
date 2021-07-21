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
msbuild api-dotnet.sln /t:build /p:Configuration="Release" /p:Platform="Any CPU" /p:SkipInvalidConfigurations=true /m \
  /p:BuildInParallel=true /p:AllowUntrustedCertificate=False /p:CreatePackageOnPublish=False /p:DeployOnBuild=False \
   /p:GenerateVSPropsFile=True /p:NodeReuse=False /p:RunCodeAnalysis=AsConfigured /p:ToolPlatform=Auto /p:Verbosity=Normal

../docker/nuget.sh
