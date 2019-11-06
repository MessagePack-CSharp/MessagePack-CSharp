#!/bin/bash

if ! [[ -v BUILDCONFIGURATION ]] ; then
  BUILDCONFIGURATION=release
fi

SCRIPT_DIR=$(dirname "$(realpath $0)")

if ! [[ -e "${SCRIPT_DIR}/../../bin/MessagePack/${BUILDCONFIGURATION}/netstandard2.0/publish" ]] ; then
    dotnet publish "${SCRIPT_DIR}/../MessagePack" -c ${BUILDCONFIGURATION} -f netstandard2.0
    dotnetExitCode=$?
    if [ $dotnetExitCode -ne 0 ] ; then
        exit $dotnetExitCode
    fi
fi

if ! [[ -d "${SCRIPT_DIR}/Assets/Plugins" ]] ; then
  mkdir -p ${SCRIPT_DIR}/Assets/Plugins
fi

cp ${SCRIPT_DIR}/../../bin/MessagePack/${BUILDCONFIGURATION}/netstandard2.0/publish/Nerdbank.Streams.dll ${SCRIPT_DIR}/Assets/Plugins/Nerdbank.Streams.dll
cp ${SCRIPT_DIR}/../../bin/MessagePack/${BUILDCONFIGURATION}/netstandard2.0/publish/System.Buffers.dll ${SCRIPT_DIR}/Assets/Plugins/System.Buffers.dll
cp ${SCRIPT_DIR}/../../bin/MessagePack/${BUILDCONFIGURATION}/netstandard2.0/publish/System.Memory.dll ${SCRIPT_DIR}/Assets/Plugins/System.Memory.dll
cp ${SCRIPT_DIR}/../../bin/MessagePack/${BUILDCONFIGURATION}/netstandard2.0/publish/System.Runtime.CompilerServices.Unsafe.dll ${SCRIPT_DIR}/Assets/Plugins/System.Runtime.CompilerServices.Unsafe.dll
cp ${SCRIPT_DIR}/../../bin/MessagePack/${BUILDCONFIGURATION}/netstandard2.0/publish/System.Threading.Tasks.Extensions.dll ${SCRIPT_DIR}/Assets/Plugins/System.Threading.Tasks.Extensions.dll
