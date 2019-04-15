#!/bin/bash

while getopts l: opts; do
   case ${opts} in
      l) LOGFILE=${OPTARG} ;;
   esac
done

SCRIPT_DIR=$(dirname "$(realpath $0)")

if ! [[ -e "${SCRIPT_DIR}/Assets/Microsoft.VisualStudio.Threading.dll" ]] ; then
   ${SCRIPT_DIR}/link_assets.sh
fi

if ! [[ -v LOGFILE ]] ; then
    LOGFILE=${SCRIPT_DIR}/../../bin/build_logs/unity_package.log
fi
LOGDIR=$(dirname "${LOGFILE}")
if ! [ -d $LOGDIR ] ; then
   mkdir -p $LOGDIR
fi

echo Writing log to ${LOGFILE}

/opt/Unity/Editor/Unity \
    -batchmode \
    -nographics \
    -silent-crashes \
    -noUpm \
    -buildTarget standalone \
    -projectPath ${SCRIPT_DIR} \
    -executeMethod PackageExport.Export \
    -logfile ${LOGFILE}

$rc = $?

echo Log follows...

cat ${LOGFILE}

exit $rc
