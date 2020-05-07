#!/bin/sh -xe

finish() {
  if [ -n "$CIDFILE" ]; then
    CID=`cat $CIDFILE`
    docker rm -f $CID
    rm -f $CIDFILE
  fi
}
trap finish EXIT

TAG=`cat docker/tag`

CIDFILE=`mktemp -u`
docker run -v $PWD:/src:ro --cidfile=$CIDFILE $TAG

CID=`cat $CIDFILE`

docker cp $CID:"/build/TestResult.xml" .
mkdir -p bin
docker cp $CID:"/build/conjur-api/bin/Release/conjur-api.dll" bin/conjur-api.dll
