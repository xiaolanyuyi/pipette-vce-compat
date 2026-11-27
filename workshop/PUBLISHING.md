# 发布到 Steam 创意工坊（steamcmd）

> 依据官方社区模组指南的 "Publishing with steamcmd" 一节整理：
> https://github.com/dragonpan2/probably-stolen-modding
> **游戏没有上传按钮**，创意工坊物品一律用 Valve 的 `steamcmd` 上传，账号需要拥有游戏。

## 0. 你需要准备

- 一个**拥有《深空当铺·可能是偷的》**的 Steam 账号（第一次上传会要求网页上接受 Steam 创意工坊法律协议）。
- [steamcmd](https://developer.valvesoftware.com/wiki/SteamCMD)（Valve 官方命令行工具）。
- 本仓库的 `mod/\` 文件夹（可以直接当上传内容）。里面已经有：
  - `manifest.xml`（创意工坊物品就是"一个模组文件夹"，manifest 必须在根目录）
  - `preview.png`（512×512，<1 MB；模组菜单/工坊预览用）
  - 其余 DLL / 规则 JSON / 本地化 / README / optional

## 1. 检查上传内容

- `mod/manifest.xml`：`<ID>pipette_vce_compat</ID>`、`<GameVersions><Version>1.0.0</Version>`、`<Prerequisites>` 里有两个前置。
- 不要放进内容文件夹的东西：`.exe/.bat/.ps1/.js/.lnk`、未在 manifest 里声明的 DLL、`dev.txt`、测试用的临时文件。
  （本仓库的 `mod/` 已经清理过，直接用即可。）
- 预览图文件名是 `preview.png`，大小 < 1 MB。

## 2. 编辑物品描述文件

`workshop/pipette_vce_compat.vdf` 已经写好，按需确认这几项：

| 字段 | 说明 |
|---|---|
| `appid` | `4348910`（深空当铺） |
| `publishedfileid` | `0` = 新建；上传后 steamcmd 会把新 id 写回这个文件 |
| `contentfolder` | 模组文件夹（默认指向本仓库的 `mod`） |
| `previewfile` | `mod/preview.png` |
| `visibility` | `0` 公开 / `1` 仅好友 / `2` 隐藏。**注意：每次更新都会按 VDF 里的值重设可见性**，所以首次测试临时用 `2`、测试完必须改回 `0`，否则下一次更新会把物品重新隐藏 |
| `title` / `description` / `changenote` | 标题 / 说明 / 本次更新说明 |

> 说明文字里的 Steam BBCode 直接贴到创意工坊网页的"描述"框里最省事（见第 4 步）；
> VDF 里的 `description` 只放一句占位即可，避免多行/引号转义出错。

## 3. 上传

```powershell
steamcmd +login <你的Steam账号> +workshop_build_item "E:\knowledge base\pipette-vce-compat\workshop\pipette_vce_compat.vdf" +quit
```

- 会提示密码和 Steam Guard；登录成功后上传，steamcmd 会打印新物品的 **item id**，并把它写回 VDF 的 `publishedfileid`。
- 第一次上传若报错提示法律协议，按提示在浏览器里同意一次后重试。
- 上传完成后的日志形如：`Success. Published file id: 1234567890`。

## 3.5 故障排查：`Uploading preview image ... Limit exceeded`

现象（2026-11 实测）：内容上传正常，卡在预览图这一步：

```
Preparing content...
Uploading content...
Uploading preview image...src\clientdll\clientugc.cpp (2088) : k_EPublishedFileStorageSystemLegacyCloud == eStorage
ERROR! Failed to update workshop item (Limit exceeded).
```

`Limit exceeded` 是 Steam 的 `EResultLimitExceeded`：出现在预览图上传阶段通常是预览图 >1 MB，
或该账号/该 App 的 Steam Cloud 空间不足；而这里的 assert 说明 steamcmd 的“预览图上传”走的是旧版云存储路径，
与这个 App 的 UGC 存储系统不匹配 —— **用 steamcmd 给这个游戏传预览图基本走不通**。

**绕开办法（推荐）**：先不带预览图上传内容，再在物品页面补预览图。

1. 用 `workshop/pipette_vce_compat.nopreview.vdf`（已去掉 `previewfile` 行，`publishedfileid` 保留上一步创建出来的 id）：

   ```
   workshop_build_item "E:\knowledge base\pipette-vce-compat\workshop\pipette_vce_compat.nopreview.vdf"
   ```

2. 看到 `Success. Published file id: ...` 后，打开 Steam 客户端 → 社区 → 创意工坊 → 我的创意工坊物品
   （或浏览器 https://steamcommunity.com/my/workshopfiles/?appid=4348910 ），编辑该物品：
   - 上传**预览图**（`mod/preview.png`，112 KB）
   - 粘贴描述（`description.bbcode`）、勾选标签、把可见性改为公开
3. 如果连“无预览图”上传也报 `Limit exceeded`：把 VDF 的 `publishedfileid` 改回 `0` 重新创建新物品，
   或把 `visibility` 改成 `0` 再试；仍失败则检查 Steam 客户端「设置 → 云」的云存储，并等几小时再试（新账号有速率限制）。
4. 如果页面编辑里**也**传不上预览图：把 `preview.png` 另存为 JPG（同样 512×512、<1 MB）再试；Steam 预览 PNG/JPG 都接受。

### 实际日志结论（2026-11-27 实测，appid 4348910）

```
D:\SteamCMD\logs\workshop_log.txt:
[AppID 4348910] Create new workshop item ... : 3846674523 (OK)
[AppID 4348910] Uploaded new content ( ManifestID 781683043439129910 ) for item 3846674523.
[AppID 4348910] Failed to upload workshop preview file ...preview.png (Limit exceeded)

D:\SteamCMD\logs\cloud_log.txt:
[AppID 4348910] UGC Upload Limit exceeded for file previewfile_3846674523.png

D:\SteamCMD\logs\stderr.txt:
src\clientdll\clientugc.cpp (2088) : k_EPublishedFileStorageSystemLegacyCloud == eStorage
```

**结论**：内容（depot manifest）上传是成功的，只有 `previewfile` 失败。原因不是图片太大（112 KB < 1 MB），
而是 steamcmd 的预览图上传走 Steam 旧版云存储（legacy cloud）路径，而 4348910 这个 App 的创意工坊用的是
新版 UGC 云存储（assert 期望 legacy 但实际不是），于是被判定为 `UGC Upload Limit exceeded`。
**steamcmd 对这个游戏的预览图上传走不通**，但内容可以正常上传。

**正确做法**：
1. 用 `pipette_vce_compat.nopreview.vdf` 补一次“无预览图更新”，让物品更新完整落地（应打印 `Success.`）。
2. 预览图改用 **Steam 客户端/网页的物品编辑器**上传：
   - Steam 客户端 → 社区 → 创意工坊 → 我的创意工坊物品（或浏览器 https://steamcommunity.com/my/workshopfiles/?appid=4348910）
   - 找到该物品 → 编辑 → “预览图像” → 上传 `mod/preview.png`（512×512，112 KB）
   这条网页/客户端路径走新版 UGC 存储，不会触发上面的 legacy cloud 限制。
3. 如果网页编辑器也报 limit：先试 JPG（仓库里有 `workshop/preview.jpg`，54 KB；对应的 `pipette_vce_compat.jpgpreview.vdf` 是给 steamcmd 再试的版本），
   或等 24 小时（新账号/UGC 上传有速率限制），或换用基于 Steamworks SDK 的上传工具（如 SteamWorkshopUploader）。

## 3.6 设置标签与预览图（steamcmd 做不到）

Steam 网页只允许改**标题 / 描述 / 截图**；**标签（tags）和封面预览图**只能由上传工具通过 Steamworks SDK 的
`SetItemTags` / `SetItemPreview` 设置：

- steamcmd 的 VDF **不支持 tags**（Steam 官方文档/社区一致结论：tags 只能经 `ISteamUGC::SetItemTags`）；
- 预览图在 4348910 这个 App 上走 steamcmd 会命中 legacy cloud 断言并报 `UGC Upload Limit exceeded`（见 §3.5）。

所以物品已创建（内容上传成功）之后，用下面任一个 **Steamworks SDK 工具**补标签和预览图。
两者都要求 **Steam 客户端正在运行、并登录着上传该物品的账号**（不需要再输密码，走本机 Steam 会话）。

### 方案 A（推荐，已下载到本机）：nozwock/steam-workshop-uploader 0.2.0

一键脚本：`workshop/set-metadata.ps1`（在普通 PowerShell 里运行；会自动 init + update 并核对结果）。

路径：`E:\knowledge base\tools\workshop-uploader\bin\workshop.exe`（旁边已有 `steam_api64.dll`）。
先把 `workshop/steam_appid.txt` 复制到该 bin 目录（内容 `4348910`，保险起见）。

```powershell
cd 'E:\knowledge base\tools\workshop-uploader\bin'

# 1) 在内容目录生成 workshop.toml（工具会自动把它排除在上传内容之外）
.\workshop.exe init --content 'E:\knowledge base\pipette-vce-compat\mod' --app-id 4348910 --item-id 3846674523 `
  --tag Mod --tag Gameplay --tag 'Simplified Chinese'

# 2) 只更新元数据（标签 + 预览图），不重传内容
.\workshop.exe update --content 'E:\knowledge base\pipette-vce-compat\mod' `
  --preview 'E:\knowledge base\pipette-vce-compat\mod\preview.png' `
  --tag Mod --tag Gameplay --tag 'Simplified Chinese' `
  --visibility Public --no-content-update
```

- 如果标签校验报错，它会打印该 App 的合法标签列表；从现有物品可见的合法值包括
  `Mod`、`Gameplay`、`tools`、`english`、`Simplified Chinese`、`Quality of Life`、`UI`、`Utilities`。
- `--no-content-update` 时不会上传文件；`workshop.toml` 始终被自动排除。


> **若报 `failed to create file ...\io.github.nozwock.steam-workshop-uploader\log\workshop.log...: 拒绝访问`**：
> 该工具会在 `%LOCALAPPDATA%\io.github.nozwock.steam-workshop-uploader\log` 写日志；如果这个目录被别的程序
> （尤其是带限制 ACL 的沙箱/杀软）创建过，工具就建不了日志文件。处理办法：
>
> ```powershell
> Remove-Item -Recurse -Force "$env:LOCALAPPDATA\io.github.nozwock.steam-workshop-uploader" -ErrorAction SilentlyContinue
> New-Item -ItemType Directory -Force "$env:LOCALAPPDATA\io.github.nozwock.steam-workshop-uploader\log" | Out-Null
> ```
>
> 然后再跑工具；还不行就用管理员 PowerShell，或改用方案 B 的 GUI 工具。
### 方案 B（GUI 备选）：nihilocrat/SteamWorkshopUploader Build 10

下载：https://github.com/nihilocrat/SteamWorkshopUploader/releases/download/Build10/SteamWorkshopUploader_10_win.zip

1. 解压后，用本仓库的 `workshop/steam_appid.txt` 覆盖程序目录里的同名文件（原内容是 480）。
2. 用 `workshop/SteamWorkshopUploader.config.json` 覆盖程序目录的 `config.json`（内含本 App 合法标签校验）。
3. 启动 Steam 客户端并登录 → 运行程序 → 在列表里新建一个条目。
4. 关闭程序，编辑生成的 `WorkshopContent\<条目>.workshop.json`，按 `workshop/workshop-item.template.json`
   填入 `publishedfileid` = `3846674523`（更新已有物品而不是新建），保存。
5. 重新打开程序 → 选中条目 → Upload / Update。

### 方案 C（本仓库自带小工具）：WsMeta

steamcmd 不支持 tags、网页也不给改封面/标签时，用仓库里的 `tools/wsmeta`（C# 直连 Steamworks SDK，
不写 AppData、不需要 .NET 运行时外的东西）。已编译好的版本在
`E:\knowledge base\tools\wsmeta\dist\`：

- `WsMeta.exe`（自包含单文件）
- `steam_api64.dll`、`steam_appid.txt`（内容 4348910）、`preview.png`
- `run-wsmeta.bat`（双击运行）、`WsMeta.log`（运行日志）

运行前提：Steam 客户端正在运行并登录着上传账号。然后：

```powershell
cd 'E:\knowledge base\tools\wsmeta\dist'
.\WsMeta.exe
```

默认给物品 `3846674523` 设置封面 `preview.png`、标签 `Mod, Gameplay, Simplified Chinese`，
并用 `SubmitItemUpdate` 提交（不重传内容）。输出 `EResult=1` 即成功；日志见 `WsMeta.log`。
源码见仓库 `tools/wsmeta/`。
### 验证

完成后刷新物品页：右侧方形预览图和标签应出现；也可以用 Steam API 核对：

```powershell
Invoke-RestMethod -Uri 'https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/' -Method Post `
  -Body @{ itemcount = 1; 'publishedfileids[0]' = 3846674523 } | % { $_.response.publishedfiledetails[0] | Select preview_url,tags }
```

`preview_url` 非空、`tags` 有值就说明成功。

### 如果两个工具都失败

去问一下 Pen / gdm56 是怎么给这个 App 的模组设置预览图和标签的（他们的物品都有封面），
或者用他们的上传配置作参考。理论上任何基于 Steamworks SDK 的工具都可以。
## 4. 上传后：在创意工坊页面上完善

物品地址：`https://steamcommunity.com/sharedfiles/filedetails/?id=<上一步打印的 id>`

打开新物品页面（Steam → 创意工坊 → 你的物品），至少做这几件事：

1. **描述**：把 `workshop/description.bbcode` 的内容整段贴进描述框（Steam 支持 `[h1]`、`[list]`、`[url]` 等 BBCode）。
2. **标签**：同游戏其他模组常见标签是 `Mod` / `Gameplay` / `tools`，按需勾选。
3. **预览图**：确认已显示 `preview.png`；若没有，在页面右侧"预览图像"里重新上传。
4. **可见性**：测试完成后从"隐藏"改成"公开"，并把 VDF 里的 `visibility` 改回 `0`（后续更新会按 VDF 重设可见性）。
5. 检查**前置**：本模组在 manifest 里已声明两个前置，玩家启用时会检查；描述里也写清了需要手动订阅。

## 5. 更新已发布物品

1. 改源码/规则 → `build.ps1` → 更新 `mod/manifest.xml` 的 `<ModVersion>` 与 `CHANGELOG.md`。
2. 编辑 VDF：`publishedfileid` 保持上次写回的 id，`changenote` 写本次更新内容。
3. 再跑一次第 3 步的命令即可；订阅者会自动收到更新。

## 6. 验收清单

- [ ] 订阅后游戏内 Mods 菜单显示该物品，版本颜色为绿（`1.0.0` 覆盖 1.0.0.16）。
- [ ] 启用时若能加载，说明 manifest / Prerequisites / DLL 都正常。
- [ ] Player.log 出现 `pipette rules loaded: 18`、11 行 `pipette patch applied`、进入游戏后
      `[pipette-vce-compat] hooked PenPipette.PipetteTransfer.OnTarget`。
- [ ] 用一个**没装过本地副本**的存档/账号试订阅，确认 Steam 下载的文件夹结构是
      `workshop/content/4348910/<itemid>/manifest.xml`。
- [ ] 更新一次（改 changenote 重新上传），确认订阅端能收到新版本。

## 备选上传方式

- 社区工具 **Probably Mod Manager**（创意工坊物品 3832565677）自带实验性的 SteamCMD 发布功能，界面操作即可，但底层同样是 `workshop_build_item`。
- 官方指南推荐 `steamcmd`，因为它最稳定、且 `publishedfileid` 会回写，方便以后更新。