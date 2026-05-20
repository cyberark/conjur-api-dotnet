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

# Create the 'conjur' account (creates conjur:user:admin and prints its API key).
echo "XXX ci/test.sh: Creating Conjur account 'conjur'..."
ADMIN_OUTPUT="$(docker compose exec -T conjur conjurctl account create conjur 2>&1 || true)"
echo "XXX ci/test.sh: account create output:"
echo "$ADMIN_OUTPUT"

CONJUR_AUTHN_API_KEY="$(echo "$ADMIN_OUTPUT" | awk -F': ' '/API key for admin/ {print $2; exit}' | tr -d '\r')"

# If the account already existed (re-run with persisted volumes), fall back to retrieve-key.
if [ -z "$CONJUR_AUTHN_API_KEY" ]; then
  echo "XXX ci/test.sh: account may already exist, falling back to retrieve-key"
  CONJUR_AUTHN_API_KEY="$(docker compose exec -T conjur \
      conjurctl role retrieve-key conjur:user:admin | tr -d '\r\n')"
fi

if [ -z "$CONJUR_AUTHN_API_KEY" ]; then
  echo "XXX ci/test.sh: ERROR could not obtain admin API key"
  exit 1
fi
export CONJUR_AUTHN_API_KEY
echo "XXX CONJUR_AUTHN_API_KEY set: ${CONJUR_AUTHN_API_KEY:0:4}****** (first 4 chars shown)"

# Set Conjur connection vars to point at the local conjur service
export CONJUR_APPLIANCE_URL="http://conjur"
echo "XXX CONJUR_APPLIANCE_URL set to: ${CONJUR_APPLIANCE_URL}"

export CONJUR_ACCOUNT="conjur"
echo "XXX CONJUR_ACCOUNT set to: ${CONJUR_ACCOUNT}"

# Write .netrc with admin credentials, matching conjur-api-go's approach.
NETRC_FILE="$(mktemp)"
cat > "$NETRC_FILE" <<EOF
machine conjur
login admin
password ${CONJUR_AUTHN_API_KEY}
EOF
chmod 600 "$NETRC_FILE"

export CONJUR_NETRC_PATH="$NETRC_FILE"
export CONJUR_NETRC_FILE="$NETRC_FILE"
echo "XXX .netrc written to: ${NETRC_FILE}"
echo "XXX CONJUR_NETRC_PATH=${CONJUR_NETRC_PATH}"
echo "XXX CONJUR_NETRC_FILE=${CONJUR_NETRC_FILE}"
ls -l "$NETRC_FILE"
echo "XXX netrc contents (password masked):"
sed 's/^password .*/password ******/' "$NETRC_FILE"

# Azure test env (Go-parity: do not require summon for these)
export RUN_AZURE_TESTS="${RUN_AZURE_TESTS:-true}"
export AZURE_SUBSCRIPTION_ID="${AZURE_SUBSCRIPTION_ID:-${INFRAPOOL_AZURE_SUBSCRIPTION_ID:-}}"
export AZURE_RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-${INFRAPOOL_AZURE_RESOURCE_GROUP:-}}"
export USER_ASSIGNED_IDENTITY="${USER_ASSIGNED_IDENTITY:-${INFRAPOOL_USER_ASSIGNED_IDENTITY:-}}"
export USER_ASSIGNED_IDENTITY_CLIENT_ID="${USER_ASSIGNED_IDENTITY_CLIENT_ID:-${INFRAPOOL_USER_ASSIGNED_IDENTITY_CLIENT_ID:-}}"

# IMDS fallback for required system-assigned test vars
if [ -z "${AZURE_SUBSCRIPTION_ID}" ]; then
  AZURE_SUBSCRIPTION_ID="$(curl -fsS --noproxy "*" -H Metadata:true \
    "http://169.254.169.254/metadata/instance/compute/subscriptionId?api-version=2021-02-01&format=text" || true)"
  export AZURE_SUBSCRIPTION_ID
fi

if [ -z "${AZURE_RESOURCE_GROUP}" ]; then
  AZURE_RESOURCE_GROUP="$(curl -fsS --noproxy "*" -H Metadata:true \
    "http://169.254.169.254/metadata/instance/compute/resourceGroupName?api-version=2021-02-01&format=text" || true)"
  export AZURE_RESOURCE_GROUP
fi

echo "XXX AZURE_SUBSCRIPTION_ID=${AZURE_SUBSCRIPTION_ID:+set}"
echo "XXX AZURE_RESOURCE_GROUP=${AZURE_RESOURCE_GROUP:+set}"
echo "XXX USER_ASSIGNED_IDENTITY=${USER_ASSIGNED_IDENTITY:+set}"
echo "XXX USER_ASSIGNED_IDENTITY_CLIENT_ID=${USER_ASSIGNED_IDENTITY_CLIENT_ID:+set}"
echo "XXX RUN_AZURE_TESTS=${RUN_AZURE_TESTS}"

if [ -z "${AZURE_SUBSCRIPTION_ID}" ] || [ -z "${AZURE_RESOURCE_GROUP}" ]; then
  echo "XXX ERROR: AZURE_SUBSCRIPTION_ID/AZURE_RESOURCE_GROUP are still empty."
  exit 1
fi

echo "XXX IMDS preflight from test container..."
docker compose run --rm --entrypoint bash test -lc '
  set -e
  echo "proxy env:"
  env | grep -Ei "proxy" || true

  echo "system-assigned IMDS call:"
  curl -sS -i --noproxy "*" -H Metadata:true \
    "http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https%3A%2F%2Fmanagement.azure.com%2F" | head -n 30

  if [ -n "${USER_ASSIGNED_IDENTITY_CLIENT_ID:-}" ]; then
    echo "user-assigned IMDS call:"
    curl -sS -i --noproxy "*" -H Metadata:true \
      "http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https%3A%2F%2Fmanagement.azure.com%2F&client_id=${USER_ASSIGNED_IDENTITY_CLIENT_ID}" | head -n 30
  fi
'

echo "XXX ci/test.sh: running test service via docker compose"
set +e
docker compose run --rm test
rc=$?
set -e

if [ "$rc" -ne 0 ]; then
  echo "XXX ci/test.sh: test container failed with rc=$rc"
  echo "XXX ci/test.sh: dumping Conjur logs..."
  docker compose logs --no-color --timestamps --tail=200 conjur || true
  echo "XXX ci/test.sh: dumping Postgres logs..."
  docker compose logs --no-color --timestamps --tail=200 postgres || true
fi

echo "XXX ci/test.sh: docker compose run exit code=$rc"
exit $rc