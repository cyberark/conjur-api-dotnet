#!/bin/sh -e

# make sure the build env is up to date
make -C docker

TAG=`cat docker/tag`

if [ -z `docker images -q $TAG` ]; then
  # the image is not present, so pull or build
  docker pull $TAG || make -C docker rebuild
fi

./build.sh
[ "$1" == "--no-sign" ] || ./sign.sh
