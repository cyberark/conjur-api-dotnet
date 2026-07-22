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
echo "Starting postgres and conjur containers..."
docker compose up -d postgres conjur

# Wait for Conjur to be ready
echo "Waiting for Conjur to be ready..."
for i in $(seq 1 30); do
  if docker compose exec -T conjur curl -sf http://localhost:80 > /dev/null 2>&1; then
    echo "Conjur is ready after ${i} attempt(s)."
    break
  fi
  if [ "$i" -eq 30 ]; then
    echo "Timed out waiting for Conjur."
    exit 1
  fi
  sleep 2
done

# Create the 'conjur' account (creates conjur:user:admin and prints its API key).
echo "Creating Conjur account 'conjur'..."
ADMIN_OUTPUT="$(docker compose exec -T conjur conjurctl account create conjur 2>&1 || true)"

CONJUR_AUTHN_API_KEY="$(echo "$ADMIN_OUTPUT" | awk -F': ' '/API key for admin/ {print $2; exit}' | tr -d '\r')"

# If the account already existed (re-run with persisted volumes), fall back to retrieve-key.
if [ -z "$CONJUR_AUTHN_API_KEY" ]; then
  echo "Conjur account may already exist, falling back to retrieve-key"
  CONJUR_AUTHN_API_KEY="$(docker compose exec -T conjur \
      conjurctl role retrieve-key conjur:user:admin | tr -d '\r\n')"
fi

if [ -z "$CONJUR_AUTHN_API_KEY" ]; then
  echo "ERROR could not obtain admin API key"
  exit 1
fi
export CONJUR_AUTHN_API_KEY

# Set Conjur connection vars to point at the local conjur service
export CONJUR_APPLIANCE_URL="http://conjur"

export CONJUR_ACCOUNT="conjur"

# Write .netrc with admin credentials.
NETRC_FILE="$(mktemp)"
cat > "$NETRC_FILE" <<EOF
machine conjur
login admin
password ${CONJUR_AUTHN_API_KEY}
EOF
chmod 600 "$NETRC_FILE"

export CONJUR_NETRC_PATH="$NETRC_FILE"
export CONJUR_NETRC_FILE="$NETRC_FILE"
ls -l "$NETRC_FILE"

# modes
export RUN_AZURE_TESTS="${RUN_AZURE_TESTS:-false}"
export TEST_GCP="${TEST_GCP:-${INFRAPOOL_TEST_GCP:-false}}"

# Azure env
export AZURE_SUBSCRIPTION_ID="${AZURE_SUBSCRIPTION_ID:-${INFRAPOOL_AZURE_SUBSCRIPTION_ID:-}}"
export AZURE_RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-${INFRAPOOL_AZURE_RESOURCE_GROUP:-}}"
export USER_ASSIGNED_IDENTITY="${USER_ASSIGNED_IDENTITY:-${INFRAPOOL_USER_ASSIGNED_IDENTITY:-}}"
export USER_ASSIGNED_IDENTITY_CLIENT_ID="${USER_ASSIGNED_IDENTITY_CLIENT_ID:-${INFRAPOOL_USER_ASSIGNED_IDENTITY_CLIENT_ID:-}}"

# GCP env from stashed files
if [ "${TEST_GCP}" = "true" ]; then
  GCP_CTX_DIR="${GCP_CTX_DIR:-gcp}"
  if [ -f "${GCP_CTX_DIR}/project-id" ]; then
    read -r GCP_PROJECT_ID < "${GCP_CTX_DIR}/project-id"
    export GCP_PROJECT_ID
  fi
  if [ -f "${GCP_CTX_DIR}/token" ]; then
    read -r GCP_ID_TOKEN < "${GCP_CTX_DIR}/token"
    export GCP_ID_TOKEN
  fi

  : "${GCP_PROJECT_ID:?GCP_PROJECT_ID is required for GCP tests}"
  : "${GCP_ID_TOKEN:?GCP_ID_TOKEN is required for GCP tests}"
fi

# Azure checks only when Azure mode is enabled
if [ "${RUN_AZURE_TESTS}" = "true" ]; then
  : "${AZURE_SUBSCRIPTION_ID:?AZURE_SUBSCRIPTION_ID is required}"
  : "${AZURE_RESOURCE_GROUP:?AZURE_RESOURCE_GROUP is required}"
  : "${USER_ASSIGNED_IDENTITY:?USER_ASSIGNED_IDENTITY is required}"
  : "${USER_ASSIGNED_IDENTITY_CLIENT_ID:?USER_ASSIGNED_IDENTITY_CLIENT_ID is required}"
fi

echo "Running tests via docker compose..."
set +e
docker compose run --rm test
rc=$?
set -e

if [ "$rc" -ne 0 ]; then
  echo "ci/test.sh: test container failed with rc=$rc"
  echo "ci/test.sh: dumping Conjur logs..."
  docker compose logs --no-color --timestamps --tail=200 conjur || true
  echo "ci/test.sh: dumping Postgres logs..."
  docker compose logs --no-color --timestamps --tail=200 postgres || true
fi

exit $rc