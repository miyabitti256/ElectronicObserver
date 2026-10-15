<#
.SYNOPSIS
    Release ビルドの出力から配布用 zip と checksums.txt を作成します。

.DESCRIPTION
    bin/<Configuration> 以下のうち配布に必要なファイルだけをステージングして zip にまとめます。

    - トップレベル: 許可した拡張子 (.exe / .dll / .config / .zip / .txt) のみ
    - x86 / x64   : ネイティブ依存 (CefSharp / CEF) を丸ごと (.pdb / .xml は除外)

    ユーザーデータ (Settings / Record / KCAPI / BrowserCache) や実行時生成物
    (rootCert.pfx, *.log) は含めません。配布する拡張子の方針は copy_to_realenv.ps1 と
    揃えていますが、配布物には .pdb を含めません。

.PARAMETER BinDir
    ビルド出力ディレクトリ (例: ElectronicObserver\bin\Release)。

.PARAMETER OutputDir
    zip と checksums.txt の出力先。配下に package\ (ステージング) を作成します。

.PARAMETER Version
    配布物のバージョン文字列 (例: 4.7.1-20261016)。zip のファイル名に使われます。

.EXAMPLE
    .\scripts\package-release.ps1 -BinDir ElectronicObserver\bin\Release -OutputDir dist -Version 4.7.1-20261016
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BinDir,

    [Parameter(Mandatory = $true)]
    [string]$OutputDir,

    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# トップレベルで配布する拡張子
$allowedExtensions = @('.exe', '.dll', '.config', '.zip', '.txt')

# ネイティブ依存を丸ごと含めるサブディレクトリ
$nativeDirs = @('x86', 'x64')

# 配布しない拡張子 (デバッグ情報 / XML ドキュメント)
$excludedExtensions = @('.pdb', '.xml')

if ([string]::IsNullOrWhiteSpace($Version)) {
    throw 'Version が指定されていません。'
}
if (-not (Test-Path -LiteralPath $BinDir -PathType Container)) {
    throw "ビルド出力ディレクトリが見つかりません: $BinDir"
}

$BinDir = (Resolve-Path -LiteralPath $BinDir).Path
$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)
$stageDir = Join-Path $OutputDir 'package'

if (Test-Path -LiteralPath $stageDir) {
    Remove-Item -LiteralPath $stageDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stageDir -Force | Out-Null

# トップレベルの配布対象ファイル
Get-ChildItem -LiteralPath $BinDir -File |
    Where-Object { $allowedExtensions -contains $_.Extension.ToLowerInvariant() } |
    ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $stageDir $_.Name) -Force
    }

# ネイティブ依存 (CefSharp / CEF) をディレクトリ構造を保ってコピー
foreach ($dir in $nativeDirs) {
    $sourceDir = Join-Path $BinDir $dir
    if (-not (Test-Path -LiteralPath $sourceDir -PathType Container)) {
        Write-Warning "ネイティブ依存ディレクトリが見つかりません (スキップ): $sourceDir"
        continue
    }

    Get-ChildItem -LiteralPath $sourceDir -Recurse -File |
        Where-Object { $excludedExtensions -notcontains $_.Extension.ToLowerInvariant() } |
        ForEach-Object {
            $relative = $_.FullName.Substring($sourceDir.Length).TrimStart('\', '/')
            $destination = Join-Path $stageDir (Join-Path $dir $relative)
            New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
        }
}

# 取りこぼし防止: 配布物に必須のファイルが存在するか確認
foreach ($required in @('ElectronicObserver.exe', 'version.txt', 'Assets.zip')) {
    if (-not (Test-Path -LiteralPath (Join-Path $stageDir $required))) {
        throw "配布物に必須ファイルが含まれていません: $required (BinDir=$BinDir)"
    }
}

$zipName = "ElectronicObserver-$Version.zip"
$zipPath = Join-Path $OutputDir $zipName
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    $stageDir,
    $zipPath,
    [System.IO.Compression.CompressionLevel]::Optimal,
    $false)

$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$checksumPath = Join-Path $OutputDir 'checksums.txt'
# sha256sum 形式 (ハッシュ 2 スペース ファイル名)
"$hash  $zipName" | Set-Content -LiteralPath $checksumPath -Encoding ascii

$zipSizeMB = [math]::Round((Get-Item -LiteralPath $zipPath).Length / 1MB, 1)
Write-Host "zip       : $zipPath ($zipSizeMB MB)"
Write-Host "checksums : $checksumPath"
Write-Host "sha256    : $hash"
