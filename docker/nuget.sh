#!/usr/bin/env bash
set -euox pipefail

mkdir nugetPackages

version="${1:-}"
dotnet pack -o ./nugetPackages --version-suffix "${version:-0.0}"

# Check if WRITE_ARTIFACTORY_URL is set
if [ -z "${WRITE_ARTIFACTORY_URL:-}" ]; then
  echo "WRITE_ARTIFACTORY_URL is not set, skipping nuget push"
  exit 0
fi

dotnet nuget add source "https://$WRITE_ARTIFACTORY_URL/artifactory/api/nuget/conjur-api-dotnet" --name "conjur-api-dotnet" \
     --username "$WRITE_ARTIFACTORY_USERNAME" --password "$WRITE_ARTIFACTORY_PASSWORD" --store-password-in-clear-text

dotnet nuget push ./nugetPackages/* --source "conjur-api-dotnet"

rm -rf nugetPackages
