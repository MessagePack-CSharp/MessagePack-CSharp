$RepoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..")
$BuildConfiguration = $env:BUILDCONFIGURATION
if (!$BuildConfiguration) {
    $BuildConfiguration = 'Debug'
}

$mpcArtifactPath = "$RepoRoot/bin/MessagePack.Generator/$BuildConfiguration/netcoreapp3.0"

@{
    $mpcArtifactPath = (Get-ChildItem $mpcArtifactPath -Recurse);
}
