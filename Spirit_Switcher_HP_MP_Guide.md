# Spirit切换系统 + Battle UI更新指南

## 📋 更新概述

这次更新包括两个主要功能：
1. **Spirit切换器**：显示所有出场Spirit的实时HP/MP状态
2. **BattleUI增强**：为HP/MP条添加文字显示

---

## 🎨 Spirit槽位UI结构（更新版）

### SpiritSlot预制体组件：
```
SpiritSlot_Prefab (Button, 120x140)
├── Icon (Image) - Spirit图标，80x80
├── NameText (TextMeshPro) - 名称，字体14
├── HPText (TextMeshPro) - "HP: 100/100"，红色，字体12
└── MPText (TextMeshPro) - "MP: 50/50"，蓝色，字体12
```

### 状态显示：
- **正常Spirit**: 白色图标 + 当前HP/MP
- **选中Spirit**: 黄色背景高亮
- **死亡Spirit**: 灰色图标 + HP显示0/最大值，不可点击
- **空槽位**: 隐藏图标，显示"Empty"

### SpiritSlot组件字段：
- `spiritIcon` (Image) - Spirit图标
- `background` (Image) - 背景（用于高亮）
- `nameText` (TMP_Text) - 名称文本
- `hpText` (TMP_Text) - HP文本
- `mpText` (TMP_Text) - MP文本

---

## 🎯 Battle UI增强

### UI_BattleView新增字段：
```csharp
[SerializeField] private TMP_Text spiritHpText;  // 玩家HP文字
[SerializeField] private TMP_Text spiritMpText;  // 玩家MP文字
[SerializeField] private TMP_Text enemyHpText;   // 敌人HP文字
[SerializeField] private TMP_Text enemyMpText;   // 敌人MP文字
```

### 显示格式：
- HP文本: `"100/150"` (当前值/最大值)
- MP文本: `"50/100"` (当前值/最大值)

---

## 🛠️ Unity设置步骤

### 步骤1：创建Spirit槽位预制体

1. **创建Button**
   - Hierarchy → UI → Button
   - 命名为 "SpiritSlot_Prefab"
   - 大小: 120x140

2. **添加Icon**
   - 右键Button → UI → Image
   - 命名为 "Icon"
   - 大小: 80x80
   - 位置: 顶部居中 (Anchor: Top, Offset Y: -45)
   - Preserve Aspect: ✓

3. **添加NameText**
   - 右键Button → UI → Text - TextMeshPro
   - 命名为 "NameText"
   - 位置: Icon下方 (Offset Y: -95)
   - 字体大小: 14
   - Alignment: 居中
   - Width: 110, Height: 20

4. **添加HPText**
   - 右键Button → UI → Text - TextMeshPro
   - 命名为 "HPText"
   - 位置: NameText下方 (Offset Y: -110)
   - 字体大小: 12
   - Alignment: 居中
   - Color: 红色 (#FF4444)
   - Width: 110, Height: 18

5. **添加MPText**
   - 右键Button → UI → Text - TextMeshPro
   - 命名为 "MPText"
   - 位置: HPText下方 (Offset Y: -125)
   - 字体大小: 12
   - Alignment: 居中
   - Color: 蓝色 (#4444FF)
   - Width: 110, Height: 18

6. **添加SpiritSlot组件**
   - 选中Button
   - Add Component → SpiritSlot
   - 连接引用：
     - Spirit Icon → Icon
     - Background → Button的Image
     - Name Text → NameText
     - HP Text → HPText
     - MP Text → MPText

7. **保存为预制体**
   - 拖到Project面板

---

### 步骤2：创建Spirit切换器面板

1. **创建主面板**
   - Canvas → UI → Panel
   - 命名为 "SpiritSwitcherPanel"
   - 位置: 底部居中
   - 大小: 800x160

2. **创建槽位容器**
   - 在Panel下创建空对象 "SlotsContainer"
   - Add Component → Horizontal Layout Group
     - Spacing: 15
     - Child Alignment: Middle Center
     - Child Force Expand: Width ✓, Height ✓
     - Padding: 10, 10, 10, 10

3. **创建切换按钮**
   - Canvas → UI → Button
   - 命名为 "ToggleSpiritSwitcherButton"
   - 位置: 右下角
   - 文字: "切换心兽"

4. **添加UI_SpiritSwitcher组件**
   - 选中SpiritSwitcherPanel
   - Add Component → UI_SpiritSwitcher
   - 连接引用：
     - Spirit Slot Prefab → SpiritSlot_Prefab预制体
     - Slots Container → SlotsContainer
     - Toggle Button → ToggleSpiritSwitcherButton
     - Panel → SpiritSwitcherPanel本身

5. **默认隐藏面板**
   - 取消勾选SpiritSwitcherPanel

---

### 步骤3：更新Battle UI

1. **找到现有UI元素**
   - 找到玩家HP条旁边的位置
   - 找到玩家MP条旁边的位置
   - 找到敌人HP条旁边的位置
   - 找到敌人MP条旁边的位置

2. **添加HP/MP文本**

   **玩家HP文本：**
   - UI → Text - TextMeshPro
   - 命名为 "SpiritHPText"
   - 位置: HP条上方或右侧
   - 字体大小: 16
   - Alignment: 居中
   - Color: 白色
   - 示例文字: "100/150"

   **玩家MP文本：**
   - UI → Text - TextMeshPro
   - 命名为 "SpiritMPText"
   - 位置: MP条上方或右侧
   - 字体大小: 16
   - Alignment: 居中
   - Color: 白色

   **敌人HP文本：**
   - UI → Text - TextMeshPro
   - 命名为 "EnemyHPText"
   - 位置: 敌人HP条上方或右侧
   - 字体大小: 16
   - Alignment: 居中
   - Color: 白色

   **敌人MP文本：**
   - UI → Text - TextMeshPro
   - 命名为 "EnemyMPText"
   - 位置: 敌人MP条上方或右侧
   - 字体大小: 16
   - Alignment: 居中
   - Color: 白色

3. **连接UI_BattleView组件**
   - 找到Battle场景中的UI_BattleView对象
   - 在Inspector中找到UI_BattleView组件
   - 连接新增字段：
     - Spirit Hp Text → SpiritHPText
     - Spirit Mp Text → SpiritMPText
     - Enemy Hp Text → EnemyHPText
     - Enemy Mp Text → EnemyMPText

---

### 步骤4：连接BattleController

1. **找到BattleController对象**
   - Battle场景中的BattleController GameObject

2. **连接Spirit Switcher**
   - Spirit Switcher字段 → SpiritSwitcherPanel

---

## 🔄 核心功能说明

### Spirit状态保存机制

系统会自动保存每个Spirit的HP/MP状态：
```csharp
// 战斗开始时初始化所有Spirit的状态
spiritRuntimeData[i] = new SpiritRuntimeData
{
    CurrentHP = data.MaxHP,
    MaxHP = data.MaxHP,
    CurrentMP = data.MaxMana,
    MaxMP = data.MaxMana
};

// 切换时自动保存当前Spirit状态
// 切换回来时自动恢复之前的HP/MP
```

### 实时更新

- HP/MP变化时自动更新所有UI
- Spirit切换器实时显示所有Spirit的状态
- 战斗UI显示当前Spirit的HP/MP

---

## 🎮 UI布局示例

### 战斗界面布局：
```
┌──────────────────────────────────────────────┐
│   [玩家]                    [敌人]           │
│   [图标]                    [图标]           │
│                                              │
│   HP: 85/100 ████████░░                      │
│   MP: 50/100 █████████░                      │
│                                              │
│   [技能1] [技能2] [技能3]  [结束回合]        │
│                                              │
│                              [切换心兽 ▼]    │
└──────────────────────────────────────────────┘
              ↓ 点击后弹出
┌──────────────────────────────────────────────┐
│                                              │
│  [🐻]    [🦊]    [🐺]    [🦅]    [ ]    [ ]  │
│  熊      狐狸    狼      鹰      空     空    │
│  HP:85/100 HP:90/90 HP:0/80 HP:70/70         │
│  MP:50/100 MP:80/80 MP:0/50 MP:60/60         │
│  选中      存活     死亡     存活             │
└──────────────────────────────────────────────┘
```

---

## 📊 测试清单

### Spirit切换器测试
- [ ] 显示所有出场Spirit的图标和名称
- [ ] 实时显示每个Spirit的HP/MP数值
- [ ] 当前Spirit有黄色背景高亮
- [ ] 死亡Spirit图标变灰且不可点击
- [ ] 空槽位显示"Empty"且禁用

### HP/MP显示测试
- [ ] Battle UI正确显示玩家HP/MP文字
- [ ] Battle UI正确显示敌人HP/MP文字
- [ ] 受伤后数值正确更新
- [ ] 使用技能后MP数值正确更新
- [ ] 格式正确："当前值/最大值"

### 状态保存测试
- [ ] 切换Spirit时，保存当前HP/MP
- [ ] 切换回来时，恢复之前的HP/MP
- [ ] Spirit受伤后切换，伤害保持
- [ ] Spirit使用技能后切换，蓝量保持
- [ ] 所有Spirit的HP/MP在切换器中实时显示

---

## 🐛 常见问题

### Q: HP/MP文本不显示？
A: 检查UI_BattleView的字段是否正确连接了TextMeshPro对象

### Q: Spirit切换器不显示HP/MP？
A: 检查SpiritSlot组件的hpText和mpText字段是否连接

### Q: 切换后HP/MP重置为满？
A: 确保BattleController中的spiritRuntimeData字典正常工作

### Q: HP/MP数值不更新？
A: 检查Update()方法是否正常调用RefreshSlots()

---

## 💡 扩展建议

1. **颜色编码**：
   - HP低于30%显示红色
   - HP低于60%显示黄色
   - HP高于60%显示绿色

2. **百分比显示**：
   - 添加百分比："HP: 85/100 (85%)"

3. **迷你HP条**：
   - 在Spirit图标下方添加小型HP/MP条

4. **动画效果**：
   - HP/MP变化时数字跳动动画
   - 切换Spirit时淡入淡出效果

---

现在你的系统完全具备实时HP/MP显示功能了！🎮✨
