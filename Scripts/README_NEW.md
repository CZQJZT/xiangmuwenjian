# 🎮 Unity军棋游戏 (Unity Junqi)

一个功能完整的中国军棋（陆战棋）Unity实现，支持传统回合制和RTS实时对战两种模式。

---

## 📋 目录

- [项目简介](#项目简介)
- [核心特性](#核心特性)
- [技术栈](#技术栈)
- [项目结构](#项目结构)
- [快速开始](#快速开始)
- [游戏模式](#游戏模式)
- [系统架构](#系统架构)
- [API文档](#api文档)
- [扩展开发](#扩展开发)
- [常见问题](#常见问题)

---

## 📖 项目简介

这是一个从JavaScript/Next.js版本移植到Unity的完整军棋游戏实现。项目不仅保留了原始版本的所有核心功能，还新增了RTS实时对战模式、行为树AI、完善的UI系统等高级特性。

**军棋规则**：中国军棋是一种双人对弈的策略棋类游戏，棋子包括司令、军长、师长等不同等级，通过吃子、占位等策略夺取对方军旗获得胜利。

---

## ✨ 核心特性

### 🎯 游戏系统
- ✅ **完整军棋规则**：25个棋子/方，12种棋子等级，完整的战斗判定系统
- ✅ **标准棋盘**：5列×13行，包含行营、铁路等特殊地形
- ✅ **双模式支持**：
  - 传统回合制模式（交替行动）
  - RTS实时对战模式（并发行动，行动点系统）
- ✅ **游戏阶段**：布阵阶段 → 对战阶段 → 结束判定

### 🤖 AI系统
- ✅ **智能布阵**：6种预设布局模板，4种难度级别
- ✅ **行为树AI**：基于行为树的决策系统（Easy/Medium/Hard/Cheating）
- ✅ **优先级策略**：大子靠后防守，小子前出侦察
- ✅ **路径规划**：工兵铁路飞行算法，A*寻路

### 🎨 UI系统
- ✅ **可视化棋盘**：动态渲染棋盘格子和连线
- ✅ **棋子动画**：平滑的移动动画和战斗特效
- ✅ **交互系统**：点击选择、拖拽移动、高亮提示
- ✅ **音效管理**：背景音乐和战斗音效

### ⚙️ RTS系统（新增）
- ✅ **行动点机制**：每0.5秒恢复0.1 AP，每次移动消耗1 AP
- ✅ **并发操作**：玩家和AI同时行动，无需等待
- ✅ **实时调度**：动作队列管理，避免冲突
- ✅ **战斗引擎**：独立的RTS战斗判定系统

### 🛠️ 技术特性
- ✅ **高性能设计**：O(1)棋子查询，扁平数组优化
- ✅ **事件驱动**：松耦合的事件回调系统
- ✅ **状态克隆**：支持AI计算的状态深拷贝
- ✅ **数据配置**：ScriptableObject配置文件，易于调整平衡性

---

## 💻 技术栈

| 类别 | 技术 |
|------|------|
| **引擎** | Unity 2020.3 LTS 或更高版本 |
| **语言** | C# (.NET Standard 2.1) |
| **UI框架** | Unity UI (uGUI) + TextMeshPro |
| **AI架构** | 行为树 (Behavior Tree) |
| **数据存储** | ScriptableObject + Resources |
| **音频** | Unity AudioSource |
| **动画** | Unity Coroutine + Lerp插值 |

---

## 📁 项目结构

```
UnityJunqi/
├── Assets/
│   ├── Scripts/                          # 核心代码
│   │   ├── Core/                         # 核心游戏逻辑
│   │   │   ├── Board.cs                  # 棋盘管理（65个位置）
│   │   │   ├── BoardPosition.cs          # 棋盘位置结构（a1-e13）
│   │   │   ├── Piece.cs                  # 棋子类（含HP/ATK属性）
│   │   │   ├── GameRules.cs              # 游戏规则引擎
│   │   │   ├── GameState.cs              # 游戏状态管理
│   │   │   ├── PathFinder.cs             # 路径查找（A*算法）
│   │   │   ├── PathCombatDetector.cs     # 路径战斗检测
│   │   │   ├── BoardUtils.cs             # 棋盘工具函数
│   │   │   ├── CellType.cs               # 格子类型枚举
│   │   │   ├── Enums.cs                  # 全局枚举定义
│   │   │   └── PieceDragHandler.cs       # 棋子拖拽处理
│   │   │
│   │   ├── AI/                           # AI系统
│   │   │   ├── AILayoutGenerator.cs      # AI布阵生成器
│   │   │   ├── AIBehaviorTree.cs         # AI行为树主控制器
│   │   │   ├── AIContext.cs              # AI上下文数据
│   │   │   ├── BehaviorTree/             # 行为树节点
│   │   │   │   ├── BTNode.cs             # 节点基类
│   │   │   │   ├── BTAction.cs           # 动作节点
│   │   │   │   ├── BTSelector.cs         # 选择器节点
│   │   │   │   ├── BTSequence.cs         # 序列器节点
│   │   │   │   └── Actions/              # 具体行为动作
│   │   │   └── RandomNumberGenerator.java # 随机数生成器（遗留）
│   │   │
│   │   ├── RTS/                          # RTS实时系统（新增）
│   │   │   ├── RTSController.cs          # RTS调度器核心
│   │   │   ├── RTSState.cs               # RTS状态管理
│   │   │   ├── CombatEngine.cs           # 战斗引擎
│   │   │   ├── Bridge/                   # 桥接层
│   │   │   │   └── HealthAttackBridge.cs # HP/ATK桥接
│   │   │   ├── Combat/                   # 战斗相关
│   │   │   ├── Data/                     # 数据配置
│   │   │   │   ├── HealthAttackConfigSO.cs # HP/ATK配置
│   │   │   │   └── RTSConfigSO.cs        # RTS配置
│   │   │   └── Interfaces/               # 接口定义
│   │   │
│   │   ├── UI/                           # UI系统
│   │   │   ├── GameUIManager.cs          # UI管理器（1700+行）
│   │   │   ├── BoardRenderer.cs          # 棋盘渲染器
│   │   │   ├── BoardLineRenderer.cs      # 连线渲染器
│   │   │   ├── PieceDisplay.cs           # 棋子显示
│   │   │   ├── PieceSelectionManager.cs  # 棋子选择管理
│   │   │   ├── AnimationManager.cs       # 动画管理器
│   │   │   ├── AudioManager.cs           # 音频管理器
│   │   │   ├── BoardCellClickHandler.cs  # 格子点击处理
│   │   │   └── Editor/                   # 编辑器扩展
│   │   │
│   │   ├── MonoBehaviours/               # Unity组件
│   │   │   └── JunqiGameManager.cs       # 游戏主控制器
│   │   │
│   │   ├── Tests/                        # 测试脚本
│   │   │   └── GameLogicTester.cs        # 逻辑测试
│   │   │
│   │   └── Examples/                     # 示例代码
│   │       └── SimpleExample.cs          # 简单示例
│   │
│   ├── Resources/                        # 资源配置
│   │   └── RTS/
│   │       └── Data/
│   │           └── HealthAttackConfig.asset # HP/ATK配置数据
│   │
│   ├── prefeb/                           # 预制体
│   │   ├── Panel.prefab                  # UI面板
│   │   └── Square.prefab                 # 棋盘格子
│   │
│   ├── image/                            # 图片资源
│   │   └── 生成游戏背景图 (1).png        # 游戏背景
│   │
│   ├── Sound/                            # 音频资源
│   │   ├── 铁轨与旗影.mp3                # 背景音乐
│   │   └── GUNTech_Tormentor Shotgun Fire_05.wav # 战斗音效
│   │
│   └── SIMSUNEXTG.TTF                    # 中文字体
│
├── README.md                             # 本文档
├── ARCHITECTURE.md                       # 架构设计文档
├── PROJECT_SUMMARY.md                    # 项目总结
├── QUICK_START.md                        # 快速启动指南
└── CHECKLIST.md                          # 功能检查清单
```

---

## 🚀 快速开始

### 环境要求
- Unity 2020.3 LTS 或更高版本
- .NET Standard 2.1 兼容

### 步骤1：打开项目
1. 在Unity Hub中添加项目
2. 选择路径：`d:\unity\My project (8)\Assets\UnityJunqi`
3. 使用Unity 2020.3+打开

### 步骤2：运行测试
1. 创建空场景
2. 将 `GameLogicTester.cs` 添加到空对象
3. 点击Play，查看Console输出

**预期输出**：
```
=== Starting Junqi Game Logic Tests ===
--- Testing BoardPosition ---
Created position: a1
Parsed from string: b12
...
=== All Tests Completed ===
```

### 步骤3：开始游戏
1. 创建空对象
2. 添加 `JunqiGameManager.cs` 组件
3. 配置参数（游戏模式、AI难度等）
4. 添加 `GameUIManager.cs` 组件
5. 关联UI元素引用
6. 运行场景

### 代码示例

#### 示例1：最简单的游戏
```csharp
using JunqiGame.Core;

void Start()
{
    // 创建游戏
    var game = new GameState();
    game.AddPlayer(new PlayerInfo(PlayerColor.Blue, "Player1", "p1"));
    game.AddPlayer(new PlayerInfo(PlayerColor.Red, "Player2", "p2"));
    
    // AI布阵
    game.InitializeAILayout(PlayerColor.Blue, AIDifficulty.Medium);
    game.InitializeAILayout(PlayerColor.Red, AIDifficulty.Medium);
    
    // 开始游戏
    game.FinishSetup(game.Players[PlayerColor.Blue]);
    game.FinishSetup(game.Players[PlayerColor.Red]);
    
    // 获取并执行移动
    var moves = game.GetValidMoves(PlayerColor.Blue);
    if (moves.Count > 0)
        game.Move(moves[0], PlayerColor.Blue);
}
```

#### 示例2：RTS模式
```csharp
// 在JunqiGameManager Inspector中启用RTS模式
// RTSConfig.RTSModeEnabled = true

// RTS模式会自动处理：
// - 行动点恢复（每0.5秒+0.1 AP）
// - 并发行动调度
// - 动作队列管理
```

---

## 🎮 游戏模式

### 1. 传统回合制模式
- **特点**：玩家轮流行动，每次一方移动一个棋子
- **适用**：经典军棋体验，适合新手
- **配置**：`JunqiGameManager.GameMode = PlayMode.Concealed`

### 2. RTS实时对战模式（新增）
- **特点**：双方同时行动，基于行动点(AP)系统
- **机制**：
  - 初始AP：根据配置（默认1.0）
  - 恢复速度：每0.5秒恢复0.1 AP
  - 移动消耗：每次移动消耗1.0 AP
  - 并发操作：无需等待对手
- **适用**：快节奏对战，竞技性强
- **配置**：`RTSConfig.RTSModeEnabled = true`

### 3. AI难度级别

| 难度 | 说明 | 行为特点 |
|------|------|---------|
| **Easy** | 简单 | 随机选择合法移动，偶尔犯错误 |
| **Medium** | 中等 | 基础策略 + 80%概率执行最优解 |
| **Hard** | 困难 | 严格策略，优先攻击和保护 |
| **Cheating** | 作弊 | 完全信息模式，知道所有棋子位置 |

---

## 🏗️ 系统架构

### 分层架构

```
┌─────────────────────────────────────┐
│       Presentation Layer            │  ← UI系统（GameUIManager等）
├─────────────────────────────────────┤
│       Application Layer             │  ← 游戏控制器（JunqiGameManager）
├─────────────────────────────────────┤
│       Domain Layer                  │  ← 核心逻辑（Board, GameRules等）
├─────────────────────────────────────┤
│       Infrastructure Layer          │  ← RTS系统、AI系统、数据层
└─────────────────────────────────────┘
```

### 核心类关系

```
JunqiGameManager (MonoBehaviour)
    ↓ 使用
GameState (游戏状态)
    ├─ Board (棋盘)
    │   ├─ BoardPosition (位置)
    │   └─ Piece (棋子)
    ├─ GameRules (规则引擎)
    │   ├─ ParseMove (解析移动)
    │   ├─ IsValidMove (验证移动)
    │   └─ ResolveCombat (战斗判定)
    └─ PathFinder (路径查找)

RTSController (RTS调度器)
    ├─ RTSState (RTS状态)
    ├─ CombatEngine (战斗引擎)
    └─ AIBehaviorTree (AI行为树)

GameUIManager (UI管理器)
    ├─ BoardRenderer (棋盘渲染)
    ├─ AnimationManager (动画管理)
    └─ AudioManager (音频管理)
```

### 数据流

#### 传统模式流程
```
用户点击棋子 → PieceSelectionManager
    ↓
选择目标位置 → GameUIManager
    ↓
验证移动 → GameRules.IsValidMove()
    ↓
执行移动 → GameState.Move()
    ↓
战斗判定 → GameRules.ResolveCombat()
    ↓
切换玩家 → GameState.SwitchPlayer()
    ↓
更新UI → GameUIManager.Refresh()
```

#### RTS模式流程
```
用户点击移动 → RTSController.EnqueueAction()
    ↓
检查AP → RTSController.ConsumeAP()
    ↓
加入队列 → actionQueue
    ↓
调度执行 → RTSController.ProcessQueue()
    ↓
播放动画 → AnimationManager.PlayMove()
    ↓
战斗结算 → CombatEngine.Resolve()
    ↓
恢复AP → RTSController.RegenAP()
```

---

## 📚 API文档

### 核心类API

#### BoardPosition（棋盘位置）
```csharp
// 创建位置
var pos = new BoardPosition('a', 1);
var pos = BoardPosition.FromString("b12");

// 属性
pos.Column  // 'a' - 'e'
pos.Row     // 1 - 13
pos.ToString()  // "a1"

// 方法
pos.IsValid()           // 检查是否有效
pos.GetAdjacentPositions()  // 获取相邻位置
```

#### Piece（棋子）
```csharp
// 创建棋子
var piece = new Piece(PlayerColor.Blue, PieceRank.Marshal);

// RTS属性
piece.Health      // 当前生命值
piece.Attack      // 攻击力
piece.MaxHealth   // 最大生命值

// 传统属性
piece.Color       // 颜色
piece.Rank        // 等级
piece.CanMove()   // 是否可以移动
piece.IsBomb()    // 是否是炸弹
piece.IsMine()    // 是否是地雷
piece.IsFlag()    // 是否是军旗
piece.IsSapper()  // 是否是工兵
```

#### Board（棋盘）
```csharp
var board = new Board();

// 放置棋子
board.PlacePiece(position, piece);

// 获取棋子
var piece = board.GetPiece(position);

// 移动棋子
var captured = board.MovePiece(from, to);

// 合并布局（用于初始化）
board.Merge(pieceDictionary);

// 特殊位置检测
Board.IsCamp(position);      // 是否是行营
Board.IsRailway(position);   // 是否是铁路
Board.GetCellType(position); // 获取格子类型
```

#### GameRules（游戏规则）
```csharp
// 解析移动字符串
var move = GameRules.ParseMove("b2-b3");    // 普通移动
var move = GameRules.ParseMove("b2xa3");    // 吃子移动

// 验证移动
bool valid = GameRules.IsValidMove(from, to, board, playerColor);

// 执行移动
var result = GameRules.ExecuteMove(board, from, to, playerColor);

// 战斗判定
var combatResult = GameRules.ResolveCombat(attacker, defender);

// 检查游戏结束
var endResult = GameRules.CheckGameEnd(board);
```

#### GameState（游戏状态）
```csharp
var gameState = new GameState();

// 设置
gameState.SetPlayMode(PlayMode.Concealed);
gameState.AddPlayer(playerInfo);

// 布阵
gameState.InitializeAILayout(color, difficulty);
gameState.FinishSetup(playerInfo);

// 游戏进行
var validMoves = gameState.GetValidMoves(color);
var result = gameState.Move("b2-b3", PlayerColor.Blue);
var endResult = gameState.Forfeit(PlayerColor.Blue);

// 事件
gameState.OnStateChange += (state, type) => { };
gameState.OnMoveExecuted += (result) => { };
gameState.OnGameEnded += (endResult) => { };
```

#### RTSController（RTS控制器）
```csharp
// 获取单例
var rts = RTSController.Instance;

// 启用RTS模式
rts.EnableRTSMode(true);

// 消费行动点
bool success = rts.ConsumeAP(PlayerColor.Blue, 1.0f);

// 入队动作
rts.EnqueueAction(new RTSMoveAction(...));

// 重置状态
rts.ResetState();
```

#### AILayoutGenerator（AI布阵）
```csharp
// 生成布阵
var layout = AILayoutGenerator.GenerateLayout(
    PlayerColor.Red, 
    AIDifficulty.Medium
);

// 返回 Dictionary<string, Piece>
// key: 位置字符串（如"a1"）
// value: 棋子对象
```

---

## 🔧 扩展开发

### 1. 添加新的棋子类型

```csharp
// 1. 在Enums.cs中添加新等级
public enum PieceRank
{
    // ... 现有等级
    NewPiece = 12  // 新棋子
}

// 2. 在Piece.cs中添加默认属性
private static (int health, int attack) GetDefaultStats(PieceRank rank)
{
    switch (rank)
    {
        case PieceRank.NewPiece: return (100, 50);
        // ...
    }
}

// 3. 在GameRules.cs中添加特殊规则
```

### 2. 自定义AI行为

```csharp
// 创建新的行为节点
public class CustomAttackAction : BTAction
{
    public override BTStatus Execute(AIContext context)
    {
        // 自定义攻击逻辑
        var target = FindBestTarget(context);
        if (target != null)
        {
            context.SelectedAction = CreateMoveAction(target);
            return BTStatus.Success;
        }
        return BTStatus.Failure;
    }
}

// 集成到行为树
private BTNode BuildTree(AIDifficulty difficulty)
{
    var selector = new BTSelector();
    selector.AddChild(new CustomAttackAction());
    // ...
    return selector;
}
```

### 3. 添加网络对战

```csharp
// 使用Photon或Mirror
public class NetworkGameConnection : MonoBehaviour
{
    private void SendMove(string moveString)
    {
        // 通过网络发送移动
        photonView.RPC("ReceiveMove", RpcTarget.Others, moveString);
    }
    
    [PunRPC]
    private void ReceiveMove(string moveString)
    {
        var manager = JunqiGameManager.Instance;
        manager.MakeMove(moveString);
    }
}
```

### 4. 自定义UI主题

```csharp
// 修改GameUIManager中的颜色和样式
public class CustomTheme : MonoBehaviour
{
    public Color blueTeamColor = Color.blue;
    public Color redTeamColor = Color.red;
    public Sprite customPieceSprite;
    
    void ApplyTheme()
    {
        var uiManager = FindObjectOfType<GameUIManager>();
        uiManager.SetTeamColors(blueTeamColor, redTeamColor);
    }
}
```

---

## ❓ 常见问题

### Q1: 编译错误？
**A**: 确保：
- 所有文件都在 `Assets/Scripts` 目录下
- Unity版本 ≥ 2020.3 LTS
- 命名空间正确（`JunqiGame.Core` 等）

### Q2: 看不到棋子？
**A**: 检查：
- `GameUIManager.piecePrefab` 是否正确关联
- `BoardRenderer` 是否正常初始化
- Resources文件夹中有无配置数据

### Q3: AI不动？
**A**: 确认：
- RTS模式下检查AP是否充足
- 传统模式下检查是否轮到AI回合
- Console中查看是否有错误日志

### Q4: 如何保存游戏？
**A**: 使用：
```csharp
// 克隆状态
var savedState = gameState.Clone();

// 或使用Unity序列化
PlayerPrefs.SetString("GameState", JsonUtility.ToJson(gameState));
```

### Q5: 性能优化？
**A**: 建议：
- 启用对象池（已实现在GameUIManager）
- 减少Debug.Log输出（发布时禁用）
- 使用Profiler分析瓶颈

### Q6: 如何调整平衡性？
**A**: 修改：
- `Resources/RTS/Data/HealthAttackConfig.asset` - 棋子HP/ATK
- `RTSConfigSO` - AP恢复速度、最大值
- `AILayoutGenerator` - AI策略权重

---

## 📊 性能指标

| 操作 | 时间复杂度 | 说明 |
|------|-----------|------|
| 获取棋子 | O(1) | 扁平数组查找 |
| 放置棋子 | O(1) | 数组插入 |
| 移动棋子 | O(1) | 数组操作 |
| 验证移动 | O(1) | 常数检查 |
| 战斗判定 | O(1) | 简单比较 |
| 获取合法移动 | O(n) | n≤25个棋子 |
| AI布阵生成 | O(n) | n=25个棋子 |
| 路径查找 | O(V+E) | A*算法 |
| 状态克隆 | O(n) | 深拷贝棋盘 |

**内存占用**：
- 单个游戏状态：~5KB
- 棋盘状态：~2KB
- UI对象池：~50KB
- RTS运行时：~100KB

---

## 📝 开发规范

### 代码风格
- 使用camelCase命名局部变量
- 使用PascalCase命名类和方法
- 所有公共API添加XML注释
- 遵循单一职责原则

### 提交规范
```
feat: 添加新功能
fix: 修复bug
docs: 更新文档
style: 代码格式调整
refactor: 重构代码
test: 添加测试
chore: 构建/工具链变更
```

---

## 🤝 贡献指南

欢迎提交Issue和Pull Request！

### 贡献步骤
1. Fork本项目
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'feat: add some feature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启Pull Request

### 代码审查要点
- ✅ 功能完整性
- ✅ 代码规范性
- ✅ 性能影响
- ✅ 向后兼容性
- ✅ 文档更新

---

## 📄 许可证

本项目基于原始JavaScript版本移植，保持相同的开源许可。

---

## 🙏 致谢

- 原始JavaScript军棋游戏作者
- Unity社区提供的优秀资源
- 所有贡献者

---

## 📞 联系方式

如有问题或建议，请：
- 提交GitHub Issue
- 发送邮件至：[your-email@example.com]
- 加入Discord社区：[链接]

---

**祝游戏愉快！** 🎮⚔️

*最后更新：2026年6月*
