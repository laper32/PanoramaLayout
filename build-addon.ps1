param(
    [Parameter(Mandatory = $true)]
    [string] $Cs2Root,

    [string] $AddonName = "panorama_layout"
)

$ErrorActionPreference = "Stop"

$projectRoot = $PSScriptRoot
$resourceCompiler = Join-Path $Cs2Root "game\bin\win64\resourcecompiler.exe"
$gameDir = Join-Path $Cs2Root "game\csgo"
$contentAddon = Join-Path $Cs2Root "content\csgo_addons\$AddonName"
$gameAddon = Join-Path $Cs2Root "game\csgo_addons\$AddonName"

$sourceLayout = Join-Path $projectRoot "addon\panorama\layout\custom_game\server_menu.xml"
$sourceStyle = Join-Path $projectRoot "addon\panorama\styles\custom_game\server_menu.css"
$layoutInput = Join-Path $contentAddon "panorama\layout\custom_game\server_menu.xml"
$styleInput = Join-Path $contentAddon "panorama\styles\custom_game\server_menu.css"
$layoutOutput = Join-Path $gameAddon "panorama\layout\custom_game\server_menu.vxml_c"
$styleOutput = Join-Path $gameAddon "panorama\styles\custom_game\server_menu.vcss_c"

foreach ($requiredFile in @($resourceCompiler, $sourceLayout, $sourceStyle)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "Required file not found: $requiredFile"
    }
}

New-Item -ItemType Directory -Force -Path (Split-Path $layoutInput), (Split-Path $styleInput) | Out-Null
Copy-Item -LiteralPath $sourceLayout -Destination $layoutInput -Force
Copy-Item -LiteralPath $sourceStyle -Destination $styleInput -Force

$panoramaDir = Join-Path $contentAddon "panorama"
New-Item -ItemType Directory -Force -Path $panoramaDir | Out-Null
Set-Content -LiteralPath (Join-Path $panoramaDir "preprocessor_config.txt") -Encoding ASCII -Value @'
"PanzipCfg"
{
    "BlockDefs"
    {
    }
}
'@

Set-Content -LiteralPath (Join-Path $contentAddon "addoninfo.txt") -Encoding UTF8 -Value @'
<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->
{
    IsPlayable = false
}
'@

& $resourceCompiler `
    -game $gameDir `
    -i $styleInput `
    -i $layoutInput `
    -f `
    -nop4 `
    -v

if ($LASTEXITCODE -ne 0) {
    throw "resourcecompiler failed with exit code $LASTEXITCODE"
}

foreach ($expectedOutput in @($layoutOutput, $styleOutput)) {
    if (-not (Test-Path -LiteralPath $expectedOutput -PathType Leaf)) {
        throw "Expected compiled resource not found: $expectedOutput"
    }
}

$strippedOutput = Join-Path $gameAddon "panorama_stripped"
if (Test-Path -LiteralPath $strippedOutput -PathType Container) {
    Remove-Item -LiteralPath $strippedOutput -Recurse -Force
}

Copy-Item -LiteralPath (Join-Path $contentAddon "addoninfo.txt") `
    -Destination (Join-Path $gameAddon "addoninfo.txt") `
    -Force

Write-Host "Built client Panorama addon: $gameAddon"
Write-Host "Layout resource: panorama/layout/custom_game/server_menu.vxml_c"
