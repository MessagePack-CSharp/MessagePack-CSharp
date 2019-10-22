#!/bin/bash

# Link in the binaries we build or restore, that Unity expects inside its Assets directory.

if ! [[ -v BuildConfiguration ]] ; then BuildConfiguration=Release; fi

SCRIPT_DIR=$(dirname "$(realpath $0)")
SRCDIR=${SCRIPT_DIR}/../../bin/MessagePack/${BuildConfiguration}/netstandard2.0/publish

if ! [ -d "${SRCDIR}" ] ; then
    dotnet publish "${SCRIPT_DIR}/../MessagePack" -c ${BuildConfiguration} -f netstandard2.0
fi

pushd ${SCRIPT_DIR}

ln -s "${SRCDIR}/Microsoft.VisualStudio.Validation.dll" "Assets/Microsoft.VisualStudio.Validation.dll"
ln -s "${SRCDIR}/Microsoft.VisualStudio.Threading.dll" "Assets/Microsoft.VisualStudio.Threading.dll"
ln -s "${SRCDIR}/Nerdbank.Streams.dll" "Assets/Nerdbank.Streams.dll"
ln -s "${SRCDIR}/System.IO.Pipelines.dll" "Assets/System.IO.Pipelines.dll"
ln -s "${SRCDIR}/System.Buffers.dll" "Assets/System.Buffers.dll"
ln -s "${SRCDIR}/System.Memory.dll" "Assets/System.Memory.dll"
ln -s "${SRCDIR}/System.Runtime.CompilerServices.Unsafe.dll" "Assets/System.Runtime.CompilerServices.Unsafe.dll"
ln -s "${SRCDIR}/System.Threading.Tasks.Extensions.dll" "Assets/System.Threading.Tasks.Extensions.dll"

popd
