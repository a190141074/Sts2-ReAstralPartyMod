param(
    [Parameter(Mandatory = $true)]
    [string]$Pattern,

    [string[]]$Roots
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = "B:\Documents\re-astral-party-mod"
$modRoot = "D:\MOD"

if (-not $Roots -or $Roots.Count -eq 0) {
    $discoveredRoots = New-Object System.Collections.Generic.List[string]
    if (Test-Path -LiteralPath $modRoot) {
        $targets = @(
            "RitsuLib-doc",
            "RitsuLib-code",
            "STS2_WineFox-main",
            "Slay-the-Spire-2-gdsdecomp"
        )

        foreach ($target in $targets) {
            $match = Get-ChildItem -LiteralPath $modRoot -Directory -Recurse -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -eq $target } |
                Select-Object -First 1 -ExpandProperty FullName
            if ($match) {
                $discoveredRoots.Add($match)
            }
        }
    }

    if (Test-Path -LiteralPath $repoRoot) {
        $discoveredRoots.Add($repoRoot)
    }

    $Roots = $discoveredRoots.ToArray()
}

$allowedExtensions = @(".cs", ".md", ".json", ".gd", ".tscn", ".csproj")
$skipPathPatterns = @(
    "\\\.git\\",
    "\\\.godot\\",
    "\\bin\\",
    "\\obj\\",
    "\\logs\\"
)
$allMatches = New-Object System.Collections.Generic.List[object]

foreach ($root in $Roots) {
    if (-not (Test-Path -LiteralPath $root)) {
        continue
    }

    try {
        $files = Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {
            $skip = $false
            foreach ($patternItem in $skipPathPatterns) {
                if ($_.FullName -match $patternItem) {
                    $skip = $true
                    break
                }
            }

            ($allowedExtensions -contains $_.Extension) -and -not $skip
        }
        $matches = $files | Select-String -Pattern $Pattern -SimpleMatch
        foreach ($match in $matches) {
            $allMatches.Add([PSCustomObject]@{
                Root = $root
                Path = $match.Path
                Line = $match.LineNumber
                Text = $match.Line.Trim()
            })
        }
    }
    catch {
        Write-Warning ("Search failed under {0}: {1}" -f $root, $_.Exception.Message)
    }
}

if ($allMatches.Count -eq 0) {
    Write-Output ("No matches found for '{0}'." -f $Pattern)
    exit 0
}

$grouped = $allMatches | Group-Object Root
foreach ($group in $grouped) {
    Write-Output ("== {0} ==" -f $group.Name)
    foreach ($item in $group.Group | Sort-Object Path, Line | Select-Object -First 20) {
        Write-Output ("{0}:{1}: {2}" -f $item.Path, $item.Line, $item.Text)
    }
    if ($group.Count -gt 20) {
        Write-Output ("... ({0} more matches under this root)" -f ($group.Count - 20))
    }
    Write-Output ""
}

$total = $allMatches.Count
$rootsHit = ($grouped | Measure-Object).Count
Write-Output ("Total matches: {0} across {1} root(s)." -f $total, $rootsHit)
