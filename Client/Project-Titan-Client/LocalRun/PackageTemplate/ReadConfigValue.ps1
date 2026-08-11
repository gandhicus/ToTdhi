param(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [Parameter(Mandatory = $true)]
    [string]$Key,

    [string]$Default = "",

    [switch]$AllowRaw
)

if (Test-Path -LiteralPath $Path) {
    foreach ($rawLine in Get-Content -LiteralPath $Path) {
        $line = $rawLine.Trim()
        if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith("#")) {
            continue
        }

        if ($line -match '^\s*([^:=#]+?)\s*[:=]\s*(.*?)\s*$') {
            if ($matches[1].Trim().Equals($Key, [System.StringComparison]::OrdinalIgnoreCase)) {
                Write-Output $matches[2].Trim()
                exit 0
            }
        }
        elseif ($AllowRaw) {
            Write-Output $line
            exit 0
        }
    }
}

Write-Output $Default
