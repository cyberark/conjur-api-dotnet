mkdir nugetPackages
VERSION=$1
nuget pack ./conjur-api.nuspec -Properties configuration="Release" -Properties platform="Any CPU" \
          -OutputDirectory ./nugetPackages -Version $VERSION -Verbosity detailed

nuget sources add -Name "conjur-dotnet-api" -Source "https://conjurinc.jfrog.io/api/conjur-dotnet-api" -Verbosity detailed

#nuget push ./nugetPackages/* -Source <repository> -ApiKey <UserName:Password?>

rm -rf nugetPackages