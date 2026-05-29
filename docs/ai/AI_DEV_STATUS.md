# AI_DEV_STATUS

更新时间：2026-05-29

本文档基于当前仓库代码静态检查得出，只评估“仓库里已经落地的实现”，不按设计文档目标高估完成度，也未进行 Unity 运行时手动验证。

## 结论概览

当前项目最成熟的是一套可玩的最小战斗闭环：`大厅 -> 选图 -> 选人 -> 战斗 -> 奖励/升级 -> 回到大厅 -> 存档`。

战斗相关模块整体已经形成完整链路；非战斗部分大多是围绕这个闭环服务的轻量实现。剧情对话、悬赏、据点经营仍未形成独立系统。

## 核心模块状态

| 模块 | 完成度 | 判断 |
| --- | --- | --- |
| 战斗系统 | 已完成 | 已有完整 4v4 回合战闭环，包含行动、技能释放、结算、死亡/濒危、奖励与返回流程。主逻辑集中在 `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs`。 |
| 站位系统 | 已完成 | 有 4 槽位站位、前后排判断、合法站位校验、换位、击退、阵型压缩与重排。见 `BattleController.cs` 的站位校验、`ExecuteSwap`、`CompactFormation`、`LayoutFormation`。 |
| 技能系统 | 已完成 | 技能数据、资源消耗、冷却、目标选择、AI 用法、表现层都已接通。见 `SkillData.cs`、`BattleController.cs` 的 `GetSkillUseState`、`ExecuteSkill`。 |
| 角色数据 | 已完成 | 角色定义、成长、专精、被动、恢复状态、招募解锁、运行时生命值都已实现。见 `CombatantDefinition.cs`。 |
| 敌人数据 | 半成品 | 敌人使用同一套 `CombatantDefinition + SkillData` 数据，并已接入地图战斗配置；但敌人种类、专属规则和独立系统层较薄，仍偏 demo 数据量。 |
| 回合流程 | 已完成 | 有回合队列、速度排序、回合开始状态结算、玩家/AI 行动、胜负收尾。见 `BattleController.cs` 的 `BattleLoop`、`RunTurn`。 |
| Buff / Debuff | 已完成 | 已有状态实例、持续回合、DOT、眩晕、净化、护卫、压制、图标与 tooltip 展示。见 `BattleUnit.cs`、`BuffData.cs`、`DebuffStatusBarFeature.cs`。 |
| 战斗 UI | 已完成 | 已有战斗 HUD、技能列表、目标信息、日志、浮字、特写、攻击表现和状态栏。见 `BattleUI.cs`、`BattleHudUI.cs`、`CombatantView.cs`。 |
| 对话系统 | 未接入 | 仓库里只有剧情文档，没有运行时代码、对话数据结构、对话 UI 或触发流程。 |
| 招募系统 | 半成品 | 酒馆招募、价格校验、解锁状态、存档持久化已接通；但目前是轻量招募，不是完整角色来源系统。见 `BattleController.cs` 的 `ShowTavern`、`RecruitHero`，以及 `TavernUI.cs`。 |
| 悬赏系统 | 未接入 | 当前只有“冒险地图/战斗地图”流程，没有独立的悬赏接单、任务条目、任务状态、悬赏结算对象模型。 |
| 据点系统 | 只有 UI | 有大厅界面和若干入口按钮，但没有建筑、升级、房间、产出或据点状态数据。见 `LobbyUI.cs`。 |
| 存档系统 | 已完成 | 已有多存档槽、覆盖/删除、读档、迁移旧存档、角色与地图进度持久化。见 `BattleController.cs` 的 `SaveGameState`、`LoadGameFromSlot`、`GetSaveSlotSummary`。 |

## 重点检查结果

### 1. 战斗系统：已完成

依据：

- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:1870` `BattleLoop`
- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:1899` `RunTurn`
- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:2062` `ExecuteSkill`
- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:1535` `GrantVictoryRewards`

判断：

- 已具备开战、行动、伤害/治疗、死亡、奖励、升级选择、结束返回的完整闭环。
- 不再是单纯展示 UI 或单次脚本演示。

### 2. 站位系统：已完成

依据：

- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:2254` `ExecuteSwap`
- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:2530` `GetValidTargets`
- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:2601` `HandleDefeatedUnit`
- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:2673` `CompactFormation`

判断：

- 技能已受施法站位和目标站位约束。
- 阵亡/濒危后会释放槽位并重排阵型。
- 存在换位技能与击退效果，说明站位已参与实际结算，不只是展示标签。

### 3. 技能系统：已完成

依据：

- `Assets/Scripts/ClockworkWasteland/Combat/SkillData.cs`
- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:2062` `ExecuteSkill`
- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:2906` `CalculateDamage`
- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:3661` `CalculateHealingAmount`
- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:3192` `ApplySkillSpecificPostEffect`

判断：

- 技能已经数据驱动到一定程度：目标类型、位置限制、消耗、冷却、Buff 附加都有。
- 但不少技能特例仍硬编码在 `BattleController`，扩展性一般，属于“当前内容已能用，但架构还没拆分”。

### 4. 角色数据：已完成

依据：

- `Assets/Scripts/ClockworkWasteland/Combat/CombatantDefinition.cs:30`
- `Assets/Scripts/ClockworkWasteland/Combat/CombatantDefinition.cs:386` `MarkWounded`
- `Assets/Scripts/ClockworkWasteland/Combat/CombatantDefinition.cs:401` `AdvanceRecovery`
- `Assets/Scripts/ClockworkWasteland/Combat/CombatantDefinition.cs:466` `GrantExperienceReward`

判断：

- 已覆盖角色身份、属性、成长、职业原型、专精、被动、招募、恢复状态、技能表。
- 英雄与敌人共用一套定义，便于最小闭环推进。

### 5. 敌人数据：半成品

依据：

- `Assets/ClockworkWastelandDemo/Data/Combatants/` 下已有敌人资产
- `Assets/Scripts/ClockworkWasteland/Combat/AdventureMapData.cs:32` `AdventureBattleConfig`
- `Assets/Scripts/ClockworkWasteland/Combat/AdventureMapData.cs:48` `GetOrderedPlacements`

判断：

- 敌人已能被地图战斗配置引用并正常出战。
- 但当前更像“少量样例敌人 + 通用战斗规则”，还不是完整的敌人设计体系。

### 6. 回合流程：已完成

依据：

- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:1870` `BattleLoop`
- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:1899` `RunTurn`

判断：

- 已有速度排序、每回合开始状态结算、玩家输入等待、敌方 AI 自动决策。
- 这一层已经是可玩的，不是占位代码。

### 7. Buff / Debuff：已完成

依据：

- `Assets/Scripts/ClockworkWasteland/Combat/BattleUnit.cs:200` `AddOrRefreshBuff`
- `Assets/Scripts/ClockworkWasteland/Combat/BattleUnit.cs:211` `TickStatuses`
- `Assets/Scripts/ClockworkWasteland/Combat/BuffData.cs`
- `Assets/Scripts/ClockworkWasteland/Combat/DebuffStatusBarFeature.cs`

判断：

- 状态已真正参与战斗数值和行动限制，不只是文本标签。
- 还有状态 UI 图标和鼠标提示，接入度较高。

### 8. 战斗 UI：已完成

依据：

- `Assets/Scripts/ClockworkWasteland/Combat/UI/Core/BattleUI.cs`
- `Assets/Scripts/ClockworkWasteland/Combat/UI/HUD/BattleHudUI.cs`
- `Assets/Scripts/ClockworkWasteland/Combat/CombatantView.cs`

判断：

- 战斗 HUD、角色面板、技能按钮、日志、目标提示、浮字和表现演出都已接通。
- UI 不只是 prefab，已经被 `BattleController` 在实际流程中驱动。

### 9. 对话系统：未接入

依据：

- 代码中未找到对话控制器、对话数据对象、对话 UI、剧情触发器。
- 仅有 `docs/ai/具体剧情故事.md` 等策划文本。

判断：

- 目前只有世界观和剧情文档，没有游戏内系统实现。

### 10. 招募系统：半成品

依据：

- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:584` `ShowTavern`
- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:594` `RecruitHero`
- `Assets/Scripts/ClockworkWasteland/Combat/UI/Screens/TavernUI.cs:37` `Show`

判断：

- 已有招募入口、候选列表、价格判断、解锁持久化。
- 但没有刷新规则深度、事件招募、池子成长或更复杂经济结构。

### 11. 悬赏系统：未接入

依据：

- 代码层没有 `Bounty`、`Contract`、`Quest` 一类的系统对象。
- 当前可玩的任务入口是 `AdventureMapData` 和战斗关卡，不是悬赏任务系统。

判断：

- 现在更接近“战斗地图选择”，不是“事务所接悬赏”的完整玩法层。

### 12. 据点系统：只有 UI

依据：

- `Assets/Scripts/ClockworkWasteland/Combat/UI/Screens/LobbyUI.cs`
- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:417` `ShowLobby`

判断：

- 有大厅/主界面作为枢纽。
- 但没有据点建筑、升级、耐久、资源产出、人员驻留等据点状态模型。

### 13. 存档系统：已完成

依据：

- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:996` `SaveGameState`
- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:1137` `LoadGameFromSlot`
- `Assets/Scripts/ClockworkWasteland/Combat/BattleController.cs:1214` `GetSaveSlotSummary`
- `Assets/Scripts/ClockworkWasteland/Combat/UI/Screens/SaveSlotUI.cs:36` `Show`

判断：

- 已支持多槽位读写、覆盖、删除、旧档迁移。
- 存储内容包含金币、英雄状态、地图通关进度，已经超过最基础的单值保存。

## 补充模块状态

| 模块 | 完成度 | 判断 |
| --- | --- | --- |
| 冒险地图 / 关卡配置 | 半成品 | 已有 `AdventureMapData`、地图解锁、战斗列表、敌人站位配置；但仍是轻量任务容器。 |
| 队伍选择 | 已完成 | 有可出战英雄过滤、最多 4 人编队和出战流程衔接。 |
| 商店 | 半成品 | 可买道具并进入存档，但商品仍很少，且可使用运行时默认道具回退。 |
| 背包 / 战斗外道具 | 半成品 | 可对英雄用药、治疗、复活并保存；但没有更丰富的物品体系。 |
| 伤员恢复 / 医疗 | 半成品 | 濒危后进入休养，医疗站可花钱急救恢复；但还不是完整基地医疗系统。 |
| 升级 / 专精选择 | 半成品 | 已有升级奖励、2 级专精、3 级成长被动；但职业树深度仍有限。 |
| 英雄图鉴 | 只有 UI | 有界面和展示入口，但偏展示层，不是完整收集系统。 |

## 当前项目真实开发重心

从代码实际落地情况看，当前项目的真实重心是：

1. 先做通一套“可战斗、可奖励、可养成、可回大厅、可存档”的最小可玩闭环。
2. 用大厅、酒馆、商店、背包、治疗站把这套闭环串起来。
3. 暂时没有把剧情对话、悬赏包装、据点经营做成独立系统。

## 风险与建议

- `BattleController.cs` 过于庞大，已经同时承担战斗、地图、招募、商店、背包、存档、升级选择等职责，后续继续加功能会很快失控。
- 技能虽然可用，但大量效果是 `skillId` 分支硬编码；当角色和技能数量继续增加时，维护成本会明显上升。
- 非战斗模块很多已经“能点开”，但系统化程度不足，后续最好优先决定：是继续强化战斗内容，还是把“悬赏/对话/据点”补成第二条主线。

## 一句话总结

当前仓库不是“什么都没做”的原型，而是“战斗闭环基本成型、外围系统轻量接入、剧情/悬赏/据点尚未系统化”的状态。
