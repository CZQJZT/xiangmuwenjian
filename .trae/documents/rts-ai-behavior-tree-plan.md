# 军棋 RTS 模式 AI 行为树设计方案

## 一、现状分析

当前 AI 在 `RTSController.TryGenerateAIAction()` 中仅做**随机走棋**：
- 从所有合法移动中随机选一个
- 不考虑棋子价值、战术目标、AP 管理等
- 没有"思考"过程，纯粹碰运气

需要设计一套**行为树（Behavior Tree）**系统，让 AI 在 RTS 模式下具备：
1. 优先级决策（紧急防守 > 机会进攻 > 战术推进 > 随机移动）
2. 棋子价值感知（司令 > 军长 > 师长 > ...）
3. AP 意识（高 AP 时积极进攻，低 AP 时保守防守）
4. 信息不对称处理（暗棋模式下对未知棋子的推断）

---

## 二、行为树架构设计

### 2.1 核心节点类型

```
BTNode (抽象基类)
├── BTComposite (组合节点)
│   ├── BTSelector  — 选择节点：依次执行子节点，任一成功则返回成功
│   └── BTSequence  — 序列节点：依次执行子节点，任一失败则返回失败
├── BTDecorator (装饰节点)
│   ├── BTInverter     — 取反
│   ├── BTRepeater     — 重复执行
│   └── BTConditional  — 条件判断（仅当条件满足时执行子节点）
└── BTLeaf (叶子节点)
    ├── BTCondition — 条件判断节点
    └── BTAction    — 执行动作节点
```

### 2.2 AI 行为树顶层结构

```
Root (Selector)
 │
 ├── [优先级1] 紧急防守 (Sequence)
 │    ├── 条件: 己方军旗受到威胁？
 │    │   └── EnemyNearFlag: 检测敌方棋子是否在军旗2格以内
 │    └── 动作: 拦截威胁
 │        └── InterceptThreat: 移动最近的高价值棋子到军旗与威胁者之间
 │
 ├── [优先级2] 机会夺旗 (Sequence)
 │    ├── 条件: 能否直接攻击敌方军旗？
 │    │   └── CanCaptureFlag: 存在合法移动可直接吃掉敌方军旗
 │    └── 动作: 夺旗
 │        └── CaptureFlag: 执行夺旗移动
 │
 ├── [优先级3] 炸弹换大子 (Sequence)
 │    ├── 条件: 有炸弹可用？
 │    │   └── HasBomb: 己方有炸弹且可移动
 │    ├── 条件: 附近有敌方高价值目标？
 │    │   └── HighValueTargetNearby: 敌方司令/军长/师长在炸弹可达范围内
 │    └── 动作: 炸弹换子
 │        └── BombStrike: 移动炸弹到高价值目标
 │
 ├── [优先级4] 工兵排雷 (Sequence)
 │    ├── 条件: 有工兵可用？
 │    │   └── HasSapper: 己方有工兵且可移动
 │    ├── 条件: 附近有地雷？
 │    │   └── MineNearby: 检测到敌方地雷（暗棋模式下根据位置推断）
 │    └── 动作: 排雷
 │        └── ClearMine: 移动工兵去排雷
 │
 ├── [优先级5] 有利吃子 (Sequence)
 │    ├── 条件: 存在有利战斗？
 │    │   └── HasFavorableCombat: 存在己方能赢的吃子机会
 │    └── 动作: 执行吃子
 │        └── ExecuteCapture: 选择价值差最大的有利吃子
 │
 ├── [优先级6] 战术推进 (Sequence)
 │    ├── 条件: AP 充足？
 │    │   └── HasEnoughAP: 当前 AP >= APMax * 0.5
 │    ├── 条件: 有可推进棋子？
 │    │   └── HasAdvanceablePiece: 存在中高价值棋子可向敌方区域推进
 │    └── 动作: 推进
 │        └── AdvanceTowardFlag: 选择最优棋子向敌方军旗方向移动
 │
 ├── [优先级7] 侦察探索 (Sequence)
 │    ├── 条件: 有低价值棋子？
 │    │   └── HasExpendablePiece: 有排长/连长等低价值可移动棋子
 │    └── 动作: 侦察
 │        └── ScoutMove: 移动低价值棋子到未知区域试探
 │
 └── [优先级8] 随机移动 (Action)
      └── RandomMove: 兜底策略，随机选一个合法移动
```

### 2.3 各决策节点详细逻辑

#### 紧急防守 `InterceptThreat`
```
输入: board, aiColor, enemyPositionsNearFlag
1. 找到距军旗最近的敌方棋子位置 threatPos
2. 找到所有可移动到 threatPos 的己方棋子
3. 按棋子价值从低到高排序（优先用低价值棋子拦截）
4. 选择第一个合法移动返回
```

#### 机会夺旗 `CaptureFlag`
```
输入: board, aiColor, validMoves
1. 过滤 validMoves 中目标位置包含敌方军旗的移动
2. 如果存在，直接返回该移动
```

#### 炸弹换大子 `BombStrike`
```
输入: board, aiColor, validMoves
1. 找到所有炸弹的合法移动
2. 对每个炸弹移动，检查目标位置是否有敌方棋子
3. 暗棋模式：如果目标位置未知，根据位置推断是否可能是高价值棋子
4. 明棋模式：直接检查棋子等级
5. 选择可换到的最高价值敌方棋子的移动
```

#### 工兵排雷 `ClearMine`
```
输入: board, aiColor, validMoves
1. 找到所有工兵的合法移动
2. 明棋模式：直接找地雷位置，选最近的工兵去排
3. 暗棋模式：推断敌方后排不动的棋子可能是地雷，优先排后排棋子
4. 返回最优工兵移动
```

#### 有利吃子 `ExecuteCapture`
```
输入: board, aiColor, validMoves
1. 过滤所有吃子移动 (含 'x' 的移动)
2. 对每个吃子移动，用 GameRules.ResolveCombat 模拟战斗
3. 只保留己方胜利的移动 (CombatResult.AttackerWin)
4. 按"敌方棋子价值 - 己方棋子风险"排序
5. 选择价值差最大的移动
```

#### 战术推进 `AdvanceTowardFlag`
```
输入: board, aiColor, validMoves, enemyFlagEstimatedPos
1. 估算敌方军旗位置（通常在后排中间 b13/d13）
2. 找到所有非吃子的普通移动
3. 计算每个移动后棋子到敌方军旗的距离变化
4. 优先选择：中高价值棋子 + 距离减少最多 + 不暴露在危险中
5. 返回最优推进移动
```

#### 侦察探索 `ScoutMove`
```
输入: board, aiColor, validMoves
1. 找到所有低价值棋子（排长、连长、营长）的移动
2. 优先选择向敌方区域移动的
3. 优先选择铁路移动（速度快）
4. 返回最优侦察移动
```

---

## 三、文件结构

在 `Scripts/AI/` 目录下新增以下文件：

```
Scripts/AI/
├── AILayoutGenerator.cs          (已有 - 布阵生成)
├── BehaviorTree/
│   ├── BTNode.cs                 — 行为树节点基类
│   ├── BTComposite.cs           — 组合节点基类
│   ├── BTSelector.cs            — 选择节点
│   ├── BTSequence.cs            — 序列节点
│   ├── BTDecorator.cs           — 装饰节点基类
│   ├── BTInverter.cs            — 取反装饰器
│   ├── BTConditional.cs         — 条件装饰器
│   ├── BTLeaf.cs                — 叶子节点基类
│   ├── BTCondition.cs           — 条件节点基类
│   └── BTAction.cs              — 动作节点基类
├── Conditions/
│   ├── BTCond_FlagInDanger.cs   — 军旗受威胁判断
│   ├── BTCond_CanCaptureFlag.cs — 可夺旗判断
│   ├── BTCond_HasBomb.cs        — 有炸弹判断
│   ├── BTCond_HighValueNearby.cs— 高价值目标判断
│   ├── BTCond_HasSapper.cs      — 有工兵判断
│   ├── BTCond_MineNearby.cs     — 地雷附近判断
│   ├── BTCond_HasFavorableCombat.cs — 有利战斗判断
│   ├── BTCond_HasEnoughAP.cs    — AP充足判断
│   ├── BTCond_HasAdvanceablePiece.cs — 可推进棋子判断
│   └── BTCond_HasExpendablePiece.cs  — 可消耗棋子判断
├── Actions/
│   ├── BTAct_InterceptThreat.cs — 拦截威胁
│   ├── BTAct_CaptureFlag.cs     — 夺旗
│   ├── BTAct_BombStrike.cs      — 炸弹换子
│   ├── BTAct_ClearMine.cs       — 工兵排雷
│   ├── BTAct_ExecuteCapture.cs  — 有利吃子
│   ├── BTAct_AdvanceTowardFlag.cs — 战术推进
│   ├── BTAct_ScoutMove.cs       — 侦察移动
│   └── BTAct_RandomMove.cs      — 随机移动
├── AIContext.cs                  — AI 上下文数据（棋盘快照、AP、合法移动等）
└── AIBehaviorTree.cs            — 行为树组装入口 + Tick 驱动
```

---

## 四、核心类设计

### 4.1 BTNode — 节点基类

```csharp
public enum BTStatus { Success, Failure, Running }

public abstract class BTNode
{
    public string Name;
    public abstract BTStatus Execute(AIContext context);
}
```

### 4.2 AIContext — AI 决策上下文

```csharp
public class AIContext
{
    public Board Board;
    public PlayerColor AIColor;
    public float CurrentAP;
    public float APMax;
    public List<string> ValidMoves;
    public AIDifficulty Difficulty;
    public PlayMode PlayMode;
    public HashSet<string> BusyPieceKeys;

    // 缓存：避免重复计算
    private Dictionary<string, Piece> enemyPiecesCache;
    private Dictionary<string, Piece> allyPiecesCache;
    private BoardPosition? estimatedFlagPos;

    public Piece GetEnemyPiece(BoardPosition pos);
    public Piece GetAllyPiece(BoardPosition pos);
    public BoardPosition GetEstimatedEnemyFlagPos();
    public List<KeyValuePair<string, Piece>> GetEnemyPieces();
    public float GetPieceThreatScore(Piece piece);
}
```

### 4.3 AIBehaviorTree — 行为树主控

```csharp
public class AIBehaviorTree
{
    private BTNode root;

    public AIBehaviorTree(AIDifficulty difficulty)
    {
        root = BuildTree(difficulty);
    }

    public RTSMoveAction Tick(AIContext context)
    {
        BTStatus status = root.Execute(context);
        return context.SelectedAction;
    }

    private BTNode BuildTree(AIDifficulty difficulty)
    {
        // 根据难度构建不同深度的行为树
        // Easy: 只有 RandomMove
        // Medium: 完整行为树但参数宽松
        // Hard: 完整行为树 + 更精确的判断
        // Cheating: 完整信息 + 最优决策
    }
}
```

---

## 五、与现有 RTSController 的集成方案

### 5.1 修改 `RTSController.TryGenerateAIAction()`

将当前的随机选移动逻辑替换为行为树 Tick：

```
原逻辑:
  validMoves → 随机选一个 → 入队

新逻辑:
  构建 AIContext → behaviorTree.Tick(context) → 返回 RTSMoveAction → 入队
```

### 5.2 集成点

在 `RTSController` 中：
1. 新增 `private AIBehaviorTree aiBehavior;` 字段
2. 在 `InitializeState()` 或 `Awake()` 中初始化行为树
3. 修改 `TryGenerateAIAction()` 调用行为树 Tick
4. 保留 AP 消耗逻辑不变

---

## 六、难度差异化

| 难度 | 行为树结构 | 判断精度 | 信息可见性 |
|------|-----------|---------|-----------|
| Easy | 仅 RandomMove | 无 | 仅可见己方棋子 |
| Medium | 完整行为树 | 80%概率执行最优 | 推断敌方棋子（有误差） |
| Hard | 完整行为树 | 95%概率执行最优 | 推断更准确 |
| Cheating | 完整行为树 | 100%最优 | 完全信息（知道所有棋子） |

---

## 七、实施步骤

### 步骤1: 创建行为树框架
- 新建 `Scripts/AI/BehaviorTree/` 目录
- 实现 `BTNode`, `BTStatus`, `BTComposite`, `BTSelector`, `BTSequence`
- 实现 `BTDecorator`, `BTInverter`, `BTConditional`
- 实现 `BTLeaf`, `BTCondition`, `BTAction`

### 步骤2: 实现 AIContext
- 新建 `Scripts/AI/AIContext.cs`
- 实现棋盘快照、合法移动缓存、敌方棋子推断、威胁评分等

### 步骤3: 实现条件节点
- 新建 `Scripts/AI/Conditions/` 目录
- 实现全部 10 个条件节点

### 步骤4: 实现动作节点
- 新建 `Scripts/AI/Actions/` 目录
- 实现全部 8 个动作节点

### 步骤5: 实现 AIBehaviorTree 主控
- 新建 `Scripts/AI/AIBehaviorTree.cs`
- 实现 `BuildTree()` 根据难度组装行为树
- 实现 `Tick()` 驱动逻辑

### 步骤6: 集成到 RTSController
- 修改 `RTSController.TryGenerateAIAction()`
- 注入 AIBehaviorTree 替换随机逻辑
- 保持 AP 消耗和动作队列机制不变

### 步骤7: 测试验证
- 在 `Scripts/Tests/` 中新增行为树测试
- 验证各优先级决策是否正确触发
- 验证不同难度的行为差异
