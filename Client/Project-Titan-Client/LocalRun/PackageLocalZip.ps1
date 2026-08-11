param(
    [string]$PackageName = "TrialsOfTitanLocal"
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ClientDir = Resolve-Path (Join-Path $ScriptDir "..")
$RootDir = Resolve-Path (Join-Path $ClientDir "..\..")

$ClientBuildDir = Join-Path $ClientDir "Builds\Windows"
$ClientExe = Join-Path $ClientBuildDir "TrialsOfTitan.exe"
$ServerBuildDir = Join-Path $ClientDir "Builds\LocalServer"
$ServerDll = Join-Path $ServerBuildDir "Run.Local.All.dll"
$ServerExe = Join-Path $ServerBuildDir "Run.Local.All.exe"
$DatabaseDir = Join-Path $RootDir "Database\DynamoDb"
$TemplateDir = Join-Path $ScriptDir "PackageTemplate"
$PackageBaseDir = Join-Path $ClientDir "Builds\Package"
$PackageDir = Join-Path $PackageBaseDir $PackageName
$ZipPath = Join-Path $ClientDir "Builds\$PackageName.zip"

if (!(Test-Path $ClientExe)) {
    throw "Client build was not found: $ClientExe. Run LocalRun\BuildClient.bat first."
}

if (!(Test-Path $ServerDll) -and !(Test-Path $ServerExe)) {
    $serverProject = Join-Path $RootDir "Server\Project-Titan\Run.Local.All\Run.Local.All.csproj"
    Write-Host "Server publish was not found. Publishing Debug build..."
    & dotnet publish $serverProject -c Debug -m:1 -nr:false -p:UseSharedCompilation=false -p:DebugType=None -p:DebugSymbols=false -o $ServerBuildDir
    if ($LASTEXITCODE -ne 0) {
        throw "Server publish failed. Run LocalRun\BuildServer.bat, then retry packaging."
    }
}

if (!(Test-Path $DatabaseDir)) {
    throw "DynamoDB Local folder was not found: $DatabaseDir"
}

if (Test-Path $PackageDir) {
    Remove-Item -LiteralPath $PackageDir -Recurse -Force
}

New-Item -ItemType Directory -Force $PackageDir | Out-Null
New-Item -ItemType Directory -Force (Join-Path $PackageDir "Client") | Out-Null
New-Item -ItemType Directory -Force (Join-Path $PackageDir "Server") | Out-Null
New-Item -ItemType Directory -Force (Join-Path $PackageDir "Database\DynamoDb") | Out-Null

Copy-Item -Path (Join-Path $ClientBuildDir "*") -Destination (Join-Path $PackageDir "Client") -Recurse -Force
Copy-Item -Path (Join-Path $ServerBuildDir "*") -Destination (Join-Path $PackageDir "Server") -Recurse -Force

$packageDatabaseDir = Join-Path $PackageDir "Database\DynamoDb"
Get-ChildItem -LiteralPath $DatabaseDir -Recurse -Force | ForEach-Object {
    if (!$_.PSIsContainer -and ($_.Extension -eq ".db" -or $_.Extension -eq ".db-shm" -or $_.Extension -eq ".db-wal" -or $_.Extension -eq ".db-journal")) {
        return
    }

    $relativePath = $_.FullName.Substring($DatabaseDir.Length).TrimStart('\', '/')
    $targetPath = Join-Path $packageDatabaseDir $relativePath

    if ($_.PSIsContainer) {
        New-Item -ItemType Directory -Force $targetPath | Out-Null
    }
    else {
        $targetDir = Split-Path -Parent $targetPath
        New-Item -ItemType Directory -Force $targetDir | Out-Null
        Copy-Item -LiteralPath $_.FullName -Destination $targetPath -Force
    }
}

Copy-Item -Path (Join-Path $TemplateDir "*") -Destination $PackageDir -Recurse -Force

if (Test-Path $ZipPath) {
    Remove-Item -LiteralPath $ZipPath -Force
}

Compress-Archive -Path $PackageDir -DestinationPath $ZipPath -CompressionLevel Optimal

Write-Host ""
Write-Host "Package created:"
Write-Host $ZipPath
Write-Host ""
Write-Host "Players can extract it and run StartAllLocal.bat."
