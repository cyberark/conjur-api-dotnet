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

echo "XXX build.sh: RUN_AZURE_TESTS on entry: ${RUN_AZURE_TESTS:-<not set>}"
echo "XXX build.sh: CONJUR_APPLIANCE_URL on entry: ${CONJUR_APPLIANCE_URL:-<not set>}"
echo "XXX build.sh: CONJUR_ACCOUNT on entry: ${CONJUR_ACCOUNT:-<not set>}"
echo "XXX build.sh: CONJUR_NETRC_PATH on entry: ${CONJUR_NETRC_PATH:-<not set>}"

# Default if not provided by summon/env.
RUN_AZURE_TESTS="${RUN_AZURE_TESTS:-false}"
echo "XXX build.sh: RUN_AZURE_TESTS after default: ${RUN_AZURE_TESTS}"

# Only enforce Conjur connection prerequisites when Azure tests are explicitly enabled.
echo "XXX build.sh: Evaluating Azure test prerequisites — RUN_AZURE_TESTS=${RUN_AZURE_TESTS}, CONJUR_APPLIANCE_URL=${CONJUR_APPLIANCE_URL:-<not set>}, CONJUR_ACCOUNT=${CONJUR_ACCOUNT:-<not set>}"
if [ "$RUN_AZURE_TESTS" = "true" ]; then
  # Resolve Conjur connection from direct or InfraPool-prefixed vars.
  CONJUR_APPLIANCE_URL="${CONJUR_APPLIANCE_URL:-${INFRAPOOL_CONJUR_APPLIANCE_URL:-}}"
  CONJUR_ACCOUNT="${CONJUR_ACCOUNT:-${INFRAPOOL_CONJUR_ACCOUNT:-}}"
  echo "XXX build.sh: After InfraPool fallback — CONJUR_APPLIANCE_URL=${CONJUR_APPLIANCE_URL:-<not set>}, CONJUR_ACCOUNT=${CONJUR_ACCOUNT:-<not set>}"

  if [ -z "${CONJUR_APPLIANCE_URL}" ] || [ -z "${CONJUR_ACCOUNT}" ]; then
    echo "XXX build.sh: CONJUR_APPLIANCE_URL or CONJUR_ACCOUNT is empty — disabling Azure tests."
    RUN_AZURE_TESTS=false
  else
    echo "XXX build.sh: Conjur prerequisites met — Azure tests will run."
  fi
else
  echo "XXX build.sh: RUN_AZURE_TESTS is not true — Azure tests will be skipped."
fi

CONJUR_NETRC_FILE="${CONJUR_NETRC_PATH:-$HOME/.netrc}"
echo "XXX build.sh: CONJUR_NETRC_FILE resolved to: ${CONJUR_NETRC_FILE}"
echo "XXX build.sh: netrc file exists: $([ -f "${CONJUR_NETRC_FILE}" ] && echo yes || echo no)"

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

echo "XXX build.sh: Launching test container with RUN_AZURE_TESTS=${RUN_AZURE_TESTS}, CONJUR_APPLIANCE_URL=${CONJUR_APPLIANCE_URL:-<not set>}, CONJUR_ACCOUNT=${CONJUR_ACCOUNT:-<not set>}"
docker run "${DOCKER_ARGS[@]}" dotnet-builder:dev

CID=$(cat "$CIDFILE")

docker cp "$CID":"/build/TestResults.xml" .
docker cp "$CID":"/build/Coverage.xml" .
mkdir -p bin
docker cp "$CID":"/build/conjur-api/bin/Release/net8.0/conjur-api.dll" bin/conjur-api.dll
docker cp "$CID":"/build/nugetPackages/." bin/

cat TestResults.xml
