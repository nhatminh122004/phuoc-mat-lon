<#
.SYNOPSIS
    Build + chạy FlowerShop (ASP.NET MVC 5) bằng IIS Express từ command line.

.EXAMPLE
    .\scripts\run.ps1
    .\scripts\run.ps1 -Port 8080
    .\scripts\run.ps1 -Port 8080 -NoBuild
    .\scripts\run.ps1 -Port 8080 -OpenBrowser
#>
[CmdletBinding()]
param(
    [int]$Port = 44300,
    [switch]$NoBuild,
    [switch]$OpenBrowser,
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$webRoot = Join-Path $repoRoot 'FlowerShop'
$solution = Join-Path $repoRoot 'FlowerShop.sln'

if (-not (Test-Path $webRoot)) { throw "Không tìm thấy thư mục web: $webRoot" }

function Find-IisExpress {
    $candidates = @(
        "$env:ProgramFiles\IIS Express\iisexpress.exe",
        "${env:ProgramFiles(x86)}\IIS Express\iisexpress.exe"
    )
    foreach ($c in $candidates) { if (Test-Path $c) { return $c } }
    throw "Không tìm thấy iisexpress.exe. Cài 'IIS Express' trong Visual Studio Installer (workload ASP.NET and web development)."
}

function Find-MSBuild {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $path = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' |
                Select-Object -First 1
        if ($path) { return $path }
    }
    foreach ($c in @(
        "$env:ProgramFiles\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "$env:ProgramFiles\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "$env:ProgramFiles\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    )) { if (Test-Path $c) { return $c } }
    throw "Không tìm thấy MSBuild.exe (cần Visual Studio 2022)."
}

if (-not $NoBuild) {
    $msbuild = Find-MSBuild
    if (-not $Quiet) { Write-Host "==> Restore + Build..." -ForegroundColor Cyan }

    & $msbuild $solution /t:restore /p:RestorePackagesConfig=true /v:q /nologo | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Restore thất bại (exit $LASTEXITCODE)." }

    & $msbuild $solution /p:Configuration=Debug /v:q /nologo | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Build thất bại (exit $LASTEXITCODE)." }
}

$iisexpress = Find-IisExpress
$url = "http://localhost:$Port/"

if (-not $Quiet) {
    Write-Host ""
    Write-Host "  Hữu Phước Florist đang chạy:" -ForegroundColor Green
    Write-Host "    Storefront : $url" -ForegroundColor Yellow
    Write-Host "    Admin      : ${url}Admin/Dashboard  (admin / Admin@123)" -ForegroundColor Yellow
    Write-Host "    Health     : ${url}Account/Login" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Nhấn Ctrl+C để dừng." -ForegroundColor DarkGray
    Write-Host ""
}

if ($OpenBrowser) { Start-Process $url | Out-Null }

# /systray:false để không chiếm khay hệ thống khi chạy trong terminal
& $iisexpress "/path:$webRoot" "/port:$Port" /systray:false
