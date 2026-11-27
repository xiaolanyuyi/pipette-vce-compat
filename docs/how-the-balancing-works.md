# 这个补丁是怎么工作的

## 1. 扩展点：PipetteReactions.json

Pen的移液枪会扫描每个已启用 mod 根目录下的 `PipetteReactions.json`，把里面的 `modes` 与 `patches`
编译进它自己的 hydra 规则表（在内存里，不写回任何文件）。本补丁全部规则都用这个公开接口实现，
DLL 只负责桶守卫这个"交互拦截"。

规则是纯数据的：

```json
{
  "modes": [ { "id": "...", "mainLiquid": "...", "detect": "share", "minShare": 0.15, "stages": [ ... ] } ],
  "patches": [ { "mode": "acid", "addStage": { ... } } ]
}
```

- `modes`：新增一条 hydra 规则（枪里吸满 `mainLiquid` 即进入该模式）。
- `patches`：往内置规则（acid/base/alcohol/reducing…）追加阶段，不覆盖原阶段。

## 2. 配平（topup）怎么算

每条配平阶段写成：

```json
{
  "id": "vce_balance_crackine",
  "kind": "topup",
  "conversion": false,
  "substrate": { "liquid": "vce_alkaloid", "locations": ["target"] },
  "catalytic": { "liquid": "acid", "locations": ["pipette"] },
  "forbidOver": { "water": 0.01 }
}
```

移液枪内部会把它编译成：

- `required = substrate × ratio`（ratio 取自 `catalyticFullAt`）
- `need = max(0, round(required) - target.催化液体)`
- `transfer = round(need × totalPipette / pipette.催化液体)`

也就是"目标里还差多少试剂"，再按枪内试剂浓度换算成实际倒出的体积。
`need = 0` 时移液枪直接返回"已达配平"，不会倒液——所以重复拖拽是安全的。

`forbidOver` / `requires` 是按**目标液体占比**的前置条件（例如快克因要求含水 ≤1%、
生石灰苛化要求碱 ≥50%），不满足时该阶段不触发。

## 3. 为什么是"配平"而不是"瞬间反应"

VCE 的化学是挂在容器每秒 tick（夜间额外 360 tick）上的持续结算。移液枪的 hydra 原本可以在拖拽瞬间
直接结算反应；但那样会跳过 VCE 的速率与平衡。本补丁选择只调比例，让 VCE 自己按原节奏反应，
效果等同于"自动、精确地加试剂"，而不是加速器。

## 4. 检测阈值与优先级

移液枪的 hydra 模式完全由"从源容器吸到什么主液体"推断（没有选择 UI）：主液体占总体积 ≥ `minShare`
才会被识别。VCE 很多试剂天生稀释（生石灰 25–30%、硅胶 ≤50%、乙酸 ≤33%、硝酸 ≤40%），
所以本补丁把阈值下调（12–30%），并把优先级设成 65（内置模式 70–110），
保证有内置模式能匹配时优先走内置行为，不会抢移液枪原有手感。

## 5. 核废料桶守卫（DLL）

VCE 把撬开的核废料桶做成"内容物不能离开"的容器，方式是在 `WaterHelper.TransferLiquid` 上做拦截。
移液枪不走这个函数（它直接改物品的液体 tag），所以本补丁在移液枪的 `OnTarget` 上做对称处理：

- **吸取方向**（空枪拖到桶上）：Prefix 记录桶内所有"不允许离开"的液体，Postfix 原样放回；
- **倒入方向**（枪里有液体）：完全不动，便于溶解/还原/萃取/玻璃固化。

跨 mod 调用走游戏提供的 `ModReflect`（前置 mod 需在 manifest 的 `<Prerequisites>` 里声明），
不申请反射能力。因为游戏加载器在调用 `OnEnable` 时还没把模组登记进 `ModLoader.Mods`，
守卫会等到第一个"所有模组登记完成"的钩子（模组资产加载 / 读档 / 新游戏）再安装。
