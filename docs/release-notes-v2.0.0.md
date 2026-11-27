## v2.0.0 — 一键配平版

**移液枪 × 原版化学扩展 兼容补丁**，适用于《深空当铺·可能是偷的》0.46D，前置：
Pen的移液枪 1.1.5 + 原版化学扩展 1.11.1。

### 安装
1. 解压 `pipette_vce_compat` 文件夹到 `<Steam>\steamapps\common\Probably Stolen\Mods\`。
2. 游戏内 MOD 列表启用「移液枪 × 原版化学扩展 兼容补丁」。
3. 日志确认：`pipette rules loaded: 18`、11 行 `pipette patch applied`、进入游戏后 `hooked PenPipette.PipetteTransfer.OnTarget`。

### 内容
- **18 条配平规则**：生石灰苛化、硅胶脱氟、硝酸硝化、乙酸酯化、梦帽/梦魇萃取液 1:1、水玻璃固化、
  乙酸乙酯钚萃取、梦毒/硝化/酯化催化酸、核废料酸/还原剂/乙酸乙酯/水玻璃、快克因成盐、辣酯、中和硝酸/乙酸等。
- **核废料桶守卫**：移液枪不能再从撬开的核废料桶吸走放射液（倒入不受影响）。
- 可选「瞬间反应版」规则：`mod\optional\instant-reactions.json`，覆盖根目录同名文件即切换。

完整说明见 [README](https://github.com/xiaolanyuyi/pipette-vce-compat#readme)。
