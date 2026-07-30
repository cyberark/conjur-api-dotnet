#!/usr/bin/env bash
set -euo pipefail

# Check if the dotnet-builder:dev image exists
if ! docker image inspect dotnet-builder:dev > /dev/null 2>&1;
then
  echo "Docker image dotnet-builder:dev not found. Building it now..."
  docker build -t dotnet-builder:dev ./docker/
fi

docker run \
  -v "$PWD":/src:ro \
  -v "$ASSET_DIR":/artifacts:ro \
  -e NUGET_API_KEY \
  --entrypoint "/release.sh" \
  dotnet-builder:dev
