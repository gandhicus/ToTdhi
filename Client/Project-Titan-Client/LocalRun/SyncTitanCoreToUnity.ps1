param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$clientDir = Resolve-Path (Join-Path $PSScriptRoot "..")
$rootDir = Resolve-Path (Join-Path $clientDir "..\..")
$titanCoreProject = Join-Path $rootDir "Library\TitanCore\TitanCore.csproj"
$outputDir = Join-Path $rootDir "Library\TitanCore\bin\$Configuration\netstandard2.0"
$pluginsDir = Join-Path $clientDir "Assets\Plugins"

if (-not (Test-Path -LiteralPath $titanCoreProject)) {
    Write-Error "TitanCore project not found: $titanCoreProject"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet was not found in PATH. Install the .NET SDK or use Developer PowerShell."
}

if (-not $SkipBuild) {
    Write-Host "Building TitanCore ($Configuration)..."
    dotnet build $titanCoreProject -c $Configuration --nologo -v minimal
    if ($LASTEXITCODE -ne 0) {
        throw "TitanCore build failed with exit code $LASTEXITCODE"
    }
}

$dlls = @("TitanCore.dll", "Utils.NET.dll")
foreach ($dll in $dlls) {
    $source = Join-Path $outputDir $dll
    if (-not (Test-Path -LiteralPath $source)) {
        throw "Expected build output not found: $source"
    }

    $destination = Join-Path $pluginsDir $dll
    Copy-Item -LiteralPath $source -Destination $destination -Force
    $info = Get-Item -LiteralPath $destination
    Write-Host "Copied $($info.Name) -> $pluginsDir ($($info.LastWriteTime))"
}

Write-Host ""
Write-Host "TitanCore sync complete. Return to Unity to recompile scripts."
