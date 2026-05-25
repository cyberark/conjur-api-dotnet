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
ls -l "$NETRC_FILE"

# Azure test env (Go parity: values must come from summon -e azure)
export RUN_AZURE_TESTS="${RUN_AZURE_TESTS:-true}"

# Required vars for Azure tests
: "${AZURE_SUBSCRIPTION_ID:?AZURE_SUBSCRIPTION_ID is required}"
: "${AZURE_RESOURCE_GROUP:?AZURE_RESOURCE_GROUP is required}"
: "${USER_ASSIGNED_IDENTITY:?USER_ASSIGNED_IDENTITY is required}"
: "${USER_ASSIGNED_IDENTITY_CLIENT_ID:?USER_ASSIGNED_IDENTITY_CLIENT_ID is required}"

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

echo "Running test service via docker compose"
set +e
docker compose run --rm test || {
  rc=$?
  echo "Docker compose failed rc=$rc"
  docker compose run --rm --entrypoint sh test -c 'command -v dotnet; dotnet --info' || true
  exit $rc
}
rc=$?
set -e

if [ "$rc" -ne 0 ]; then
  echo "Test container failed with rc=$rc"
  echo "Dumping Conjur logs..."
  docker compose logs --no-color --timestamps --tail=200 conjur || true
  echo "Dumping Postgres logs..."
  docker compose logs --no-color --timestamps --tail=200 postgres || true
fi

echo "Docker compose run exit code=$rc"
exit $rc