$RepoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..")
$BuildConfiguration = $env:BUILDCONFIGURATION
if (!$BuildConfiguration) {
    $BuildConfiguration = 'Debug'
}

$ArtifactBasePath = "$RepoRoot\obj\_artifacts"
$mpcArtifactPath = "$ArtifactBasePath\mpc"
if (-not (Test-Path "$mpcArtifactPath/linux")) { New-Item -ItemType Directory -Path "$mpcArtifactPath/linux" | Out-Null }
if (-not (Test-Path "$mpcArtifactPath/osx")) { New-Item -ItemType Directory -Path "$mpcArtifactPath/osx" | Out-Null }
if (-not (Test-Path "$mpcArtifactPath/win")) { New-Item -ItemType Directory -Path "$mpcArtifactPath/win" | Out-Null }

Copy-Item "$RepoRoot/bin/MessagePack.Generator/$BuildConfiguration/netcoreapp3.0/linux-x64/publish/*" "$mpcArtifactPath/linux" -Container
Copy-Item "$RepoRoot/bin/MessagePack.Generator/$BuildConfiguration/netcoreapp3.0/osx-x64/publish/*" "$mpcArtifactPath/osx" -Container
Copy-Item "$RepoRoot/bin/MessagePack.Generator/$BuildConfiguration/netcoreapp3.0/win-x64/publish/*" "$mpcArtifactPath/win" -Container

@{
    "$mpcArtifactPath" = (Get-ChildItem $mpcArtifactPath -Recurse);
}
