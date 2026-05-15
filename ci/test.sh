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

# Generate a data key for Conjur if not already set
export CONJUR_DATA_KEY="${CONJUR_DATA_KEY:-$(openssl rand -base64 32)}"

# Start postgres and conjur
docker compose up -d postgres conjur

# Wait for Conjur to be ready
echo "Waiting for Conjur to be ready..."
for i in $(seq 1 30); do
  if docker compose exec -T conjur curl -sf http://localhost:80 > /dev/null 2>&1; then
    echo "Conjur is ready."
    break
  fi
  if [ "$i" -eq 30 ]; then
    echo "Timed out waiting for Conjur."
    exit 1
  fi
  sleep 2
done

# Retrieve admin API key
export CONJUR_AUTHN_API_KEY
CONJUR_AUTHN_API_KEY=$(docker compose exec -T conjur conjurctl role retrieve-key conjur:user:admin)

# Set Conjur connection vars to point at the local conjur service
export CONJUR_APPLIANCE_URL="http://conjur"
export CONJUR_ACCOUNT="conjur"

# Write .netrc with admin credentials, matching conjur-api-go's approach
NETRC_FILE="$(mktemp)"
cat > "$NETRC_FILE" <<EOF
machine conjur
login admin
password ${CONJUR_AUTHN_API_KEY}
EOF
export CONJUR_NETRC_PATH="$NETRC_FILE"

# Enable Azure integration tests
export RUN_AZURE_TESTS="true"

# Delegate to build.sh which handles docker run with the correct
# env vars and netrc mount, now that CONJUR_APPLIANCE_URL,
# CONJUR_ACCOUNT, CONJUR_NETRC_PATH and RUN_AZURE_TESTS are all set.
"$(dirname "$0")/../build.sh"