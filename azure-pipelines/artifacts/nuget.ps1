$RepoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..")
$BuildConfiguration = $env:BUILDCONFIGURATION
if (!$BuildConfiguration) {
    $BuildConfiguration = 'Debug'
}

$result = @{}

$PackagesRoot = "$RepoRoot/bin/Packages/$BuildConfiguration/NuGet"
if (Test-Path $PackagesRoot) {
    $result[$PackagesRoot] = (Get-ChildItem $PackagesRoot -Recurse)
}

return $result
