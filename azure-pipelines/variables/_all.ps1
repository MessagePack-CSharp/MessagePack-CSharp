#!/usr/bin/env pwsh

# This script returns a hashtable of build variables that should be set
# at the start of a build or release definition's execution.

$vars = @{}

Get-ChildItem "$PSScriptRoot\*.ps1" -Exclude "_*" |% {
    Write-Host "Computing $($_.BaseName) variable"
    $vars[$_.BaseName] = & $_
}

$vars
