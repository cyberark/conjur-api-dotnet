#!/bin/sh -xe

BIN=/conjur-api.dll

PFX=`mktemp -p /dev/shm`
PASS=`mktemp -p /dev/shm`
SNKEY=`mktemp -p /dev/shm`

finish() {
  rm -f $PFX $PASS $SNKEY
}
trap finish EXIT

umask 077
set +x # don't show the keys in logs
echo "$PKCS" | base64 -d > $PFX
echo "$SN" | base64 -d > $SNKEY
echo -n "$PKCS_PASS" > $PASS
set -x
umask 022

cp $BIN $BIN.unsigned

sn -R $BIN.unsigned $SNKEY

osslsigncode -pkcs12 $PFX -in $BIN.unsigned \
  -out $BIN.signed -h sha2 -readpass $PASS \
  -n "Conjur .NET API" -i https://www.conjur.net -comm \
  -t http://timestamp.verisign.com/scripts/timstamp.dll
