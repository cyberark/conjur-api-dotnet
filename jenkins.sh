#!/bin/sh -e

finish() {
  if [ -n "$CIDFILE" ]; then
    CID=`cat $CIDFILE`
    docker rm -f $CID
    rm -f $CIDFILE
  fi
}
trap finish EXIT

# make sure the build env is up to date
make -C docker

TAG=`cat docker/tag`

if [ -z `docker images -q $TAG` ]; then
  # the image is not present, so pull or build
  docker pull $TAG || make -C docker rebuild
fi

# build
CIDFILE=`mktemp -u`
docker run -v $PWD:/src:ro --cidfile=$CIDFILE $TAG

CID=`cat $CIDFILE`

docker cp $CID:"/build/TestResult.xml" .
