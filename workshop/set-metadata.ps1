# 更新创意工坊物品 3846674523 的标签与预览图（不重传内容）
# 前提：Steam 客户端已运行，并登录着当初 steamcmd 上传该物品的账号。
# 用法：右键 → 使用 PowerShell 运行；或在普通 PowerShell 里执行本脚本。

$ErrorActionPreference = 'Stop'
$docs = Split-Path -Parent $MyInvocation.MyCommand.Path
$content = (Resolve-Path (Join-Path (Split-Path -Parent $docs) 'mod')).Path

$toolDir = 'E:\01TestProject\ProbablyStolen-PipetteVceCompat\tools\workshop-uploader\bin'
$exe = Join-Path $toolDir 'workshop.exe'
if (-not (Test-Path $exe)) {
    throw "没找到 $exe —— 请先解压 nozwock/steam-workshop-uploader 0.2.0 的 Windows 版，或改这里的路径。"
}
if (-not (Get-Process steam -ErrorAction SilentlyContinue)) {
    throw 'Steam 客户端没有运行：请先打开 Steam 并登录。'
}

# 保险起见：让 Steamworks 也看到正确的 appid
Copy-Item (Join-Path $docs 'steam_appid.txt') (Join-Path $toolDir 'steam_appid.txt') -Force

$tags = @('Mod', 'Gameplay', 'Simplified Chinese')
$itemId = '3846674523'
$preview = Join-Path $content 'preview.png'

Write-Host "内容目录 : $content"
Write-Host "物品 ID  : $itemId"
Write-Host "标签     : $($tags -join ', ')"
Write-Host ''

# 1) 生成 workshop.toml（工具会自动把它排除在上传内容之外）
$a = @('init', '--content', $content, '--app-id', '4348910', '--item-id', $itemId, '--force')
foreach ($t in $tags) { $a += @('--tag', $t) }
& $exe @a
if ($LASTEXITCODE -ne 0) { throw "init 失败（exit $LASTEXITCODE）" }

# 2) 只更新元数据（标签 + 预览图），不重传内容
$b = @('update', '--content', $content, '--preview', $preview, '--visibility', 'Public', '--no-content-update')
foreach ($t in $tags) { $b += @('--tag', $t) }
& $exe @b
if ($LASTEXITCODE -ne 0) { throw "update 失败（exit $LASTEXITCODE）" }

Write-Host ''
Write-Host '完成。下面用 Steam API 核对（preview_url 非空、tags 有值即成功）：'
$body = @{ itemcount = 1; 'publishedfileids[0]' = [uint64]$itemId }
try {
    $resp = Invoke-RestMethod -Uri 'https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/' -Method Post -Body $body -TimeoutSec 30
    $d = $resp.response.publishedfiledetails[0]
    Write-Host ("  preview_url = " + $d.preview_url)
    Write-Host ("  tags        = " + (($d.tags | ForEach-Object { $_.tag }) -join ', '))
} catch {
    Write-Host ("  查询失败（不影响上传）：" + $_.Exception.Message)
}
Write-Host '刷新创意工坊页面即可看到封面图和标签。'
