param(
    [switch]$Check,
    [string]$InstalledRoot = 'C:\Users\LuoYe\.codex\skills'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$skillNames = @('sts2-ritsulib-modding', 'sts2-reastralparty-modding')

foreach ($name in $skillNames) {
    $source = Join-Path $repoRoot $name
    $target = Join-Path $InstalledRoot $name
    if (-not (Test-Path $source)) { throw "Missing repository skill: $source" }

    $sourceFiles = Get-ChildItem -LiteralPath $source -Recurse -File
    $targetFiles = if (Test-Path $target) { Get-ChildItem -LiteralPath $target -Recurse -File } else { @() }
    $sourceByRelative = @{}
    $targetByRelative = @{}
    foreach ($file in $sourceFiles) { $sourceByRelative[$file.FullName.Substring($source.Length + 1)] = $file }
    foreach ($file in $targetFiles) { $targetByRelative[$file.FullName.Substring($target.Length + 1)] = $file }

    $relativePaths = @($sourceByRelative.Keys) + @($targetByRelative.Keys) | Sort-Object -Unique
    foreach ($relative in $relativePaths) {
        $sourceFile = $sourceByRelative[$relative]
        $targetFile = $targetByRelative[$relative]
        if ($null -eq $sourceFile -or $null -eq $targetFile) {
            if ($Check) { throw "Mirror file set differs: $name\$relative" }
            continue
        }
        if ((Get-FileHash $sourceFile.FullName).Hash -ne (Get-FileHash $targetFile.FullName).Hash) {
            if ($Check) { throw "Mirror content differs: $name\$relative" }
        }
    }

    if (-not $Check) {
        New-Item -ItemType Directory -Path $target -Force | Out-Null
        foreach ($file in $sourceFiles) {
            $relative = $file.FullName.Substring($source.Length + 1)
            $destination = Join-Path $target $relative
            $destinationDirectory = Split-Path -Parent $destination
            New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
            Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
        }
        foreach ($file in $targetFiles) {
            $relative = $file.FullName.Substring($target.Length + 1)
            if (-not $sourceByRelative.ContainsKey($relative)) {
                Remove-Item -LiteralPath $file.FullName -Force
            }
        }
        Write-Output "Synced $name"
    }
}

if ($Check) { Write-Output 'Skill mirrors match.' }
