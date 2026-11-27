# WsMeta — 给创意工坊物品设置标签 / 封面预览图

steamcmd 不支持 Workshop 的 **tags**，网页也不给改**封面预览图**，所以写了这个最小工具，直接调 Steamworks SDK：

- `ISteamUGC::SetItemPreview` → 上传封面
- `ISteamUGC::SetItemTags` → 设置标签
- `ISteamUGC::CreateItem` / `SetItemContent` / `SubmitItemUpdate` → 可选地新建/更新内容
- `ISteamRemoteStorage::GetQuota` / `FileDelete` → 诊断/清理 Steam Cloud 配额

## 运行前提

- Steam 客户端正在运行并登录上传账号
- 同目录有 `steam_api64.dll`（从 Steamworks SDK 或本工具发行包获取）
- 同目录有 `steam_appid.txt`，内容 `4348910`
- 默认内容目录：`E:\01TestProject\ProbablyStolen-PipetteVceCompat\repo\mod`

## 常用命令

```powershell
# 只补标签 + 封面（默认：物品 3846674523）
.\WsMeta.exe                                   # 同时设置内容/标题/描述/标签/封面、公开
.\WsMeta.exe --no-preview                      # 不碰封面（quota 不够时可以先设标签）
.\WsMeta.exe item=3846692642                   # 指定物品 id
.\WsMeta.exe preview=preview.jpg tags="Mod,Gameplay" # 自定义封面/标签

# 诊断与清理
.\WsMeta.exe --cloud-info                      # 查 Steam Cloud 配额 + 云端文件
.\WsMeta.exe delete-cloud="Questing Goose Studio/Probably Stolen/save_11_day1_xxx.es3"
.\WsMeta.exe --create                          # 新建物品（本工具会自动取回新 id 需要看 Steam 日志）
.\WsMeta.exe delete-item=3846687425            # 删除物品
```

## 重要：Steam Cloud 配额

Workshop 的**封面预览图存在该游戏的 Steam Cloud 配额里**（legacy cloud 路径）。
如果该游戏的云配额已满（`--cloud-info` 显示 `available = 0`），封面上传会失败：

```
Failed to upload workshop preview file ... (Limit exceeded)
src\clientdll\clientugc.cpp (2088) : k_EPublishedFileStorageSystemLegacyCloud == eStorage
```

处理：腾出几 MB 云端空间（游戏内删旧存档，或用 `delete-cloud=` 删云端副本——本地存档不受影响），
再运行本工具即可。steamcmd 的 `workshop_build_item` 在同样情况下也会失败，这不是工具/格式问题。

## 源码

C# / .NET 10，P/Invoke 直连 `steam_api64.dll`（`SteamAPI_InitSafe`，不走 AppData、不写日志到用户目录）。
编译：`dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true`。
