#!/bin/sh -xe

TAG=`cat docker/tag`
CIDFILE=`mktemp -u`

finish() {
  if [ -n "$CIDFILE" ]; then
    CID=`cat $CIDFILE`
    docker rm -f $CID
    rm -f $CIDFILE
  fi
}
trap finish EXIT

conjur env run --yaml '{
  PKCS: !var authenticode/pkcs12,
  PKCS_PASS: !var authenticode/password,
  SN: !var dotnet/strongname-key
}' -- docker run \
  -v $PWD/bin/conjur-api.dll:/conjur-api.dll:ro \
  -v $PWD/docker/sign.sh:/sign.sh:ro \
  --cidfile=$CIDFILE \
  -e PKCS \
  -e PKCS_PASS \
  -e SN \
  $TAG /sign.sh

CID=`cat $CIDFILE`
docker cp $CID:/conjur-api.dll.signed bin/conjur-api.dll
