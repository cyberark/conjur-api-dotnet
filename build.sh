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

TAG=$(cat docker/tag)

# When running in Jenkins, a VERSION file is automatically added to the repo root.
# When running locally, add one here.
if [ ! -f "$PWD/VERSION" ]; then
  echo "0.0.0-dev" > "$PWD/VERSION"
fi

CIDFILE=$(mktemp -u)
docker run \
  -v "$PWD":/src:ro \
  --cidfile="$CIDFILE" \
  -e WRITE_ARTIFACTORY_USERNAME \
  -e WRITE_ARTIFACTORY_PASSWORD \
  -e WRITE_ARTIFACTORY_URL \
  -e RUN_AWS_TESTS \
  "$TAG"

CID=$(cat "$CIDFILE")

docker cp "$CID":"/build/TestResults.xml" .
docker cp "$CID":"/build/Coverage.xml" .
mkdir -p bin
docker cp "$CID":"/build/conjur-api/bin/Release/net8.0/conjur-api.dll" bin/conjur-api.dll

cat TestResults.xml
