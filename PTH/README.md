# Unknown Technology

## 1. Project Overview

**Unknown Technology** 是一款第一人称教育型博物馆沙盒解谜游戏。玩家扮演科技与创新展览的博物馆员工，调查九件失踪或损坏的文物，并使用一把可升级的权杖寻找线索、回收碎片和完成修复。每次修复都会补充历史事实、短篇闪回与技术时间线，最终连接古代、现代和未来三个时代。

本仓库已经形成第一人称可玩基础切片：可从正式入口进入主菜单并加载古代展馆灰盒，使用键鼠或手柄移动、观察、暂停和调整基础设置。M01–M04 已按当前范围完成，M05 已完成支撑该切片的第一人称控制；文物交互、修复、历史内容和完整游戏循环仍待后续模块实现。

## 2. High Concept

玩家在一座舒适而神秘的科技博物馆中调查缺失文物，通过观察、逻辑推理和空间装配重建技术史。核心体验不是战斗，而是发现、理解和修复：玩家既是调查员，也是文物修复者和历史整理者。

## 3. Core Experience

游戏围绕四个体验支柱展开：

- **调查与发现**：探索展馆、阅读环境线索、找到文物碎片与事实。
- **逻辑与修复**：移动、旋转、匹配并吸附碎片，重建文物。
- **历史联系**：理解文物解决了什么问题，以及它如何影响后续技术。
- **展馆恢复**：空展柜、照明、声音和时间线随进度逐步恢复，提供清晰的成就反馈。

## 4. Target Audience

- 对历史、科技或博物馆主题感兴趣的玩家。
- 游戏经验有限、需要清晰引导和低操作负担的玩家。
- 不设严格年龄范围；最终历史文本、题目难度和内容分级仍需由内容团队审核。

## 5. Target Platforms

- 主要平台：Windows 64 位。
- 发布平台：支持 WebGL 的网站或 itch.io 页面。
- 输入：键盘鼠标与通用手柄均为首版验收范围。
- 最低界面基线：960×600。
- 不以触屏、XR 或本地多人为首版目标。

## 6. Core Features

- 第一人称博物馆调查与环境交互。
- 一把贯穿全程、可升级的文物修复权杖。
- 古代、现代、未来三个时代展馆。
- 九件独特文物，每个时代三件。
- 碎片搜索、空间旋转、匹配和吸附修复。
- 历史事实笔记、文物闪回与技术时间线。
- 每个时代一组 3 题测验，通过后解锁时代权杖。
- 条件线性 NPC 对话与渐进式提示。
- 自动存档、`Continue`、键鼠与手柄提示，以及基础无障碍设置。

## 7. Scope and Constraints

目标游戏时长约 20 分钟：序章约 2 分钟，三个时代各约 5 分钟，结尾约 3 分钟。首个可玩目标是 5–6 分钟的古代展馆垂直切片。

首版明确不包含：

- 战斗、生命值、死亡、时间限制或资源惩罚。
- 蹲伏、冲刺、攀爬和复杂移动能力；仅保留基础落地跳跃。
- NPC 选择树、支线任务或行为树。
- 可自由操作的历史闪回关卡。
- 触屏、XR、联网、云存档或多人功能。
- 三把权杖各自独立的操作系统。
- 未经来源登记和内容审核的历史事实。

## 8. Core Gameplay Loop

单件文物循环：

```text
发现空展柜
→ 调查附近线索
→ 获得历史事实
→ 找到2–4个碎片
→ 前往修复台
→ 移动、旋转和装配
→ 文物恢复
→ 观看可跳过闪回
→ 写入历史笔记和时间线
```

单时代循环：

```text
进入展馆
→ NPC介绍时代主题
→ 修复三件文物
→ 查看时代时间线
→ 完成三题测验（3/3）
→ 解锁时代权杖
→ 开放下一时代
```

完整流程：

```text
主菜单 → 序章 → 古代 → 现代 → 未来 → 完整时间线 → 结局
```

详细规则见[核心玩法](Documentation/01_GameDesign/CoreGameplay.md)与[玩家流程](Documentation/01_GameDesign/PlayerFlow.md)。

## 9. Current Repository Status

以下状态基于 2026-08-31 的实现与验证结果。

| 项目 | 当前状态 |
|---|---|
| Unity 版本 | `6000.4.10f1` |
| 渲染 | URP `17.4.0` 已安装并配置 |
| 输入 | Input System `1.19.0`；已建立正式 `Gameplay`、`Restoration`、`UI` Action Maps，以及键鼠与手柄控制方案 |
| 导航 | AI Navigation `2.0.12` 已安装 |
| UI | uGUI `2.0.0` 与 TMP；已有主菜单、暂停菜单和最小设置面板，完整 M11 尚未实现 |
| 时间线 | Timeline `1.8.12` 已安装；尚无游戏闪回内容 |
| 测试 | Unity Test Framework `1.6.0`；Edit Mode 33/33、Play Mode 3/3 通过 |
| 正式场景 | `Bootstrap`、`MainMenu`、`Era_Ancient`、`Era_Modern`、`Era_Future` 已登记；Modern/Future 当前锁定 |
| SampleScene | 文件保留，但已从正式 Build Settings 移除 |
| 全局状态 | `GamePhase`、只读快照、显式转换守卫、暂停恢复和唯一 `GameContext` 已实现 |
| EventBus | 非静态强类型总线、快照发布及 `IDisposable` 订阅已实现；旧字符串占位总线已移除 |
| 玩家控制 | 第一人称 `CharacterController` 移动、落地跳跃、重力、碰撞、镜头、灰盒手部与权杖反馈已实现 |
| 环境交互 | 未实现 |
| 权杖与修复 | 未实现 |
| NPC 与对话 | 未实现 |
| UI 与无障碍 | 仅完成当前切片所需 UI 缩放、Y 轴反转、灵敏度和 Reduced Motion；完整 M11 未实现 |
| 音频控制 | 未实现 |
| 存档与进度 | 仅设置使用项目专属 PlayerPrefs 键持久化；M13 游戏进度存档未实现 |
| 三时代内容 | Ancient 为可探索灰盒；Modern/Future 是锁定占位场景，M07 内容数据未实现 |
| Windows 构建 | Development 构建成功，隐藏启动 8 秒无运行时错误 |
| WebGL 构建 | Development 构建成功，`index.html` 与 WASM 经本地 HTTP 返回 200 |

```text
Project Stage: Playable foundation / Vertical slice
Current Playable State: Main Menu → Ancient greybox is playable
Current Milestone: M01–M04 complete; M05 first-person slice complete
```

## 10. Module Completion Status

状态统一使用 `Planned → In Progress → Review → Complete`。

| 模块 | 状态 | 当前完成 | 下一步 |
|---|---|---|---|
| M01：启动与全局状态 | Complete | 状态快照、转换守卫、暂停恢复、唯一 Bootstrap/Context、时代场景开发回退 | 后续模块只通过公开状态接口接入 |
| M02：事件总线 | Complete | 强类型发布订阅、可释放句柄、订阅快照、异常日志与测试清理 | 后续模块新增只读事件消息 |
| M03：场景流程 | Complete | 五场景路由、异步单事务、门禁、出生点恢复、失败事件和最小菜单入口 | M09/M13 完成后替换门禁与 Continue 适配器 |
| M04：输入与设置 | Complete | 三套 Action Maps、键鼠/手柄、设备事件、安全的帧末映射切换、灵敏度、反转、Reduced Motion、设置持久化 | M11/M12 接入完整 UI 与音量消费端 |
| M05：玩家控制 | Complete | 当前切片所需移动、落地跳跃、碰撞、镜头、锁定和灰盒表现已完成 | 后续只增加交互挂点，不扩展战斗移动 |
| M06：环境交互 | Planned | 无 | 设计统一聚焦和交互规则 |
| M07：内容数据 | Planned | 无 | 设计时代、文物和事实数据 |
| M08：权杖与文物修复 | Planned | 无 | 设计探测、碎片与装配会话 |
| M09：进度与测验 | Planned | 无 | 设计门禁、测验和奖励状态 |
| M10：NPC 与对话 | Planned | 无 | 设计条件线性对话与导航状态 |
| M11：UI 与无障碍 | In Progress | 已有主菜单、暂停/设置面板、UI 缩放与 Reduced Motion 接口 | 完成全部面板、焦点导航、字幕和 960×600 验收 |
| M12：音频与闪回 | Planned | 无 | 设计音频事件和闪回表现 |
| M13：存档 | Planned | 无 | 设计进度与设置的存档边界 |
| M14：验证与测试 | In Progress | M01–M05 自动测试、Windows/WebGL 构建和启动检查已建立 | 后续为 M06–M13 增加对应验证，不将当前覆盖误标为完整 M14 |

单个模块的状态记录格式：

```text
Module:
Phase:
Status:
Completed:
Known Issues:
Next Step:
```

## 11. Planned Implementation Phases

1. **已完成——可玩基础**：M01、M02、M03、M04，以及当前范围的 M05。
2. **下一阶段——探索交互**：M06 和 M07，建立统一交互契约与文物数据。
3. **核心闭环**：M08、M09，完成权杖、碎片修复、测验与门禁。
4. **呈现与引导**：M10、M11、M12，补齐 NPC、完整无障碍 UI、音频和闪回。
5. **进度与内容**：M13 正式存档，并配置三时代九件文物。
6. **验证与发布**：完成 M14 全模块覆盖、性能验收和 itch.io 发布检查。

## 12. Documentation Index

- [核心玩法设计](Documentation/01_GameDesign/CoreGameplay.md)
- [技术架构总览](Documentation/02_TechnicalDesign/ArchitectureOverview.md)
- [技术模块索引](Documentation/02_TechnicalDesign/Modules/README.md)
- [内容总纲](Documentation/03_Content/ContentBible.md)
- [测试策略](Documentation/05_QA/TestStrategy.md)

文档中出现的 `TBD` 表示尚未取得可靠内容或尚未完成产品决策，后续不得以未经审核的事实替换。

## 13. Build and Development Requirements

- Unity Editor：`6000.4.10f1`。
- 目标构建：Windows 64 位与 WebGL。
- 主要包版本以 `Packages/manifest.json` 为准。
- 场景、资源、代码与测试位于 `Assets/UnknownTechnology`；TMP 基础资源位于 `Assets/TextMesh Pro`。
- 技术实现应遵守 Core → Gameplay → Presentation → Validation 与 Tests 的单向依赖。
- Development 构建输出到被 Git 忽略的 `Builds/Development/Windows` 与 `Builds/Development/WebGL`。
- 自动测试结果输出到 `Temp/TestResults`，不会进入正式资源目录。
- 发布前必须通过[发布检查清单](Documentation/05_QA/ReleaseChecklist.md)。
