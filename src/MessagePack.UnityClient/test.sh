#!/bin/bash

while getopts r:l: opts; do
   case ${opts} in
      l) LOGFILE=${OPTARG} ;;
      r) TESTRESULTS=${OPTARG} ;;
   esac
done

SCRIPT_DIR=$(dirname "$(realpath $0)")
if ! [[ -v LOGFILE ]] ; then
    LOGFILE=${SCRIPT_DIR}/../../bin/build_logs/unity_tests.log
fi
if ! [[ -v TESTRESULTS ]] ; then
    TESTRESULTS=${SCRIPT_DIR}/../../bin/build_logs/unity_testresults.xml
fi

echo Writing log to ${LOGFILE}

/opt/Unity/Editor/Unity \
    -batchmode \
    -quit \
    -nographics \
    -silent-crashes \
    -noUpm \
    -buildTarget standalone \
    -projectPath ${SCRIPT_DIR} \
    -runEditorTests
    -editorTestsResultFile ${TESTRESULTS} \
    -logfile ${LOGFILE}
