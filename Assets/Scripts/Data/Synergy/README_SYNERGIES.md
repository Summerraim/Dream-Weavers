# 羁绊系统完整配置指南

本指南说明如何在Unity中配置所有羁绊，包括基础羁绊和新增羁绊。

---

## 📋 所有羁绊列表

### 基础羁绊

#### 1. 重装战士 (HeavyWarrior) - 2/4/6档位

**路径**: `Assets/Scripts/Data/Synergy/HeavyWarrior.cs`

**触发条件**:

- (2) +10% 最大生命值
- (4) +20% 最大生命值
- (6) +40% 最大生命值

**Unity配置**:

- Synergy Id: "HeavyWarrior"
- Display Name: "重装战士"
- Trigger Counts: [2, 4, 6]

#### 2. 狂战士 (Berserker) - 2/4/6档位

**路径**: `Assets/Scripts/Data/Synergy/Berserker.cs`

**触发条件**:

- (2) 基础攻击力低于敌方时积1层怒意（每层+10攻击力），高于时消耗造成10*层数伤害
- (4) 每层怒意变为+20攻击力
- (6) 怒意不消耗

**Unity配置**:

- Synergy Id: "Berserker"
- Display Name: "狂战士"
- Trigger Counts: [2, 4, 6]

---

### 新增羁绊（7个）

#### 3. 处决者 (Executioner) - 4/6档位

**路径**: `Assets/Scripts/Data/Synergy/Executioner.cs`

**触发条件**:

- (4) 敌方生命值低于10%时，造成一次登场宠物攻击力的额外攻击
- (6) 触发阈值提升至20%

**Unity配置**:

- Synergy Id: "Executioner"
- Display Name: "处决者"
- **Trigger Counts: [4, 6]** （删除默认的2）
- Tier Four Threshold: 0.1
- Tier Six Threshold: 0.2

#### 4. 祭品 (Sacrifice) - 3档位

**路径**: `Assets/Scripts/Data/Synergy/Sacrifice.cs`

**触发条件**:

- (3) 此羁绊宠物阵亡后，随机为其余非祭品队友施加以下一种效果：
  - A. 10%生命偷取
  - B. 20%最大生命值
  - C. 20%攻击力

**Unity配置**:

- Synergy Id: "Sacrifice" （重要！代码中硬编码检查）
- Display Name: "祭品"
- **Trigger Counts: [3]**

#### 5. 角斗士 (Gladiator) - 3档位

**路径**: `Assets/Scripts/Data/Synergy/Gladiator.cs`

**触发条件**:

- (3) 同一宠物连续登场6回合，触发"角斗"：
  - 每次玩家回合开始时，随机扣除敌方最大生命值10*X%（X∈1-6）
  - 无视无敌/免疫

**Unity配置**:

- Synergy Id: "Gladiator"
- Display Name: "角斗士"
- **Trigger Counts: [3]**

#### 6. 燃法者 (ManaBurner) - 2/4档位

**路径**: `Assets/Scripts/Data/Synergy/ManaBurner.cs`

**触发条件**:

- (2) 造成伤害时额外扣除对方该次伤害10%的最大法力值
- (4) 扣除比例提升至20%

**Unity配置**:

- Synergy Id: "ManaBurner"
- Display Name: "燃法者"
- Trigger Counts: [2, 4] 或 [2, 4, 6]
- Tier Two Burn Percent: 0.1
- Tier Four Burn Percent: 0.2

#### 7. 射手 (Archer) - 2/4档位

**路径**: `Assets/Scripts/Data/Synergy/Archer.cs`

**触发条件**:

- (2) +15%攻击力
- (4) +30%攻击力

**Unity配置**:

- Synergy Id: "Archer"
- Display Name: "射手"
- Trigger Counts: [2, 4] 或 [2, 4, 6]
- Tier Two Bonus: 0.15
- Tier Four Bonus: 0.3

#### 8. 学者 (Scholar) - 2/4档位

**路径**: `Assets/Scripts/Data/Synergy/Scholar.cs`

**触发条件**:

- (2) +20%最大法力值
- (4) +40%最大法力值

**Unity配置**:

- Synergy Id: "Scholar"
- Display Name: "学者"
- Trigger Counts: [2, 4] 或 [2, 4, 6]
- Tier Two Bonus: 0.2
- Tier Four Bonus: 0.4

#### 9. 疗愈者 (Healer) - 2/4档位

**路径**: `Assets/Scripts/Data/Synergy/Healer.cs`

**触发条件**:

- (2) 每次释放技能回复队伍里面随机心兽5%最大生命值
- (4) 回复10%最大生命值

**Unity配置**:

- Synergy Id: "Healer"
- Display Name: "疗愈者"
- Trigger Counts: [2, 4] 或 [2, 4, 6]
- Tier Two Heal Percent: 0.05
- Tier Four Heal Percent: 0.1

---

## 📊 羁绊机制说明

### 按触发时机分类

**战斗开始时激活**:

- 重装战士、狂战士、射手、学者、疗愈者、处决者、祭品、角斗士、燃法者

**造成伤害时触发**:

- 处决者（检查目标HP）
- 燃法者（扣除法力值）

**释放技能时触发**:

- 狂战士（怒意机制）
- 疗愈者（治疗队友）

**单位死亡时触发**:

- 祭品（为队友提供增益）

**回合开始时触发**:

- 角斗士（连续登场6回合后）

### 按效果类型分类

**属性加成类**:

- 重装战士（生命值）
- 射手（攻击力）
- 学者（法力值）
- 狂战士（动态攻击力）

**治疗类**:

- 疗愈者（技能治疗）

**伤害类**:

- 处决者（斩杀）
- 角斗士（百分比真伤）
- 狂战士（怒意爆发）

**控制/削弱类**:

- 燃法者（法力燃烧）

**增益传递类**:

- 祭品（死亡增益）

---

## 🔧 统一配置步骤

### 1. 创建羁绊资源

在Project窗口：

1. 右键 → Create → Data/Synergy → [选择羁绊]
2. 命名为对应的中文名称或ID
3. 移动到合适的文件夹

### 2. 配置通用字段

所有羁绊都需要配置：

- **Synergy Id**: 英文ID（建议与类名一致）
- **Display Name**: 中文显示名称
- **Description**: 羁绊描述
- **Image**: 羁绊图标（Sprite）
- **Trigger Counts**: 触发档位数组 ⚠️**重要**

### 3. 配置特定参数

根据每个羁绊的特定参数进行配置（见上方列表）

### 4. 添加到Spirit

1. 打开Spirit的ScriptableObject
2. 找到Synergies列表
3. 增加列表大小
4. 将羁绊资源拖入列表

---

## ⚠️ 重要注意事项

### Trigger Counts配置

**必须正确配置**，否则羁绊不会触发：

- 2/4/6档位羁绊：`[2, 4, 6]`
- 2/4档位羁绊：`[2, 4]` 或 `[2, 4, 6]`（6档不生效）
- 4/6档位羁绊：`[4, 6]`
- 3档位羁绊：`[3]`

### 特殊Synergy ID

- **祭品**必须使用ID："Sacrifice"（代码硬编码检查）

### Spirit类特性

部分羁绊需要Spirit类的特定方法：

- MaxHP加成：`SetMaxHpBonusPercent()`
- MaxMana加成：`SetMaxManaBonusPercent()`
- 攻击力加成：通过Buff的`GetDamageBonus()`

---

## 🎮 组队建议

### 前排肉盾流

- **重装战士（2-6人）** + 角斗士（3人）
- 高血量持久战

### 输出爆发流

- **射手（2-4人）** + 狂战士（2-6人） + 处决者（4-6人）
- 高攻击力 + 斩杀

### 法术流

- **学者（2-4人）** + 燃法者（2-4人） + 疗愈者（2-4人）
- 高法力支持频繁释放技能

### 祭品流

- **祭品（3人）** + 射手/学者/重装战士
- 前排祭品换后排增益

### 角斗士流

- **角斗士（3人）** + 重装战士（2-6人）
- 单核心连续登场

---

## 📁 完整文件清单

### Buff类 (Assets/Scripts/Models/BuffModel/)

- `ArcherBuff.cs` - 射手
- `AttackBuff.cs` - 攻击力加成（祭品用）
- `ExecutionerBuff.cs` - 处决者
- `GladiatorBuff.cs` - 角斗士
- `HealerBuff.cs` - 疗愈者
- `LifeStealBuff.cs` - 生命偷取（祭品用）
- `ManaBurnBuff.cs` - 燃法者
- `MaxHealthBuff.cs` - 最大生命值加成（祭品用）
- `RageBuff.cs` - 怒意（狂战士用）
- `SacrificeBuff.cs` - 祭品
- `ScholarBuff.cs` - 学者

### Synergy类 (Assets/Scripts/Data/Synergy/)

- `Archer.cs` - 射手
- `Berserker.cs` - 狂战士
- `Executioner.cs` - 处决者
- `Gladiator.cs` - 角斗士
- `Healer.cs` - 疗愈者
- `HeavyWarrior.cs` - 重装战士
- `ManaBurner.cs` - 燃法者
- `Sacrifice.cs` - 祭品
- `Scholar.cs` - 学者

### 修改的核心文件

- **Spirit.cs** - 添加了MaxMana百分比加成支持
  - `bonusMaxManaPercent` 字段
  - `MaxManaBonusPercent` 属性
  - `SetMaxManaBonusPercent()` 方法

- **BattleController.cs** - 添加了羁绊触发支持
  - `SacrificeSynergyBridge.DeployedSpirits` 设置
  - `TriggerHealerSynergy()` 方法

---

## 🧪 测试清单

### 基础测试

- [ ] 所有羁绊ScriptableObject创建成功
- [ ] Trigger Counts正确配置
- [ ] 图标和名称正确显示

### 功能测试

**重装战士**:

- [ ] 2/4/6档位正确增加MaxHP
- [ ] HP条显示正确

**狂战士**:

- [ ] 低攻击力时积累怒意
- [ ] 高攻击力时消耗怒意
- [ ] 6档位怒意不消耗

**处决者**:

- [ ] 敌人低血时触发额外伤害
- [ ] 4档10%、6档20%阈值正确

**祭品**:

- [ ] 阵亡时为队友添加随机buff
- [ ] 不会为其他祭品添加buff

**角斗士**:

- [ ] 连续登场6回合触发角斗
- [ ] 切换Spirit重置计数
- [ ] 随机扣除敌人HP

**燃法者**:

- [ ] 造成伤害时燃烧敌人法力
- [ ] 2档10%、4档20%正确

**射手**:

- [ ] 2档+15%、4档+30%攻击力
- [ ] 伤害计算正确

**学者**:

- [ ] 2档+20%、4档+40%法力值
- [ ] 当前法力值同步增加

**疗愈者**:

- [ ] 释放技能时随机治疗队友
- [ ] 2档5%、4档10%治疗量正确

---

## 💡 高级技巧

### 多羁绊组合

可以让一个Spirit同时拥有多个羁绊，触发多重效果：

- 重装战士 + 疗愈者 = 高血量治疗辅助
- 射手 + 狂战士 = 超高输出爆发
- 学者 + 燃法者 = 法力优势控制

### 动态阵容调整

根据敌人特点选择上场Spirit：

- 高物理输出敌人 → 重装战士
- 高法力敌人 → 燃法者
- Boss战 → 角斗士（长期战）

---

现在你的羁绊系统已经完全配置好了！🎉
共计**9个羁绊**，涵盖攻击、防御、法力、治疗、控制等各种玩法！
