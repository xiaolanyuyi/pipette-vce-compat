## v2.0.1 — 创意工坊上架准备

**移液枪 × 原版化学扩展 兼容补丁**，适用于《深空当铺·可能是偷的》**1.0.0.16（VERSION 100-Rev16）**，
前置：Pen的移液枪 1.1.5 + 原版化学扩展 1.11.1。

### 本版变更
- `manifest.xml` 的 `<GameVersions>` 从旧编号 `0.46D` 改为 `1.0.0`（覆盖 1.0.0.16；旧编号在 1.0 之后会被忽略）。
- 新增 `preview.png`（512×512）：模组菜单 / 创意工坊预览图。
- 新增创意工坊发布资料（`workshop/`：VDF 模板、中英双语描述、steamcmd 发布步骤）。
- **无代码与配平规则变更**。

### 安装
1. 订阅并启用 Pen的移液枪 与 原版化学扩展。
2. 把 `pipette_vce_compat` 文件夹放进 `<Steam>\steamapps\common\Probably Stolen\Mods\`，
   或在创意工坊订阅本补丁。
3. 游戏内 MOD 列表启用本补丁；日志确认 `pipette rules loaded: 18`、11 行 `pipette patch applied`、
   进入游戏后 `[pipette-vce-compat] hooked PenPipette.PipetteTransfer.OnTarget`。

完整说明见 [README](https://github.com/xiaolanyuyi/pipette-vce-compat#readme)。
