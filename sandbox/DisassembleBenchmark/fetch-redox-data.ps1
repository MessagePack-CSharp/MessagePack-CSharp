# Downloads the twelve simdjson-data documents the RE:Dox benchmark corpus binds (RedoxBenchmark.cs)
# into ./redox-data (gitignored), pinned to the simdjson-data revision RE:Dox's submodule points at.
$ErrorActionPreference = 'Stop'
$revision = '4197c425e857f0ec38e89822fdd0bd9ea21f4daf'
$files = 'apache_builds', 'canada', 'citm_catalog', 'github_events', 'gsoc-2018', 'instruments',
         'marine_ik', 'mesh', 'numbers', 'random', 'twitter', 'update-center'
$dir = Join-Path $PSScriptRoot 'redox-data'
New-Item -ItemType Directory -Force $dir | Out-Null
foreach ($f in $files) {
    $target = Join-Path $dir "$f.json"
    if (Test-Path $target) { continue }
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/simdjson/simdjson-data/$revision/jsonexamples/$f.json" -OutFile $target
    Write-Host "fetched $f.json"
}
