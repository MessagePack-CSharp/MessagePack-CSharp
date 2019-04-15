#!/bin/bash

while getopts l: opts; do
   case ${opts} in
      l) LOGFILE=${OPTARG} ;;
   esac
done

if [[ -v LOGFILE ]] ; then
    echo Writing log to ${LOGFILE}
fi

/opt/Unity/Editor/Unity -batchmode -quit -nographics -logfile ${LOGFILE} -createManualActivationFile
