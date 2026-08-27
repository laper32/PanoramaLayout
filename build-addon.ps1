param(
    [Parameter(Mandatory = $true)]
    [string] $Cs2Root,

    [string] $AddonName = "panorama_layout",

    [string] $DeployAssetsPath
)

$ErrorActionPreference = "Stop"

$projectRoot = $PSScriptRoot
$resourceCompiler = Join-Path $Cs2Root "game\bin\win64\resourcecompiler.exe"
$gameDir = Join-Path $Cs2Root "game\csgo"
$contentAddon = Join-Path $Cs2Root "content\csgo_addons\$AddonName"
$gameAddon = Join-Path $Cs2Root "game\csgo_addons\$AddonName"

$resources = @(
    @{
        Source = Join-Path $projectRoot "addon\panorama\layout\custom_game\server_menu.xml"
        Input = Join-Path $contentAddon "panorama\layout\custom_game\server_menu.xml"
        Output = Join-Path $gameAddon "panorama\layout\custom_game\server_menu.vxml_c"
        RelativeOutput = "panorama\layout\custom_game\server_menu.vxml_c"
    },
    @{
        Source = Join-Path $projectRoot "addon\panorama\styles\custom_game\server_menu.css"
        Input = Join-Path $contentAddon "panorama\styles\custom_game\server_menu.css"
        Output = Join-Path $gameAddon "panorama\styles\custom_game\server_menu.vcss_c"
        RelativeOutput = "panorama\styles\custom_game\server_menu.vcss_c"
    },
    @{
        Source = Join-Path $projectRoot "addon\panorama\layout\custom_game\zeus_hub.xml"
        Input = Join-Path $contentAddon "panorama\layout\custom_game\zeus_hub.xml"
        Output = Join-Path $gameAddon "panorama\layout\custom_game\zeus_hub.vxml_c"
        RelativeOutput = "panorama\layout\custom_game\zeus_hub.vxml_c"
    },
    @{
        Source = Join-Path $projectRoot "addon\panorama\styles\custom_game\zeus_hub.css"
        Input = Join-Path $contentAddon "panorama\styles\custom_game\zeus_hub.css"
        Output = Join-Path $gameAddon "panorama\styles\custom_game\zeus_hub.vcss_c"
        RelativeOutput = "panorama\styles\custom_game\zeus_hub.vcss_c"
    }
)

foreach ($requiredFile in @($resourceCompiler) + $resources.Source) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "Required file not found: $requiredFile"
    }
}

foreach ($resource in $resources) {
    New-Item -ItemType Directory -Force -Path (Split-Path $resource.Input) | Out-Null
    Copy-Item -LiteralPath $resource.Source -Destination $resource.Input -Force
}

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

$compilerArguments = @("-game", $gameDir)
foreach ($resource in $resources) {
    $compilerArguments += @("-i", $resource.Input)
}
$compilerArguments += @("-f", "-nop4", "-v")

& $resourceCompiler @compilerArguments

if ($LASTEXITCODE -ne 0) {
    throw "resourcecompiler failed with exit code $LASTEXITCODE"
}

foreach ($expectedOutput in $resources.Output) {
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
foreach ($resource in $resources) {
    Write-Host "Resource: $($resource.RelativeOutput.Replace('\', '/'))"
}

if (-not [string]::IsNullOrWhiteSpace($DeployAssetsPath)) {
    $resolvedDeployPath = [System.IO.Path]::GetFullPath($DeployAssetsPath)
    if (-not (Test-Path -LiteralPath $resolvedDeployPath -PathType Container)) {
        throw "Deploy assets directory not found: $resolvedDeployPath"
    }

    foreach ($resource in $resources) {
        $destination = Join-Path $resolvedDeployPath $resource.RelativeOutput
        New-Item -ItemType Directory -Force -Path (Split-Path $destination) | Out-Null
        Copy-Item -LiteralPath $resource.Output -Destination $destination -Force
    }

    Write-Host "Deployed compiled Panorama resources: $resolvedDeployPath"
}
