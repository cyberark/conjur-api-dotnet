#!/usr/bin/env bash
set -euo pipefail

REGISTRY_URL="${REGISTRY_URL:-docker.io}"
export REGISTRY_URL

finish() {
  docker compose down -v --remove-orphans || true
  if [ -n "${NETRC_FILE:-}" ]; then
    rm -f "$NETRC_FILE"
  fi
}
trap finish EXIT

echo "XXX CONJUR_DATA_KEY before generation: ${CONJUR_DATA_KEY:-<not set>}"
# Generate a data key for Conjur if not already set
export CONJUR_DATA_KEY="${CONJUR_DATA_KEY:-$(openssl rand -base64 32)}"
echo "XXX CONJUR_DATA_KEY after generation: ${CONJUR_DATA_KEY}"

# Start postgres and conjur
echo "XXX Starting postgres and conjur containers..."
docker compose up -d postgres conjur

# Wait for Conjur to be ready
echo "XXX Waiting for Conjur to be ready..."
for i in $(seq 1 30); do
  if docker compose exec -T conjur curl -sf http://localhost:80 > /dev/null 2>&1; then
    echo "XXX Conjur is ready after ${i} attempt(s)."
    break
  fi
  if [ "$i" -eq 30 ]; then
    echo "XXX Timed out waiting for Conjur."
    exit 1
  fi
  sleep 2
done

# Retrieve admin API key
echo "XXX Retrieving admin API key from Conjur..."
export CONJUR_AUTHN_API_KEY
CONJUR_AUTHN_API_KEY=$(docker compose exec -T conjur conjurctl role retrieve-key conjur:user:admin)
echo "XXX CONJUR_AUTHN_API_KEY set: ${CONJUR_AUTHN_API_KEY:0:4}****** (first 4 chars shown)"

# Set Conjur connection vars to point at the local conjur service
export CONJUR_APPLIANCE_URL="http://conjur"
echo "XXX CONJUR_APPLIANCE_URL set to: ${CONJUR_APPLIANCE_URL}"

export CONJUR_ACCOUNT="conjur"
echo "XXX CONJUR_ACCOUNT set to: ${CONJUR_ACCOUNT}"

# Write .netrc with admin credentials, matching conjur-api-go's approach
NETRC_FILE="$(mktemp)"
cat > "$NETRC_FILE" <<EOF
machine conjur
login admin
password ${CONJUR_AUTHN_API_KEY}
EOF
export CONJUR_NETRC_PATH="$NETRC_FILE"
echo "XXX .netrc written to: ${NETRC_FILE} and CONJUR_NETRC_PATH set to: ${CONJUR_NETRC_PATH}"

echo "XXX RUN_AZURE_TESTS as injected by summon: ${RUN_AZURE_TESTS:-<not set>}"

# RUN_AZURE_TESTS is injected by summon -e azure from secrets.yml
# Delegate to build.sh which handles docker run with the correct
# env vars and netrc mount.
echo "XXX Calling build.sh with: CONJUR_APPLIANCE_URL=${CONJUR_APPLIANCE_URL} CONJUR_ACCOUNT=${CONJUR_ACCOUNT} RUN_AZURE_TESTS=${RUN_AZURE_TESTS:-<not set>}"
"$(dirname "$0")/../build.sh"