# Effect Image使用指南

## 概述

Effect系统现在完全支持Image（图标）显示！你在Unity编辑器中为Effect赋予的Image会自动在EffectSlot中显示。

## 系统架构

```
Effect (ScriptableObject)
├── Image (Sprite) ← Unity编辑器中赋值
└── Apply() → 创建Buff时传递Image

Buff (运行时实例)
├── Image (Sprite) ← 从Effect接收
└── 其他属性...

EffectSlot (UI组件)
└── 直接显示 buffData.Image
```

## 快速开始

### 第1步：在Unity编辑器中为Effect赋予图标

1. 在Project面板中选择你的Effect资产（例如：`Weaken.asset`）
2. 在Inspector中找到 **Image** 字段
3. 拖入你想要的Sprite图标

### 第2步：修改Effect的Apply方法

在创建Buff时，将`this.Image`作为参数传递：

```csharp
public override void Apply(IBattleUnit caster, IBattleUnit target)
{
    // ... 你的逻辑 ...

    // 创建Buff时传递Image（最后一个参数）
    var buff = new YourBuff(target, duration, otherParams, this.Image);
    CurrentBattle.AddBuff(buff);
}
```

### 第3步：修改Buff的构造函数

在Buff的构造函数中添加`icon`参数并传递给基类：

```csharp
public YourBuff(IBattleUnit owner, int duration, OtherParams params, Sprite icon = null)
    : base(owner, duration, icon)  // 传递icon给基类
{
    // 你的初始化代码...
}
```

完成！EffectSlot会自动显示你设置的图标。

## 完整示例

### 示例1：Weaken Effect（已完成）

**Weaken.cs** - Effect ScriptableObject:
```csharp
[CreateAssetMenu(menuName = "Data/Effects/Debuff/Weaken")]
public class Weaken : Effect
{
    [SerializeField] private int duration = 3;
    [SerializeField] private float damageReduction = 0.2f;
    [SerializeField] private float defenseReduction = 0.2f;

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        // 创建Buff时传递 this.Image
        var weakenBuff = new WeakenBuff(
            target,
            duration,
            damageReduction,
            defenseReduction,
            this.Image  // ← 传递图标
        );

        CurrentBattle.AddBuff(weakenBuff);
    }
}
```

**WeakenBuff.cs** - Buff运行时类:
```csharp
public class WeakenBuff : Buff
{
    public override string DisplayName => "虚弱";
    public override string Description => $"造成伤害降低{damageReduction * 100}%";

    private float damageReduction;
    private float defenseReduction;

    // 构造函数添加 icon 参数
    public WeakenBuff(
        IBattleUnit owner,
        int duration,
        float damageReduction,
        float defenseReduction,
        Sprite icon = null  // ← 添加icon参数（可选）
    )
        : base(owner, duration, icon)  // ← 传递给基类
    {
        this.damageReduction = Mathf.Clamp01(damageReduction);
        this.defenseReduction = Mathf.Clamp01(defenseReduction);
    }

    // 其他方法...
}
```

### 示例2：Poison Effect（建议修改）

**修改前** - Poison.cs:
```csharp
public override void Apply(IBattleUnit caster, IBattleUnit target)
{
    // 创建Buff但没有传递Image
    var debuff = new PoisonDebuff(target, duration, damage);
    CurrentBattle.AddBuff(debuff);
}
```

**修改后** - Poison.cs:
```csharp
public override void Apply(IBattleUnit caster, IBattleUnit target)
{
    // 创建Buff时传递Image
    var debuff = new PoisonDebuff(target, duration, damage, this.Image);
    CurrentBattle.AddBuff(debuff);
}
```

**修改前** - PoisonDebuff.cs:
```csharp
public PoisonDebuff(IBattleUnit owner, int duration, int damage)
    : base(owner, duration)
{
    this.damage = damage;
}
```

**修改后** - PoisonDebuff.cs:
```csharp
public PoisonDebuff(IBattleUnit owner, int duration, int damage, Sprite icon = null)
    : base(owner, duration, icon)  // 传递icon
{
    this.damage = damage;
}
```

### 示例3：永久Buff（羁绊系统）

对于永久Buff（如射手羁绊），可以在创建时传递null或预设图标：

```csharp
// 在Synergy的Apply方法中
public override void Apply(SynergyModel model)
{
    // 可以传递null（不显示图标）
    var archerBuff = new ArcherBuff(model.Owner, attackBonus, null);

    // 或者加载预设的羁绊图标
    // Sprite synergyIcon = Resources.Load<Sprite>("Icons/Synergy_Archer");
    // var archerBuff = new ArcherBuff(model.Owner, attackBonus, synergyIcon);

    GetBattleModel().AddBuff(archerBuff);
}
```

## 修改现有Effect的批量步骤

如果你有很多现有的Effect需要支持Image，按照以下步骤批量修改：

### 方案1：逐个修改（推荐）

1. 找到所有Effect ScriptableObject类（在`Assets/Scripts/Data/Effects/`）
2. 找到所有Buff类（在`Assets/Scripts/Models/BuffModel/`）
3. 对每对Effect-Buff：
   - 修改Effect的`Apply`方法，传递`this.Image`
   - 修改Buff的构造函数，添加`icon`参数并传给基类

### 方案2：使用SetImage方法（备选）

如果不想修改构造函数，可以在创建Buff后调用`SetImage`：

```csharp
public override void Apply(IBattleUnit caster, IBattleUnit target)
{
    var buff = new YourBuff(target, duration, otherParams);
    buff.SetImage(this.Image);  // 创建后设置Image
    CurrentBattle.AddBuff(buff);
}
```

但这种方式不如直接在构造函数中传递优雅。

## 为Effect创建图标资源

### 推荐图标规格

- **尺寸**: 64x64 或 128x128 像素
- **格式**: PNG（支持透明背景）
- **风格**: 统一的美术风格
- **命名**: 与Effect名称对应（如：`Icon_Weaken.png`）

### 图标组织结构

```
Assets/
└── Art/
    └── Icons/
        ├── Buffs/          # 增益效果图标
        │   ├── Icon_Archer.png
        │   ├── Icon_Scholar.png
        │   └── Icon_Strengthen.png
        ├── Debuffs/        # 减益效果图标
        │   ├── Icon_Weaken.png
        │   ├── Icon_Poison.png
        │   └── Icon_Burn.png
        └── ControlDebuffs/ # 控制效果图标
            ├── Icon_Frozen.png
            ├── Icon_Sleep.png
            └── Icon_Confusion.png
```

### 在Unity中导入图标

1. 将图标文件放入上述目录结构
2. 选择图标文件，在Inspector中设置：
   - **Texture Type**: Sprite (2D and UI)
   - **Pixels Per Unit**: 100
   - **Filter Mode**: Bilinear
   - **Max Size**: 128 或 256

## EffectSlot显示逻辑

EffectSlot会自动处理图标显示：

```csharp
// 在EffectSlot.UpdateDisplay()中
if (effectIcon != null)
{
    if (buffData.Image != null)
    {
        // 有图标：显示图标
        effectIcon.sprite = buffData.Image;
        effectIcon.enabled = true;
        effectIcon.color = Color.white;
    }
    else
    {
        // 没有图标：隐藏Icon组件
        effectIcon.enabled = false;
    }
}
```

**特性**:
- ✅ 有图标时自动显示
- ✅ 没有图标时自动隐藏Icon组件
- ✅ 背景颜色仍然正常显示（区分Buff/Debuff）
- ✅ Duration数字仍然正常显示

## 常见问题

### Q1: 图标不显示怎么办？

**检查清单**:
1. ✓ 在Unity编辑器中为Effect资产设置了Image吗？
2. ✓ Effect的Apply方法传递了`this.Image`吗？
3. ✓ Buff的构造函数接收并传递icon参数了吗？
4. ✓ EffectSlot预制体中有Icon（Image）组件吗？

### Q2: 旧的Effect还能正常工作吗？

**答**: 能！因为构造函数中的`icon`参数是**可选参数**（默认为null）。

未修改的旧Effect会继续正常工作，只是不显示图标而已。

### Q3: 如何批量为所有Effect设置图标？

1. 准备好所有图标文件
2. 在Unity中选中所有Effect资产
3. 使用自定义Editor脚本批量赋值（可选）
4. 或手动为每个Effect在Inspector中拖入对应图标

### Q4: 能否动态改变Buff的图标？

**答**: 可以！使用`buff.SetImage(newSprite)`方法。

```csharp
// 在某个Buff的特殊逻辑中
public override void OnTurnStart()
{
    // 根据状态改变图标
    if (某个条件)
    {
        SetImage(alternativeIcon);
    }
}
```

### Q5: 永久Buff需要设置图标吗？

**建议**:
- ✅ **羁绊Buff**: 设置统一的羁绊图标
- ✅ **状态Buff**: 设置特定的状态图标
- ⚠️ **临时技术Buff**: 可以不设置图标

## 测试清单

创建Effect后请测试：

- [ ] 在Unity编辑器中为Effect设置了Image
- [ ] Effect的Apply方法正确传递了this.Image
- [ ] Buff的构造函数接收并传递icon参数
- [ ] 在战斗中施放技能后，EffectSlot正确显示图标
- [ ] 图标清晰可见，大小合适
- [ ] 背景颜色正确（Buff绿色/Debuff红色）
- [ ] Duration数字正确显示
- [ ] 没有图标的Effect仍能正常工作（不显示Icon组件）

## 进阶：图标特效

可以为EffectSlot添加更多视觉效果：

### 添加图标动画

```csharp
// 在EffectSlot.cs中
public void SetEffect(Buff buff)
{
    buffData = buff;
    UpdateDisplay();

    // 添加淡入动画
    if (effectIcon != null && effectIcon.enabled)
    {
        StartCoroutine(FadeInIcon());
    }
}

private IEnumerator FadeInIcon()
{
    Color color = effectIcon.color;
    color.a = 0f;
    effectIcon.color = color;

    float duration = 0.3f;
    float elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        color.a = Mathf.Lerp(0f, 1f, elapsed / duration);
        effectIcon.color = color;
        yield return null;
    }

    color.a = 1f;
    effectIcon.color = color;
}
```

### 添加图标边框

在EffectSlot预制体中添加一个边框Image：

```
EffectSlot
├── Background (Image)
├── Icon (Image) ← 图标
├── Border (Image) ← 新增边框
└── DurationText (TextMeshPro)
```

### 添加稀有度标识

根据Effect的稀有度改变边框颜色：

```csharp
// 在Effect.cs中添加
public enum EffectRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

[SerializeField] private EffectRarity rarity = EffectRarity.Common;
public EffectRarity Rarity => rarity;
```

然后在Buff中传递稀有度，EffectSlot根据稀有度改变边框颜色。

## 总结

Effect Image系统的关键点：

1. ✅ **Effect基类**已有Image属性
2. ✅ **Buff基类**已添加Image属性和SetImage方法
3. ✅ **EffectSlot**已自动显示buffData.Image
4. ✅ 使用**可选参数**，不破坏现有代码
5. ✅ 支持**动态修改**图标

只需要：
- 在Unity编辑器中为Effect赋予Image
- 修改Effect的Apply方法传递Image
- 修改Buff的构造函数接收Image

就能让所有Effect都显示精美的图标！
