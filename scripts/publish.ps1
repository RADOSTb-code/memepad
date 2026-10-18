param([string]$Version = "0.1.0")
# Builds a self-contained single-file MemePad.exe for win-x64 and zips it with LICENSE and README.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root "artifacts\publish"
$zip = Join-Path $root "artifacts\MemePad-$Version-win-x64.zip"

if (Test-Path $out) { Remove-Item -Recurse -Force $out }
dotnet publish (Join-Path $root "src\MemePad.App\MemePad.App.csproj") -c Release -r win-x64 --self-contained `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
    -p:Version=$Version -o $out
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Copy-Item (Join-Path $root "LICENSE"), (Join-Path $root "README.md") $out
Get-ChildItem $out -Filter *.pdb | Remove-Item
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path (Join-Path $out "*") -DestinationPath $zip
"Created $zip ($([math]::Round((Get-Item $zip).Length / 1MB, 1)) MB)"
