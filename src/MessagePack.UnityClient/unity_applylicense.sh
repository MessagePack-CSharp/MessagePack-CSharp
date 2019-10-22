#!/bin/bash

while getopts i:l: opts; do
   case ${opts} in
      i) ULF=${OPTARG} ;;
      l) LOGFILE=${OPTARG} ;;
   esac
done

if ! [[ -v ULF ]] ; then
    echo Missing required -i parameter.
    exit 1
fi

if [[ -v LOGFILE ]] ; then
    echo Writing log to ${LOGFILE}
fi

/opt/Unity/Editor/Unity -batchmode -quit -nographics -logfile ${LOGFILE} -manualLicenseFile ${ULF}
