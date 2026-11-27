# WsMeta — 给创意工坊物品设置标签 / 封面预览图

steamcmd 不能设置 Workshop 的 **tags** 和 **cover(预览图)**（网页也改不了），
所以写了这个最小工具，直接调用 Steamworks SDK：

- `ISteamUGC::SetItemPreview` → 上传封面
- `ISteamUGC::SetItemTags` → 设置标签
- `ISteamUGC::SetItemVisibility` → 保持公开
- `ISteamUGC::SubmitItemUpdate` → 提交（不重传内容）

## 运行前提

- **Steam 客户端正在运行，并登录着上传该物品的账号**
- 本目录下的 `steam_appid.txt` 内容为 `4348910`（不要改）
- 本目录下的 `steam_api64.dll` 不要删

## 运行

双击 `run-wsmeta.bat`，或在 PowerShell 里：

```powershell
cd 'E:\knowledge base\tools\wsmeta\dist'
.\WsMeta.exe
```

默认目标：物品 `3846674523`，预览图本目录的 `preview.png`，
标签 `Mod, Gameplay, Simplified Chinese`。
输出同时写入本目录的 `WsMeta.log`。

也可以自定义：

```powershell
.\WsMeta.exe "D:\path\preview.png" "Mod,Gameplay,Simplified Chinese" "v2.0.1 add tags and preview"
```

## 参数说明

| 参数 | 默认值 |
|---|---|
| 1. 预览图路径 | 本目录 `preview.png` |
| 2. 标签（逗号或分号分隔） | `Mod, Gameplay, Simplified Chinese` |
| 3. 更新说明（changenote） | `v2.0.1 add tags and preview` |

## 预期输出

```
connected, appid = 4348910
update handle = ...
SetItemPreview = True ; SetItemTags = True ; SetItemVisibility(public) = True
SubmitItemUpdate call = ...
  status=1 ... status=4 ... status=5 ...
result: callbackID=... EResult=1 needsLegalAgreement=False item=3846674523
Done. Check the workshop page ...
```

- `EResult=1` = OK
- `needsLegalAgreement=True`：在浏览器打开创意工坊物品页，接受一次 Workshop 法律协议后再跑一遍
- `SteamAPI_Init failed`：Steam 客户端没开/没登录，或 steam_appid.txt 不在 exe 旁边

## 源码

本工具源码在 GitHub 仓库的 `tools/wsmeta/`（C# / .NET 10，P/Invoke 直连 `steam_api64.dll`，不写 AppData）。
