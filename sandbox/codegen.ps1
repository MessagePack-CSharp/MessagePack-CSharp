$codeGenScripts =
    "$PSScriptRoot/Sandbox/codegen.ps1",
    "$PSScriptRoot/TestData2/codegen.ps1"

$exitCode = 0
$codeGenScripts | % {
    & $_
    if ($LASTEXITCODE -ne 0) { $exitCode = 1 }
}

exit $exitCode
