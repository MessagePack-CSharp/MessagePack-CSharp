$RepoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..")
$BuildConfiguration = $env:BUILDCONFIGURATION
if (!$BuildConfiguration) {
    $BuildConfiguration = 'Debug'
}

$MpcBin = "$RepoRoot/bin/MessagePack.Generator/$BuildConfiguration/netcoreapp3.0/osx-x64/publish"

if (!(Test-Path $MpcBin))  { return }

@{
    "$MpcBin" = (Get-ChildItem $MpcBin)
}
