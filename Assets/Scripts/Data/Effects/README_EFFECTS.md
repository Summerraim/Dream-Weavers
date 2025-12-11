# Effects 系统文档

## 概述

Effect系统负责处理游戏中的所有战斗效果，包括伤害、治疗、增益、减益等。共**25种不同效果**。

## 核心架构

### Effect → Buff 机制

- **Effect**: ScriptableObject资源，定义效果配置和触发逻辑
- **Buff**: 战斗中的持久状态，由Effect创建并通过BattleModel管理
- **BattleModel**: 中央状态管理器，负责Buff的添加/移除/更新

### 应用流程

Skill.Execute() → Effect.Apply(caster, target) → 创建Buff → BattleModel.AddBuff()

---

## 伤害结算逻辑

### 基础伤害计算公式

总伤害 = 固定伤害 + (施法者攻击力 × 伤害倍率)

### 伤害结算顺序

1. **计算原始伤害** - Effect根据配置计算
2. **应用攻击者Buff** - 力量祝福(+30%)、暴击等
3. **应用目标Debuff** - 易伤(+50%)、虚弱(-20%)等
4. **护盾吸收** - ShieldBuff优先吸收伤害
5. **防御计算** - 目标Defense属性减伤
6. **最终伤害** - 调用target.ReceiveDamage()

### 特殊伤害类型

#### 处死伤害 (ExecuteStrike)

基础伤害 = 固定值 + (目标最大HP × 已损失血量% × 缩放系数)
如果已损失血量 ≥ 75%: 最终伤害 = 基础伤害 × 2.0

#### 法力燃烧 (ManaBurn)

消耗法力 = 目标当前法力 × 30%
额外伤害 = 消耗法力 × 0.5
总伤害 = 基础伤害 + 额外伤害

#### 生命偷取 (LifeSteal)

造成伤害后:
治疗量 = 实际造成伤害 × 50%

---

## Effect分类

### 1. 伤害类 (4)

| 名称 | 文件 | 核心功能 |
|-----|------|---------|
| Damage | `Damage.cs` | 基础伤害，支持固定值+攻击力缩放 |
| ExecuteStrike | `ExecuteStrike.cs` | 斩杀，低血量目标伤害翻倍 |
| LifeSteal | `LifeSteal.cs` | 生命偷取，造成伤害并恢复生命 |
| ManaBurn | `ManaBurn.cs` | 法力燃烧，消耗法力并造成额外伤害 |

### 2. 治疗类 (1)

| 名称 | 文件 | 核心功能 |
|-----|------|---------|
| Heal | `Heal.cs` | 治疗，支持固定值+最大HP百分比 |

### 3. 强化Buff (9)

| 名称 | 文件 | 效果 | 持续 |
|-----|------|-----|------|
| Strengthen | `Strengthen.cs` | 攻击力+30% | 3回合 |
| DefenseUp | `DefenseUp.cs` | 防御力+10% | 3回合 |
| CriticalStrike | `CriticalStrike.cs` | 攻击+30%，暴击率+30%，暴击伤害×2 | 3回合 |
| Shield | `Shield.cs` | 护盾吸收伤害 | 3回合 |
| ToughSkin | `ToughSkin.cs` | 减伤20% | 永久 |
| Vampiric | `Vampiric.cs` | 吸血30% | 3回合 |
| ManaShield | `ManaShield.cs` | 50%伤害转化为法力消耗 | 永久 |
| Thorn | `Thorn.cs` | 反弹20%受到的伤害 | 5回合 |
| Invincibility | `Invincibility.cs` | 完全免疫伤害 | 2回合 |

### 4. 削弱Debuff (9)

| 名称 | 文件 | 效果 | 持续 |
|-----|------|-----|------|
| Weaken | `Weaken.cs` | 攻击和防御-20% | 3回合 |
| WeakenAttack | `WeakenAttack.cs` | 攻击力-30% | 3回合 |
| WeakenDefense | `WeakenDefense.cs` | 防御力-30% | 3回合 |
| Vulnerability | `Vulnerability.cs` | 受到伤害+50% | 3回合 |
| Curse | `Curse.cs` | 最大HP-30% | 3回合 |
| HealingReduction | `HealingReduction.cs` | 治疗效果-50% | 3回合 |
| Poison | `Poison.cs` | 每回合损失50HP或最大HP的10% | 3回合 |
| Burn | `Burn.cs` | 每回合受到40火焰伤害或最大HP的8% | 3回合 |
| ManaLeech | `ManaLeech.cs` | 每回合损失20法力或最大法力的10% | 3回合 |

### 5. 控制Debuff (5)

| 名称 | 文件 | 效果 | 持续 |
|-----|------|-----|------|
| Frozen | `Frozen.cs` | 无法行动 | 2回合 |
| Sleep | `Sleep.cs` | 无法行动，受伤醒来 | 1-3回合 |
| Blind | `Blind.cs` | 50%几率技能失效 | 2回合 |
| Confusion | `Confusion.cs` | 50%几率攻击自己 | 2回合 |
| Silence | `Silence.cs` | 技能法力消耗+50% | 2回合 |

### 6. 恢复Buff (2)

| 名称 | 文件 | 效果 | 持续 |
|-----|------|-----|------|
| HealthRegeneration | `HealthRegeneration.cs` | 每回合恢复最大HP的10% | 永久 |
| ManaRegeneration | `ManaRegeneration.cs` | 每回合恢复最大法力的10% | 3回合 |

### 7. 特殊效果 (4)

| 名称 | 文件 | 核心功能 |
|-----|------|---------|
| PrepareEffect | `PrepareEffect.cs` | 蓄力N回合后触发存储的效果 |
| Cleanse | `Cleanse.cs` | 移除目标的Debuff（最多1个） |
| Dispel | `Dispel.cs` | 移除目标的Buff（最多1个） |
| Revive | `Revive.cs` | 死亡后复活并恢复30%生命（一次性） |

---

## 常用配置参数

### 伤害Effect通用参数

```csharp
[SerializeField] private int initDamage;           // 固定伤害值
[SerializeField] private float damageMultiplier;   // 攻击力缩放倍率
[SerializeField] private bool scaleWithDamage;     // 是否按攻击力缩放
```

### Buff/Debuff通用参数

```csharp
[SerializeField] private int duration;             // 持续回合数 (-1=永久)
[SerializeField] private bool applyToCaster;       // true=施法者 false=目标
```

### 百分比类效果参数

```csharp
[SerializeField] private bool usePercentDamage;    // 是否使用百分比
[SerializeField] private float percentDamage;      // 百分比值 (0-1)
```

---

## 伤害修饰系统

### Buff中的伤害修饰方法

```csharp
// Buff基类提供的伤害修饰接口
public virtual int GetDamageBonus()           // 攻击力加成
public virtual int GetDefenseBonus()          // 防御力加成
public virtual int ModifyDamageReceived(int) // 受到伤害修正
public virtual int ModifyDamageDealt(int)    // 造成伤害修正
```

### 修饰链

```
原始伤害
  ↓
+ GetDamageBonus() (StrengthBuff)
  ↓
× ModifyDamageDealt() (CriticalStrikeBuff)
  ↓
- target.Defense
  ↓
× ModifyDamageReceived() (ToughSkinBuff)
  ↓
- ShieldBuff吸收
  ↓
最终伤害
```

---

## 使用示例

### 创建一个简单攻击技能

```
1. 在Unity中创建SkillData资源
2. 添加Damage Effect
3. 配置:
   - initDamage = 100
   - damageMultiplier = 1.0
   - scaleWithDamage = true
```

**效果**: 造成 100 + (施法者攻击力 × 1.0) 点伤害

### 创建斩杀技能

```
1. 添加ExecuteStrike Effect
2. 配置:
   - initDamage = 60
   - missingHealthScaling = 0.1
   - executeThreshold = 0.75
   - executeMultiplier = 2.0
```

**效果**: 对低血量目标造成更高伤害，血量<25%时伤害翻倍

### 创建蓄力技能

```
1. 添加PrepareEffect
2. preparingEffects添加Shield (给自己护盾)
3. triggeredEffects添加Damage (蓄力完成后造成伤害)
4. 配置prepareTime = 3
```

**效果**: 准备3回合，期间获得护盾，完成后释放大招

---

## 开发指南

### 添加新Effect的步骤

1. **创建Effect脚本**

```csharp
[CreateAssetMenu(menuName = "Data/Effects/Custom/MyEffect")]
public class MyEffect : Effect
{
    public static BattleModel CurrentBattle { get; set; }

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        // 实现效果逻辑
    }
}
```

2. **创建对应Buff（如需持续效果）**

```csharp
public class MyBuff : Buff
{
    public MyBuff(IBattleUnit owner, int duration) : base(owner, duration) { }

    public override void OnTurnStart() { /* 回合开始时触发 */ }
    public override void OnTurnEnd() { /* 回合结束时触发 */ }
}
```

3. **在BattleController.InitializeBattle()中注册**

```csharp
MyEffect.CurrentBattle = model;
```

4. **在Unity中创建Effect资源**

- 右键 → Create → Data/Effects/Custom/MyEffect
- 配置参数
- 添加到技能的Effects列表

---

## 注意事项

1. **伤害必须为正值** - 负值伤害不会造成治疗
2. **护盾优先级高于减伤** - 伤害先被护盾吸收，然后才应用减伤
3. **控制效果互斥** - Frozen/Sleep同时只能有一个生效
4. **百分比叠加** - 多个百分比修饰相加而非相乘
5. **Buff过期自动移除** - duration=0的Buff会在回合结束时移除
6. **静态引用必须设置** - 每个Effect的CurrentBattle必须在战斗开始时设置

---

## 文件位置

- **Effect脚本**: `Assets/Scripts/Data/Effects/`
- **Buff脚本**: `Assets/Scripts/Models/BuffModel/`
- **Effect基类**: `Assets/Scripts/Models/EffectModel/Effect.cs`
- **Buff基类**: `Assets/Scripts/Models/BuffModel/Buff.cs`
