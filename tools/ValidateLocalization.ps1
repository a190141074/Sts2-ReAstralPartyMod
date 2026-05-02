param(
    [Parameter(Mandatory = $true)]
    [string]$LocalizationDir
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $LocalizationDir)) {
    throw "Localization directory not found: $LocalizationDir"
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false, $true)
$jsonFiles = Get-ChildItem -LiteralPath $LocalizationDir -Filter '*.json' -File | Sort-Object Name

if (-not $jsonFiles) {
    Write-Host "No JSON localization files found under $LocalizationDir"
    exit 0
}

foreach ($file in $jsonFiles) {
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        throw "Localization file contains UTF-8 BOM: $($file.FullName)"
    }

    try {
        $text = $utf8NoBom.GetString($bytes)
    }
    catch {
        throw "Localization file is not valid UTF-8: $($file.FullName)`n$($_.Exception.Message)"
    }

    try {
        $null = $text | ConvertFrom-Json
    }
    catch {
        throw "Localization file is not valid JSON: $($file.FullName)`n$($_.Exception.Message)"
    }
}

Write-Host "Chinese localization validated:" $jsonFiles.Count "files"
