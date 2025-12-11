# Spirit切换系统 - Unity设置指南

## 📋 系统概述

Spirit切换系统允许玩家在战斗中手动切换出场的心兽。系统包括：
- 显示6个阵容槽位的UI面板
- 点击槽位切换到对应的Spirit
- 显示当前选中和死亡状态
- 空槽位自动显示为空

---

## 🎮 组件说明

### 1. UI_SpiritSwitcher（切换器主组件）
- 管理整个切换面板
- 处理槽位点击事件
- 刷新槽位状态

### 2. SpiritSlot（单个槽位组件）
- 显示Spirit图标和名称
- 处理点击事件
- 显示选中/死亡状态

### 3. BattleController（扩展功能）
- `SwitchToSpirit(int index)` - 手动切换Spirit
- `GetDeployedSpirits()` - 获取阵容列表
- `IsSpiritAlive(int index)` - 检查存活状态
- 自动跟踪Spirit存活状态

---

## 🛠️ Unity设置步骤

### 步骤1：创建Spirit切换UI

#### 1.1 创建主面板
1. 在Battle场景的Canvas下，右键 → UI → Panel
2. 重命名为 "SpiritSwitcherPanel"
3. 设置位置和大小：
   - Anchor: 居中或底部
   - Width: 700
   - Height: 150

#### 1.2 创建槽位容器
1. 在SpiritSwitcherPanel下，右键 → Create Empty
2. 重命名为 "SlotsContainer"
3. 添加 **Horizontal Layout Group** 组件：
   - Spacing: 10
   - Child Alignment: Middle Center
   - Child Force Expand: Width ✓, Height ✓
   - Padding: Left=10, Right=10, Top=10, Bottom=10

#### 1.3 创建Spirit槽位预制体（可选）
1. 在Hierarchy中创建 → UI → Button
2. 重命名为 "SpiritSlot_Prefab"
3. 设置Button大小: 100x100
4. 在Button下创建子对象：
   ```
   SpiritSlot_Prefab (Button)
   ├── Icon (Image) - 显示Spirit图标
   ├── NameText (TextMeshPro) - 显示Spirit名称
   ├── SelectedIndicator (Image) - 选中边框（黄色）
   └── DeadOverlay (Image) - 死亡遮罩（半透明黑色）
   ```

5. 配置各子对象：
   - **Icon**:
     - 大小: 80x80
     - Preserve Aspect: ✓
   - **NameText**:
     - 位置: 底部
     - 字体大小: 14
   - **SelectedIndicator**:
     - 铺满整个Button
     - Color: 黄色 (1, 0.8, 0.3, 0.8)
     - 默认隐藏
   - **DeadOverlay**:
     - 铺满整个Button
     - Color: 黑色半透明 (0, 0, 0, 0.7)
     - 默认隐藏

6. 添加 **SpiritSlot** 组件到Button
7. 在Inspector中连接引用：
   - Spirit Icon → Icon (Image)
   - Background → Button的Image
   - Name Text → NameText
   - Selected Indicator → SelectedIndicator
   - Dead Overlay → DeadOverlay

8. 将Button拖到Project面板创建预制体

#### 1.4 创建切换按钮
1. 在Canvas下创建 → UI → Button
2. 重命名为 "ToggleSpiritSwitcherButton"
3. 设置位置：屏幕右上角或合适位置
4. 修改按钮文字为 "切换心兽" 或图标

---

### 步骤2：配置UI_SpiritSwitcher组件

1. 选中 **SpiritSwitcherPanel**
2. 在Inspector中点击 **Add Component**
3. 添加 **UI_SpiritSwitcher** 组件
4. 配置引用：
   - **Spirit Slot Prefab**: 拖入之前创建的预制体
   - **Slots Container**: 拖入SlotsContainer对象
   - **Toggle Button**: 拖入ToggleSpiritSwitcherButton
   - **Panel**: 拖入SpiritSwitcherPanel本身

5. 默认隐藏面板（取消勾选SpiritSwitcherPanel）

---

### 步骤3：连接BattleController

1. 选中Battle场景中的 **BattleController** GameObject
2. 在Inspector中找到 **BattleController** 组件
3. 找到 **Spirit Switcher** 字段
4. 将 **SpiritSwitcherPanel** 拖入该字段

---

### 步骤4：配置PlayerData

确保你的PlayerData配置了DeployedSpirits：
1. 在Project中找到你的PlayerData资源
2. 在Inspector中配置：
   - **Deployed Spirits**: 添加1-6个Spirit
   - 每个Spirit必须有Image（图标）

---

## 🎨 UI布局建议

### 方案1：底部横向布局
```
┌────────────────────────────────────┐
│         战斗区域                    │
│                                    │
│   [玩家]          [敌人]           │
│                                    │
│   [技能1] [技能2] [技能3]          │
├────────────────────────────────────┤
│ [切换心兽]                          │
└────────────────────────────────────┘
     ↓ 点击后弹出
┌────────────────────────────────────┐
│ [槽1] [槽2] [槽3] [槽4] [槽5] [槽6] │
└────────────────────────────────────┘
```

### 方案2：侧边纵向布局
```
┌─────┬────────────────────────┐
│[槽1]│                        │
│[槽2]│    战斗区域             │
│[槽3]│                        │
│[槽4]│  [玩家]    [敌人]      │
│[槽5]│                        │
│[槽6]│  [技能按钮]             │
└─────┴────────────────────────┘
```

---

## 🎯 功能说明

### 自动功能
1. **战斗开始时**：自动初始化所有槽位
2. **Spirit死亡时**：
   - 自动标记为死亡
   - 自动切换到下一个存活的Spirit
   - 槽位显示死亡遮罩
3. **切换成功时**：
   - 自动刷新UI
   - 自动应用羁绊效果
   - 更新选中状态

### 手动操作
1. **点击切换按钮**：显示/隐藏Spirit面板
2. **点击槽位**：切换到对应的Spirit
3. **限制**：
   - 不能切换到当前Spirit
   - 不能切换到死亡的Spirit
   - 不能切换到空槽位

---

## 🔧 自定义配置

### 修改槽位样式

在SpiritSlot预制体中：
- **正常状态**: 灰色背景
- **选中状态**: 黄色高亮边框
- **死亡状态**: 黑色半透明遮罩
- **空槽位**: 无图标，按钮禁用

### 添加动画

可以为SpiritSwitcherPanel添加Animator：
1. 创建Animation：Panel弹出/收起动画
2. 在UI_SpiritSwitcher的TogglePanel中触发动画

### 修改布局

修改SlotsContainer的Layout Group：
- **Horizontal**: 横向排列
- **Vertical**: 纵向排列
- **Grid**: 网格排列

---

## 🐛 故障排除

### 问题1：点击槽位没有反应
**检查**：
- BattleController是否连接了Spirit Switcher？
- SpiritSlot的Button是否添加了SpiritSlot组件？
- Console中是否有错误信息？

### 问题2：槽位显示为空
**检查**：
- PlayerData的DeployedSpirits是否配置？
- Spirit的Image字段是否有图标？
- SpiritSlot的spiritIcon是否正确连接？

### 问题3：无法切换Spirit
**检查**：
- 目标Spirit是否还活着？
- 是否试图切换到当前Spirit？
- Console中查看详细日志

### 问题4：切换后羁绊丢失
**不用担心**：系统会自动重新应用羁绊
- 查看Console输出："Team synergies re-applied to new spirit"

---

## 📊 测试清单

### 基础测试
- [ ] 战斗开始时，面板正确显示6个槽位
- [ ] 空槽位显示为空/禁用
- [ ] 当前Spirit有选中高亮
- [ ] 点击槽位可以切换Spirit

### 死亡测试
- [ ] Spirit死亡时，槽位显示死亡遮罩
- [ ] 无法点击死亡的Spirit槽位
- [ ] 自动切换到下一个存活Spirit
- [ ] 所有Spirit死亡时，战斗失败

### UI测试
- [ ] 切换按钮正常显示/隐藏面板
- [ ] 切换后UI正确刷新
- [ ] 技能按钮更新为新Spirit的技能
- [ ] HP/Mana条显示新Spirit的数值

---

## 💡 进阶功能建议

1. **Spirit信息提示**：鼠标悬停显示Spirit详细信息
2. **切换冷却**：限制切换频率（如每3回合一次）
3. **切换动画**：Spirit切换时播放过渡动画
4. **音效**：切换时播放音效
5. **快捷键**：按数字键1-6快速切换
6. **拖拽切换**：拖拽槽位改变出场顺序
7. **战斗外编队**：在主菜单管理出场阵容

---

## 📝 代码示例

### 快捷键切换（可选）

在BattleController.Update()中添加：
```csharp
// 数字键快速切换Spirit
if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToSpirit(0);
if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToSpirit(1);
if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToSpirit(2);
if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchToSpirit(3);
if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchToSpirit(4);
if (Input.GetKeyDown(KeyCode.Alpha6)) SwitchToSpirit(5);
```

### 切换冷却（可选）

在BattleController中添加：
```csharp
private int lastSwitchTurn = 0;
private int switchCooldown = 3; // 3回合冷却

public bool CanSwitchSpirit()
{
    return model.CurrentTurn - lastSwitchTurn >= switchCooldown;
}

// 在SwitchToSpirit中添加检查
if (!CanSwitchSpirit())
{
    Debug.LogWarning("Spirit switch is on cooldown");
    return false;
}
lastSwitchTurn = model.CurrentTurn;
```

---

现在你的Spirit切换系统已经完全实现！按照上述步骤在Unity中设置即可。🎮
