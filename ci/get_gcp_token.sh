#!/bin/bash
set -euo pipefail

if [[ $# -lt 3 ]]; then
  echo "Usage: $0 <hostId> <account> <outputDir>"
  exit 1
fi

HOST_ID="$1"
ACCOUNT="$2"
OUTPUT_DIR="$3"

BASE_URL="http://metadata.google.internal/computeMetadata/v1"
IDENTITY_URL="$BASE_URL/instance/service-accounts/default/identity"
PROJECT_ID_URL="$BASE_URL/project/project-id"
METADATA_FLAVOR_HEADER="Metadata-Flavor: Google"

if [[ -z "$ACCOUNT" || -z "$HOST_ID" || -z "$OUTPUT_DIR" ]]; then
  echo "Usage: $0 <hostId> <account> <outputDir>"
  exit 1
fi

rm -rf "$OUTPUT_DIR" 2>/dev/null
mkdir -p "$OUTPUT_DIR"

AUDIENCE="conjur/$ACCOUNT/$HOST_ID"

# Fail on connection and HTTP errors; show errors on stderr.
if ! TOKEN="$(
  curl -sSf \
    -H "$METADATA_FLAVOR_HEADER" \
    --get \
    --data-urlencode "audience=$AUDIENCE" \
    --data-urlencode "format=full" \
    "$IDENTITY_URL"
)"; then
  echo "Failed to fetch the token from GCP metadata service."
  exit 1
fi

# Basic JWT shape validation (header.payload.signature)
if [[ ! "$TOKEN" =~ ^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$ ]]; then
  echo "Metadata service returned a non-JWT token payload."
  exit 1
fi

printf '%s\n' "$TOKEN" > "$OUTPUT_DIR/token"
echo "Token saved to $OUTPUT_DIR/token"

if ! GCP_PROJECT="$(curl -sSf -H "$METADATA_FLAVOR_HEADER" "$PROJECT_ID_URL")"; then
  echo "Failed to fetch project ID from GCP metadata service."
  exit 1
fi

if [[ -z "$GCP_PROJECT" ]]; then
  echo "Metadata service returned an empty project ID."
  exit 1
fi

printf '%s\n' "$GCP_PROJECT" > "$OUTPUT_DIR/project-id"
echo "Project ID saved to $OUTPUT_DIR/project-id"