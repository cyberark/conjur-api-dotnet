mkdir nugetPackages

version=`ikdasm -assembly bin/Release/conjur-api.dll | grep -E Version | sed -n -r 's@^Version:\s+([[:digit:]]+\.[[:digit:]]+)\..*@\1@p'`
nuget pack ./conjur-api.nuspec -Properties configuration="Release" -Properties platform="Any CPU" \
          -OutputDirectory ./nugetPackages -Version "${version:-0.0}.${build_name}" -Verbosity detailed

nuget sources add -Name "conjur-api-dotnet" -Source "https://conjurinc.jfrog.io/artifactory/api/nuget/conjur-api-dotnet" \
     -UserName $WRITE_ARTIFACTORY_USERNAME -Password $WRITE_ARTIFACTORY_PASSWORD -Verbosity detailed

nuget push ./nugetPackages/* -Source "conjur-api-dotnet" -Verbosity detailed

rm -rf nugetPackages