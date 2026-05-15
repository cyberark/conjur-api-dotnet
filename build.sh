#!/usr/bin/env bash
set -euo pipefail

finish() {
  if [ -n "${CIDFILE:-}" ]; then
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

# Resolve Conjur connection: if CONJUR_APPLIANCE_URL is not already set
# (e.g. by docker-compose or an Azure executor agent), skip integration tests
# that require a live Conjur.
if [ -z "${CONJUR_APPLIANCE_URL:-}" ]; then
  echo "CONJUR_APPLIANCE_URL not set — Azure integration tests will be skipped."
  RUN_AZURE_TESTS=false
fi

CONJUR_NETRC_FILE="${CONJUR_NETRC_PATH:-$HOME/.netrc}"

CIDFILE=$(mktemp -u)

DOCKER_ARGS=(
  -v "$PWD":/src:ro
  --cidfile="$CIDFILE"
  -e RUN_AWS_TESTS
  -e "RUN_AZURE_TESTS=${RUN_AZURE_TESTS:-false}"
  -e CONJUR_APPLIANCE_URL
  -e CONJUR_ACCOUNT
  -e AZURE_SUBSCRIPTION_ID
  -e AZURE_RESOURCE_GROUP
  -e USER_ASSIGNED_IDENTITY
  -e USER_ASSIGNED_IDENTITY_CLIENT_ID
)

if [ -f "${CONJUR_NETRC_FILE}" ]; then
  DOCKER_ARGS+=(-v "${CONJUR_NETRC_FILE}":/root/.netrc:ro)
fi

docker run "${DOCKER_ARGS[@]}" dotnet-builder:dev

CID=$(cat "$CIDFILE")

docker cp "$CID":"/build/TestResults.xml" .
docker cp "$CID":"/build/Coverage.xml" .
mkdir -p bin
docker cp "$CID":"/build/conjur-api/bin/Release/net8.0/conjur-api.dll" bin/conjur-api.dll
docker cp "$CID":"/build/nugetPackages/." bin/

cat TestResults.xml
