#!/bin/sh -xe

mkdir nugetPackages

version=`cat conjur-api/conjur-api.csproj | grep '<AssemblyVersion>' | cut -d ">" -f 2 | cut -d "<" -f 1`
dotnet pack -o ./nugetPackages --version-suffix "${version:-0.0}.${build_name}"

dotnet nuget add source "https://$WRITE_ARTIFACTORY_URL/artifactory/api/nuget/conjur-api-dotnet" --name "conjur-api-dotnet" \
     --username $WRITE_ARTIFACTORY_USERNAME --password $WRITE_ARTIFACTORY_PASSWORD --store-password-in-clear-text

dotnet nuget push ./nugetPackages/* --source "conjur-api-dotnet"

rm -rf nugetPackages
