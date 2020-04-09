$RepoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..")
$BuildConfiguration = $env:BUILDCONFIGURATION
if (!$BuildConfiguration) {
    $BuildConfiguration = 'Debug'
}

$result = @{}

$VsixRoot = "$RepoRoot/bin/MessagePackAnalyzer.Vsix/$BuildConfiguration"
if (Test-Path $VsixRoot) {
    $result[$VsixRoot] = (Get-ChildItem "$VsixRoot/*vsix")
}

return $result
