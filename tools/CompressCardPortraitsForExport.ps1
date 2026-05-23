param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("prepare", "restore")]
    [string]$Mode,

    [Parameter(Mandatory = $true)]
    [string]$SourceDir,

    [Parameter(Mandatory = $true)]
    [string]$StateDir,

    [string]$ImportedDir = "",

    [double]$Scale = 0.5
)

$ErrorActionPreference = "Stop"

function Ensure-Directory([string]$Path)
{
    if (-not (Test-Path -LiteralPath $Path))
    {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Get-SourceBackupDir([string]$BaseStateDir)
{
    return Join-Path $BaseStateDir "card_portraits_backup"
}

function Get-ImportedBackupDir([string]$BaseStateDir)
{
    return Join-Path $BaseStateDir "imported_backup"
}

function Resize-PngInPlace([string]$Path, [double]$ResizeScale)
{
    $inputStream = $null
    $outputStream = $null
    $tempPath = "$Path.tmp"

    try
    {
        $inputStream = [System.IO.File]::OpenRead($Path)
        $decoder = New-Object System.Windows.Media.Imaging.PngBitmapDecoder(
            $inputStream,
            [System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat,
            [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
        $frame = $decoder.Frames[0]

        $targetWidth = [Math]::Max(1, [int][Math]::Round($frame.PixelWidth * $ResizeScale))
        $targetHeight = [Math]::Max(1, [int][Math]::Round($frame.PixelHeight * $ResizeScale))
        if ($targetWidth -eq $frame.PixelWidth -and $targetHeight -eq $frame.PixelHeight)
        {
            return
        }

        $scaleX = $targetWidth / [double]$frame.PixelWidth
        $scaleY = $targetHeight / [double]$frame.PixelHeight
        $transform = New-Object System.Windows.Media.ScaleTransform($scaleX, $scaleY)
        $bitmap = New-Object System.Windows.Media.Imaging.TransformedBitmap($frame, $transform)

        $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
        $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))

        $outputStream = [System.IO.File]::Create($tempPath)
        $encoder.Save($outputStream)
    }
    finally
    {
        if ($outputStream -ne $null)
        {
            $outputStream.Dispose()
        }

        if ($inputStream -ne $null)
        {
            $inputStream.Dispose()
        }
    }

    Move-Item -LiteralPath $tempPath -Destination $Path -Force
}

function Backup-ImportedCardPortraitArtifacts([string]$PortraitDir, [string]$ImportedRoot, [string]$ImportedBackupDir)
{
    if ([string]::IsNullOrWhiteSpace($ImportedRoot) -or -not (Test-Path -LiteralPath $ImportedRoot))
    {
        return
    }

    Ensure-Directory $ImportedBackupDir
    $pngFiles = Get-ChildItem -LiteralPath $PortraitDir -Filter "*.png" -File
    foreach ($file in $pngFiles)
    {
        foreach ($importedFile in Get-ChildItem -LiteralPath $ImportedRoot -File | Where-Object { $_.Name -like "$($file.Name)-*" })
        {
            Copy-Item -LiteralPath $importedFile.FullName -Destination (Join-Path $ImportedBackupDir $importedFile.Name) -Force
        }
    }
}

function Restore-ImportedCardPortraitArtifacts([string]$ImportedRoot, [string]$ImportedBackupDir)
{
    if ([string]::IsNullOrWhiteSpace($ImportedRoot) -or -not (Test-Path -LiteralPath $ImportedRoot) -or -not (Test-Path -LiteralPath $ImportedBackupDir))
    {
        return
    }

    foreach ($backupFile in Get-ChildItem -LiteralPath $ImportedBackupDir -File)
    {
        Copy-Item -LiteralPath $backupFile.FullName -Destination (Join-Path $ImportedRoot $backupFile.Name) -Force
    }
}

function Prepare-CardPortraits([string]$PortraitDir, [string]$BackupStateDir, [string]$ImportedRoot, [double]$ResizeScale)
{
    if ($ResizeScale -le 0.0 -or $ResizeScale -gt 1.0)
    {
        throw "Scale must be in the range (0, 1]."
    }

    if (-not (Test-Path -LiteralPath $PortraitDir))
    {
        throw "Source directory not found: $PortraitDir"
    }

    Add-Type -AssemblyName PresentationCore
    Add-Type -AssemblyName WindowsBase

    if (Test-Path -LiteralPath $BackupStateDir)
    {
        Remove-Item -LiteralPath $BackupStateDir -Recurse -Force
    }

    Ensure-Directory $BackupStateDir
    $sourceBackupDir = Get-SourceBackupDir $BackupStateDir
    $importedBackupDir = Get-ImportedBackupDir $BackupStateDir

    Copy-Item -LiteralPath $PortraitDir -Destination $sourceBackupDir -Recurse -Force
    Backup-ImportedCardPortraitArtifacts -PortraitDir $PortraitDir -ImportedRoot $ImportedRoot -ImportedBackupDir $importedBackupDir

    $pngFiles = Get-ChildItem -LiteralPath $PortraitDir -Filter "*.png" -File
    $originalTotalBytes = ($pngFiles | Measure-Object Length -Sum).Sum

    foreach ($file in $pngFiles)
    {
        Resize-PngInPlace -Path $file.FullName -ResizeScale $ResizeScale
    }

    $compressedTotalBytes = (Get-ChildItem -LiteralPath $PortraitDir -Filter "*.png" -File | Measure-Object Length -Sum).Sum
    @(
        "files=$($pngFiles.Count)"
        "original_bytes=$originalTotalBytes"
        "compressed_bytes=$compressedTotalBytes"
        "scale=$ResizeScale"
    ) | Set-Content -LiteralPath (Join-Path $BackupStateDir "summary.txt")

    Write-Host ("Prepared {0} card portraits for export. {1:N2} MB -> {2:N2} MB (scale={3})." -f
        $pngFiles.Count,
        ($originalTotalBytes / 1MB),
        ($compressedTotalBytes / 1MB),
        $ResizeScale)
}

function Restore-CardPortraits([string]$PortraitDir, [string]$BackupStateDir, [string]$ImportedRoot)
{
    $sourceBackupDir = Get-SourceBackupDir $BackupStateDir
    if (-not (Test-Path -LiteralPath $sourceBackupDir))
    {
        Write-Host "No backup state found. Nothing to restore."
        return
    }

    foreach ($backupFile in Get-ChildItem -LiteralPath $sourceBackupDir -File)
    {
        Copy-Item -LiteralPath $backupFile.FullName -Destination (Join-Path $PortraitDir $backupFile.Name) -Force
    }

    Restore-ImportedCardPortraitArtifacts -ImportedRoot $ImportedRoot -ImportedBackupDir (Get-ImportedBackupDir $BackupStateDir)
    Remove-Item -LiteralPath $BackupStateDir -Recurse -Force

    $restoredCount = (Get-ChildItem -LiteralPath $PortraitDir -Filter "*.png" -File).Count
    Write-Host ("Restored {0} card portraits after export." -f $restoredCount)
}

switch ($Mode)
{
    "prepare" { Prepare-CardPortraits -PortraitDir $SourceDir -BackupStateDir $StateDir -ImportedRoot $ImportedDir -ResizeScale $Scale }
    "restore" { Restore-CardPortraits -PortraitDir $SourceDir -BackupStateDir $StateDir -ImportedRoot $ImportedDir }
}
