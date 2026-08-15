param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectDir,

    [Parameter(Mandatory = $true)]
    [string]$PckPath
)

$ErrorActionPreference = 'Stop'

$requiredResources = @(
    'ReAstralPartyMod/images/ui/global_switching_settings.png',
    'ReAstralPartyMod/images/ui/person_choose.png',
    'ReAstralPartyMod/images/background/starting_person_pelic_selection_screen.png',
    'ReAstralPartyMod/images/enchantments/mini_lulu_enchantment.png',
    'ReAstralPartyMod/images/enchantments/astral_cooldown_enchantment.png',
    'ReAstralPartyMod/images/enchantments/essence_eye_of_sun_enchantment.png',
    'ReAstralPartyMod/images/enchantments/essence_paranoid_enchantment.png',
    'ReAstralPartyMod/images/enchantments/essence_sacred_faith_enchantment.png',
    'ReAstralPartyMod/images/enchantments/tetra_warforge_enchantment.png'
)

if (-not (Test-Path -LiteralPath $PckPath -PathType Leaf)) {
    throw "PCK was not produced: $PckPath"
}

$pckBytes = [System.IO.File]::ReadAllBytes($PckPath)
$pckText = [System.Text.Encoding]::GetEncoding(28591).GetString($pckBytes)
foreach ($relativePath in $requiredResources) {
    $resourcePath = Join-Path $ProjectDir $relativePath
    if (-not (Test-Path -LiteralPath $resourcePath -PathType Leaf)) {
        throw "Required resource is missing: $relativePath"
    }

    $pngBytes = [System.IO.File]::ReadAllBytes($resourcePath)
    if ($pngBytes.Length -lt 8 -or
        $pngBytes[0] -ne 0x89 -or $pngBytes[1] -ne 0x50 -or
        $pngBytes[2] -ne 0x4E -or $pngBytes[3] -ne 0x47) {
        throw "Required resource is not a valid PNG: $relativePath"
    }

    $importPath = "$resourcePath.import"
    $importText = Get-Content -LiteralPath $importPath -Raw
    if ($importText -notmatch '(?m)^\[remap\]$' -or
        $importText -notmatch '(?m)^importer="texture"$' -or
        $importText -notmatch '(?m)^source_file="res://') {
        throw "Godot import metadata is incomplete: $relativePath.import"
    }

    $ctexMatch = [regex]::Match($importText, 'res://\.godot/imported/([^"\]]+\.ctex)')
    if (-not $ctexMatch.Success -or -not (Test-Path -LiteralPath (Join-Path $ProjectDir ".godot/imported/$($ctexMatch.Groups[1].Value)") -PathType Leaf)) {
        throw "Godot imported texture is missing: $relativePath"
    }

    if ($pckText.IndexOf("res://$relativePath", [System.StringComparison]::Ordinal) -lt 0) {
        throw "Exported PCK does not contain required resource path: res://$relativePath"
    }
}

Write-Host "Validated $($requiredResources.Count) required exported Godot resources."
