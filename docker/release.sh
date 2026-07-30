#!/usr/bin/env bash
set -euo pipefail

cd /artifacts

# Artifactory no longer hosts a NuGet feed for this package (the old
# NuGet feed's host is retired). The .nupkg is uploaded to Artifactory
# as a generic release asset by the shared library's release()/promote()
# steps instead - see conjEntArtifactory.groovy.
if [ "${NUGET_API_KEY:-}" != "" ]; then
  dotnet nuget push conjur-api.*.nupkg --source "https://api.nuget.org/v3/index.json" -k "$NUGET_API_KEY"
fi
