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
| `visibility` | `0` 公开 / `1` 仅好友 / `2` 隐藏（建议先用 `2` 测试） |
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

## 4. 在创意工坊页面上完善

打开新物品页面（Steam → 创意工坊 → 你的物品），至少做这几件事：

1. **描述**：把 `workshop/description.bbcode` 的内容整段贴进描述框（Steam 支持 `[h1]`、`[list]`、`[url]` 等 BBCode）。
2. **标签**：同游戏其他模组常见标签是 `Mod` / `Gameplay` / `tools`，按需勾选。
3. **预览图**：确认已显示 `preview.png`；若没有，在页面右侧"预览图像"里重新上传。
4. **可见性**：测试完成后从"隐藏"改成"公开"。
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
