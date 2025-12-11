# Effect展示系统使用指南

## 概述

Effect展示系统用于在战斗界面显示玩家和敌人身上的所有Buff/Debuff效果，参考了SpiritSlot和SpiritSwitcher的架构设计。

## 文件说明

### 1. EffectSlot.cs
**位置**: `Assets/Scripts/UI/EffectSlot.cs`

**功能**: 单个Effect槽位组件，负责显示一个Buff/Debuff的信息

**显示内容**:
- 效果名称 (DisplayName)
- 持续回合数 (RemainingTurns，-1显示为∞)
- 效果描述 (Description)
- 背景颜色区分效果类型

**颜色系统**:
- 🟢 **绿色** - 增益Buff (ArcherBuff, ScholarBuff等)
- 🔴 **红色** - 减益Debuff (PoisonDebuff, BurnDebuff, WeakenBuff等)
- 🟣 **紫色** - 控制型Debuff (FrozenDebuff, SleepDebuff, ConfusionDebuff)
- 🔵 **蓝色** - 永久效果 (RemainingTurns = -1)

### 2. UI_EffectDisplay.cs
**位置**: `Assets/Scripts/UI/UI_EffectDisplay.cs`

**功能**: Effect展示面板管理器，负责管理所有Effect槽位

**特性**:
- 分别显示玩家和敌人的Effect
- 对象池模式（预创建槽位，避免运行时频繁创建销毁）
- 自动刷新Effect显示
- 支持隐藏空面板选项

## Unity编辑器配置

### 第一步：创建Effect槽位预制体（可选）

如果不提供预制体，系统会自动创建默认槽位。建议创建自定义预制体以获得更好的视觉效果。

1. 创建一个新的GameObject，命名为 `EffectSlot`
2. 添加以下组件结构：

```
EffectSlot (Image + EffectSlot.cs)
├── Icon (Image) - 效果图标
├── NameText (TextMeshPro - Text) - 效果名称
├── DurationText (TextMeshPro - Text) - 持续回合数
└── DescriptionText (TextMeshPro - Text) - 效果描述
```

3. 配置EffectSlot组件：
   - **Effect Icon**: 拖入Icon的Image组件
   - **Background**: 拖入根对象的Image组件
   - **Name Text**: 拖入NameText的TMP_Text组件
   - **Duration Text**: 拖入DurationText的TMP_Text组件
   - **Description Text**: 拖入DescriptionText的TMP_Text组件

4. 自定义颜色配置（可选）：
   - **Buff Color**: 增益效果颜色（默认绿色）
   - **Debuff Color**: 减益效果颜色（默认红色）
   - **Control Debuff Color**: 控制效果颜色（默认紫色）
   - **Permanent Color**: 永久效果颜色（默认蓝色）

5. 保存为预制体：拖入Project面板 `Assets/Prefabs/UI/`

### 第二步：在战斗UI中添加Effect展示面板

1. 在你的战斗场景Canvas中创建以下结构：

```
BattleCanvas
├── UI_BattleView (你现有的战斗UI)
└── UI_EffectDisplay (新增)
    ├── PlayerEffectsPanel (Panel/GameObject)
    │   └── PlayerEffectsContainer (Horizontal/Vertical Layout Group)
    └── EnemyEffectsPanel (Panel/GameObject)
        └── EnemyEffectsContainer (Horizontal/Vertical Layout Group)
```

2. 在 `UI_EffectDisplay` GameObject上添加 `UI_EffectDisplay.cs` 组件

3. 配置 `UI_EffectDisplay` 组件：
   - **Effect Slot Prefab**: 拖入你创建的EffectSlot预制体（可选）
   - **Player Effects Container**: 拖入PlayerEffectsContainer的Transform
   - **Player Effects Panel**: 拖入PlayerEffectsPanel的GameObject
   - **Enemy Effects Container**: 拖入EnemyEffectsContainer的Transform
   - **Enemy Effects Panel**: 拖入EnemyEffectsPanel的GameObject
   - **Hide When Empty**: 勾选以在没有Effect时自动隐藏面板
   - **Max Effects Per Unit**: 设置每个单位最多显示的Effect数量（默认10）

4. 为Container添加Layout Group组件：
   - 添加 `Horizontal Layout Group` 或 `Vertical Layout Group`
   - 设置Spacing、Padding等参数
   - 勾选 `Child Control Size` 和 `Child Force Expand`

### 第三步：在BattleController中集成

找到 `BattleController.cs` 文件，添加对UI_EffectDisplay的引用和绑定：

```csharp
public class BattleController : MonoBehaviour
{
    // 现有字段...

    [Header("UI References")]
    [SerializeField]
    private UI_BattleView battleView;

    [SerializeField]
    private UI_SpiritSwitcher spiritSwitcher;

    [SerializeField]
    private UI_EffectDisplay effectDisplay; // 新增

    private void Start()
    {
        // 现有初始化代码...

        // 绑定Effect显示器
        if (effectDisplay != null)
        {
            effectDisplay.Bind(this, model);
        }
    }

    // 在需要刷新Effect显示的地方调用
    private void RefreshUI()
    {
        if (battleView != null)
            battleView.Refresh();

        if (effectDisplay != null)
            effectDisplay.RefreshDisplay(); // 新增
    }
}
```

### 第四步：集成到UI刷新流程

在以下时机调用 `effectDisplay.RefreshDisplay()` 或 `effectDisplay.UpdateEffects()`：

1. **回合开始时** - 显示最新的Effect状态
2. **回合结束时** - 更新Effect持续回合数
3. **施放技能后** - 显示新添加的Effect
4. **Buff添加/移除时** - 实时更新显示

推荐在BattleController中统一管理：

```csharp
public void EndPlayerTurn()
{
    // 现有逻辑...

    // 刷新UI
    RefreshUI();
}

public void UsePlayerSkill(int skillIndex)
{
    // 现有逻辑...

    // 刷新UI
    RefreshUI();
}

private void RefreshUI()
{
    if (battleView != null)
        battleView.Refresh();

    if (effectDisplay != null)
        effectDisplay.UpdateEffects();
}
```

## 使用示例

### 基础使用

```csharp
// 在BattleController中
public class BattleController : MonoBehaviour
{
    [SerializeField] private UI_EffectDisplay effectDisplay;
    private BattleModel model;

    private void Start()
    {
        // 初始化战斗
        model = new BattleModel();
        // ... 初始化玩家和敌人

        // 绑定Effect显示器
        effectDisplay.Bind(this, model);
    }

    private void OnSkillUsed()
    {
        // 技能使用后刷新Effect显示
        effectDisplay.RefreshDisplay();
    }

    private void OnTurnEnd()
    {
        // 回合结束后刷新Effect显示
        effectDisplay.UpdateEffects();
    }
}
```

### 高级用法

```csharp
// 获取Effect数量
int playerEffectCount = effectDisplay.GetPlayerEffectCount();
int enemyEffectCount = effectDisplay.GetEnemyEffectCount();

// 手动控制面板显示
effectDisplay.ShowPlayerEffects();
effectDisplay.HideEnemyEffects();

// 解除绑定（战斗结束时）
effectDisplay.Unbind();
```

## 布局建议

### 推荐布局1：水平排列在角色下方

```
[玩家头像]
HP: ███████ 100/100
MP: ████ 50/80
Effects: [🟢 射手] [🔵 学者] [🔴 虚弱]
```

**配置**:
- PlayerEffectsContainer: Horizontal Layout Group
- Spacing: 5
- Child Alignment: Middle Left

### 推荐布局2：垂直排列在侧边栏

```
┌─ 玩家状态 ────┐
│ HP: 100/100   │
│ MP: 50/80     │
│               │
│ 效果列表:     │
│ 🟢 射手 [∞]   │
│ 🔵 学者 [∞]   │
│ 🔴 虚弱 [2]   │
└───────────────┘
```

**配置**:
- PlayerEffectsContainer: Vertical Layout Group
- Spacing: 3
- Child Alignment: Upper Left

### 推荐布局3：紧凑网格布局

```
Effects: [🟢][🔵][🔴]
         [🟣][🟢]
```

**配置**:
- PlayerEffectsContainer: Grid Layout Group
- Cell Size: 60x60
- Spacing: 5x5
- Constraint: Fixed Column Count = 3

## 性能优化

### 对象池机制

系统使用对象池模式，在初始化时预创建所有槽位对象：
- 避免运行时频繁创建销毁GameObject
- 减少GC压力
- 提高性能

### 刷新策略

建议的刷新时机（从高频到低频）：
1. ✅ **必须刷新**: 回合结束、技能使用、Buff添加/移除
2. ⚠️ **可选刷新**: 每秒轮询检查
3. ❌ **避免刷新**: Update()中每帧刷新

## 扩展功能

### 添加Tooltip提示

可以为EffectSlot添加鼠标悬停提示：

```csharp
// 在EffectSlot.cs中添加
using UnityEngine.EventSystems;

public class EffectSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buffData != null && tooltipPanel != null)
        {
            tooltipPanel.SetActive(true);
            tooltipText.text = $"{buffData.DisplayName}\n\n{buffData.Description}";
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
}
```

### 添加Effect图标系统

创建一个ScriptableObject来管理Effect图标映射：

```csharp
[CreateAssetMenu(menuName = "Data/Effect Icon Database")]
public class EffectIconDatabase : ScriptableObject
{
    [System.Serializable]
    public class EffectIconEntry
    {
        public string effectTypeName;
        public Sprite icon;
    }

    public List<EffectIconEntry> entries = new List<EffectIconEntry>();

    public Sprite GetIcon(string effectTypeName)
    {
        var entry = entries.Find(e => e.effectTypeName == effectTypeName);
        return entry?.icon;
    }
}
```

在EffectSlot中使用：

```csharp
[SerializeField] private EffectIconDatabase iconDatabase;

private void UpdateDisplay()
{
    // 现有代码...

    if (effectIcon != null && iconDatabase != null)
    {
        string typeName = buffData.GetType().Name;
        Sprite icon = iconDatabase.GetIcon(typeName);
        if (icon != null)
        {
            effectIcon.sprite = icon;
            effectIcon.enabled = true;
        }
        else
        {
            effectIcon.enabled = false;
        }
    }
}
```

## 故障排查

### 问题1: Effect不显示

**可能原因**:
- UI_EffectDisplay未正确绑定BattleController和BattleModel
- Container的Layout Group未正确配置
- Panel被其他UI遮挡

**解决方案**:
1. 检查 `effectDisplay.Bind(this, model)` 是否被调用
2. 确保Container有Layout Group组件
3. 调整Canvas的Sorting Order

### 问题2: Effect颜色显示不正确

**可能原因**:
- Buff类型判断逻辑需要更新
- 自定义颜色配置不正确

**解决方案**:
1. 检查 `GetEffectColor()` 方法中的类型判断
2. 在Inspector中调整颜色配置

### 问题3: Effect数量过多导致UI溢出

**可能原因**:
- maxEffectsPerUnit设置过大
- Container大小不足

**解决方案**:
1. 减少 `maxEffectsPerUnit` 的值
2. 为Container添加 `Content Size Fitter` 组件
3. 使用 `Scroll Rect` 实现滚动显示

## 测试清单

- [ ] Effect槽位正确显示Buff信息
- [ ] Effect槽位正确显示Debuff信息
- [ ] 颜色区分正常（Buff绿色、Debuff红色）
- [ ] 持续回合数正确显示
- [ ] 永久Effect显示∞符号
- [ ] 玩家Effect面板正常刷新
- [ ] 敌人Effect面板正常刷新
- [ ] 空面板正确隐藏（如果启用hideWhenEmpty）
- [ ] Effect添加时UI立即更新
- [ ] Effect移除时UI立即更新
- [ ] 回合结束时持续回合数正确递减

## 总结

Effect展示系统提供了一个完整的战斗效果可视化方案，关键特性：
- ✅ 参考SpiritSlot/SpiritSwitcher架构，保持代码一致性
- ✅ 对象池机制，优化性能
- ✅ 颜色区分不同类型的Effect
- ✅ 自动刷新机制
- ✅ 灵活的布局配置
- ✅ 易于扩展（Tooltip、图标系统等）

有任何问题或需要进一步优化，请随时调整配置或代码！
