mkdir nugetPackages

nuget pack ./conjur-api.nuspec -Properties configuration="Release" -Properties platform="Any CPU" \
          -OutputDirectory ./nugetPackages -Version $version -Verbosity detailed

echo "UserName $WRITE_ARTIFACTORY_USERNAME"
echo "Password $WRITE_ARTIFACTORY_PASSWORD"
nuget sources add -Name "conjur-api-dotnet" -Source "https://conjurinc.jfrog.io/artifactory/api/nuget/conjur-api-dotnet" \
     -UserName $WRITE_ARTIFACTORY_USERNAME -Password $WRITE_ARTIFACTORY_PASSWORD -Verbosity detailed

nuget push ./nugetPackages/* -Source "conjur-api-dotnet"

rm -rf nugetPackages