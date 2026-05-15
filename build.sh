#!/usr/bin/env bash
set -euo pipefail

finish() {
  if [ -n "$CIDFILE" ]; then
    CID=$(cat "$CIDFILE")
    docker rm -f "$CID"
    rm -f "$CIDFILE"
  fi
}
trap finish EXIT

# When running in Jenkins, a VERSION file is automatically added to the repo root.
# When running locally, add one here.
if [ ! -f "$PWD/VERSION" ]; then
  echo "0.0.0-dev" > "$PWD/VERSION"
fi

docker build -t dotnet-builder:dev ./docker/

CIDFILE=$(mktemp -u)
docker run \
  -v "$PWD":/src:ro \
  -v "${CONJUR_NETRC_PATH:-$HOME/.netrc}":/root/.netrc:ro \
  --cidfile="$CIDFILE" \
  -e RUN_AWS_TESTS \
  -e RUN_AZURE_TESTS \
  -e CONJUR_APPLIANCE_URL \
  -e CONJUR_ACCOUNT \
  -e AZURE_SUBSCRIPTION_ID \
  -e AZURE_RESOURCE_GROUP \
  -e USER_ASSIGNED_IDENTITY \
  -e USER_ASSIGNED_IDENTITY_CLIENT_ID \
  dotnet-builder:dev

CID=$(cat "$CIDFILE")

docker cp "$CID":"/build/TestResults.xml" .
docker cp "$CID":"/build/Coverage.xml" .
mkdir -p bin
docker cp "$CID":"/build/conjur-api/bin/Release/net8.0/conjur-api.dll" bin/conjur-api.dll
docker cp "$CID":"/build/nugetPackages/." bin/

cat TestResults.xml
