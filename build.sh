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

# Default if not provided by summon/env.
RUN_AZURE_TESTS="${RUN_AZURE_TESTS:-false}"

# Only enforce Conjur connection prerequisites when Azure tests are explicitly enabled.
if [ "$RUN_AZURE_TESTS" = "true" ]; then
  # Resolve Conjur connection from direct or InfraPool-prefixed vars.
  CONJUR_APPLIANCE_URL="${CONJUR_APPLIANCE_URL:-${INFRAPOOL_CONJUR_APPLIANCE_URL:-}}"
  CONJUR_ACCOUNT="${CONJUR_ACCOUNT:-${INFRAPOOL_CONJUR_ACCOUNT:-}}"

  if [ -z "${CONJUR_APPLIANCE_URL}" ] || [ -z "${CONJUR_ACCOUNT}" ]; then
    echo "RUN_AZURE_TESTS=true but CONJUR_APPLIANCE_URL/CONJUR_ACCOUNT are not set; skipping Azure integration tests."
    RUN_AZURE_TESTS=false
  fi
fi

CONJUR_NETRC_FILE="${CONJUR_NETRC_PATH:-$HOME/.netrc}"

CIDFILE=$(mktemp -u)

DOCKER_ARGS=(
  -v "$PWD":/src:ro
  --cidfile="$CIDFILE"
  -e RUN_AWS_TESTS
  -e "RUN_AZURE_TESTS=${RUN_AZURE_TESTS}"
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
