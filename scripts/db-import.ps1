<#
.SYNOPSIS
    Import database FlowerShop vào MySQL của Laragon — AN TOÀN UTF-8 (không làm mất dấu tiếng Việt).

.DESCRIPTION
    Vì sao phải dùng Start-Process -RedirectStandardInput thay vì `Get-Content | mysql.exe`:
      PowerShell 5.1 chuyển chuỗi sang native exe qua code page console (thường là 437/OEM),
      ký tự có dấu không biểu diễn được -> bị thay bằng "?" và MẤT VĨNH VIỄN trong DB.
      Redirect file vào stdin sẽ đẩy nguyên byte UTF-8, không qua chuyển mã.

.EXAMPLE
    .\scripts\db-import.ps1
    .\scripts\db-import.ps1 -Password "matkhau" -MySqlExe "C:\laragon\bin\mysql\mysql-8.4.3-winx64\bin\mysql.exe"
#>
[CmdletBinding()]
param(
    [string]$MySqlExe,
    [string]$User = 'root',
    [string]$Password = '',
    [string]$Database = 'FlowerShopDb',
    [switch]$SkipVerify
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$dump = Join-Path $repoRoot 'FlowerShop\App_Data\FlowerShop_Database_Dump.sql'

if (-not (Test-Path $dump)) { throw "Không thấy file dump: $dump" }

function Find-MySql {
    if ($MySqlExe -and (Test-Path $MySqlExe)) { return $MySqlExe }

    foreach ($root in @('C:\laragon\bin\mysql', 'C:\xampp\mysql', 'C:\wamp64\bin\mysql')) {
        if (Test-Path $root) {
            $found = Get-ChildItem $root -Recurse -Filter mysql.exe -ErrorAction SilentlyContinue |
                     Sort-Object FullName -Descending | Select-Object -First 1
            if ($found) { return $found.FullName }
        }
    }

    $cmd = Get-Command mysql.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    throw "Không tìm thấy mysql.exe. Truyền đường dẫn: -MySqlExe 'C:\laragon\bin\mysql\...\bin\mysql.exe'"
}

function Invoke-MySqlFile {
    param([string]$Client, [string]$FilePath, [string[]]$BaseArgs, [string]$PasswordValue)

    $args = @($BaseArgs) + @("--default-character-set=utf8mb4")
    if ($PasswordValue) { $args += "-p$PasswordValue" }

    $p = Start-Process -FilePath $Client -ArgumentList $args -RedirectStandardInput $FilePath `
                       -RedirectStandardError "$env:TEMP\fs-import-err.txt" -NoNewWindow -PassThru -Wait
    $err = Get-Content "$env:TEMP\fs-import-err.txt" -Raw -ErrorAction SilentlyContinue
    Remove-Item "$env:TEMP\fs-import-err.txt" -Force -ErrorAction SilentlyContinue

    if ($p.ExitCode -ne 0) {
        throw "mysql.exe trả về exit code $($p.ExitCode). Chi tiết: $err"
    }

    # mysql 8 có thể trả exit code 0 kèm cảnh báo -> vẫn in ra để người dùng thấy
    if ($err -and $err.Trim()) { Write-Host "   (stderr) $($err.Trim())" -ForegroundColor DarkYellow }
}

function Invoke-MySqlQuery {
    param([string]$Client, [string[]]$BaseArgs, [string]$PasswordValue, [string]$Sql)

    $args = @($BaseArgs) + @("--default-character-set=utf8mb4", '-e', $Sql)
    if ($PasswordValue) { $args += "-p$PasswordValue" }

    & $Client @args 2>&1
}

$mysql = Find-MySql
Write-Host "==> MySQL client : $mysql" -ForegroundColor Cyan
Write-Host "==> File dump    : $dump" -ForegroundColor Cyan

# --- Bỏ UTF-8 BOM (nếu có) để câu lệnh SQL đầu tiên không bị dính ký tự ﻿ ---
$bytes = [System.IO.File]::ReadAllBytes($dump)
$hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF

$importFile = $dump
if ($hasBom) {
    $importFile = Join-Path $env:TEMP ("flowershop-dump-" + [guid]::NewGuid().ToString('N') + ".sql")
    [System.IO.File]::WriteAllBytes($importFile, $bytes[3..($bytes.Length - 1)])
    Write-Host "==> Đã bỏ UTF-8 BOM khi import (bản tạm: $importFile)" -ForegroundColor DarkGray
}

$baseArgs = @('-h', 'localhost', '-u', $User)

Write-Host "==> Import (raw bytes -> stdin, không qua console code page)..." -ForegroundColor Cyan
try {
    Invoke-MySqlFile -Client $mysql -FilePath $importFile -BaseArgs $baseArgs -PasswordValue $Password
}
finally {
    if ($importFile -ne $dump -and (Test-Path $importFile)) { Remove-Item $importFile -Force }
}

Write-Host "==> Import xong." -ForegroundColor Green

if ($SkipVerify) { return }

Write-Host "`n==> Kiểm tra số dòng:" -ForegroundColor Cyan
Invoke-MySqlQuery -Client $mysql -BaseArgs $baseArgs -PasswordValue $Password `
    -Sql "SELECT 'Products' t, COUNT(*) n FROM $Database.Products UNION ALL SELECT 'Orders', COUNT(*) FROM $Database.Orders UNION ALL SELECT 'Users', COUNT(*) FROM $Database.Users UNION ALL SELECT 'Reviews', COUNT(*) FROM $Database.Reviews;"

# --- Kiểm tra UTF-8 round-trip: nếu có "?" nghĩa là dữ liệu đã bị cắt dấu ---
Write-Host "`n==> Kiểm tra tiếng Việt lưu trong DB:" -ForegroundColor Cyan
# -N: bỏ dòng tên cột, để giá trị lấy ra sạch
$name = (Invoke-MySqlQuery -Client $mysql -BaseArgs ($baseArgs + '-N') -PasswordValue $Password `
        -Sql "SELECT Name FROM $Database.Products WHERE Id = 1;") -join "`n"
$name = $name.Trim()

Write-Host "   Products.Id=1 -> $name"

if ($name -match '\?') {
    Write-Host ""
    Write-Host "!! DỮ LIỆU ĐANG BỊ MẤT DẤU (có ký tự '?')." -ForegroundColor Red
    Write-Host "   Nguyên nhân thường gặp: import bằng công cụ/terminal dùng charset không phải utf8mb4," -ForegroundColor Red
    Write-Host "   hoặc DB cũ đã bị nhiễm từ lần import trước. Hãy chạy lại script này." -ForegroundColor Red
    exit 1
}

# So khớp với giá trị gốc đọc trực tiếp từ file dump (UTF-8) => không phụ thuộc literal tiếng Việt trong script
$sqlText = [System.IO.File]::ReadAllText($dump, [System.Text.Encoding]::UTF8)
$match = [regex]::Match($sqlText, "\(1,1,2,1,'([^']+(?:''[^']*)*)'")
$expected = if ($match.Success) { $match.Groups[1].Value.Replace("''", "'") } else { '' }

if ($expected -and ($name.Trim() -ne $expected.Trim())) {
    Write-Host "!! Tên sản phẩm trong DB không khớp file dump." -ForegroundColor Yellow
    Write-Host "   DB    : $($name.Trim())" -ForegroundColor Yellow
    Write-Host "   Expect: $expected" -ForegroundColor Yellow
    Write-Host "   => Charset khi kết nối/import không phải utf8mb4." -ForegroundColor Yellow
    exit 1
}

Write-Host "   OK - tiếng Việt lưu đúng UTF-8." -ForegroundColor Green
