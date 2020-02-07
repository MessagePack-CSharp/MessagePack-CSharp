$RepoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..")
$BuildConfiguration = $env:BUILDCONFIGURATION
if (!$BuildConfiguration) {
    $BuildConfiguration = 'Debug'
}

$result = @{}

$UnityPackRoot = "$RepoRoot/bin"
if (Test-Path $UnityPackRoot) {
    $result[$UnityPackRoot] = (Get-ChildItem $UnityPackRoot/*.unitypackage)
}

return $result
