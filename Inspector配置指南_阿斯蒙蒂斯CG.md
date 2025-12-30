# 阿斯蒙蒂斯特殊CG动画 - Unity Inspector配置指南

## 📋 配置步骤总览

1. **在Scene中创建CG Panel层级结构**
2. **为每个CG物体创建Animation动画**
3. **配置BossDefeatedCGSequence组件**
4. **在BattleView中引用CG序列**
5. **测试动画播放**

---

## 🎬 第一步：创建CG Panel层级结构

### 1.1 在Battle场景中找到Canvas
打开你的战斗场景（BattleScene），在Hierarchy窗口中找到：
```
Canvas
└── BattleView (或类似的UI根节点)
```

### 1.2 创建AsmontisCGPanel容器
1. 右键点击 `BattleView` → `Create Empty`
2. 重命名为 `AsmontisCGPanel`
3. 在Inspector中：
   - **RectTransform**: 设置为全屏
     - Anchor Presets: 选择 **Stretch/Stretch**（右下角的全屏模式）
     - Left, Top, Right, Bottom 全部设为 `0`
   - **初始状态**: 取消勾选左上角的复选框（SetActive = false）

### 1.3 创建5个CG物体
在 `AsmontisCGPanel` 下创建5个子物体：

#### CGObject1（开场动画）
1. 右键 `AsmontisCGPanel` → `UI` → `Image`
2. 重命名为 `CGObject1`
3. 配置RectTransform：
   - **位置**: 根据你的开场动画需求
   - **推荐**: 全屏或屏幕中央
   - Anchor: Center-Center
   - Width: 800, Height: 600（或根据你的动画图片大小）
4. **初始状态**: SetActive = false

#### CGObject2（剧情CG - 左侧）
1. 右键 `AsmontisCGPanel` → `UI` → `Image`
2. 重命名为 `CGObject2`
3. 配置RectTransform：
   - **建议位置**: 屏幕左侧
   - Anchor: Middle-Left
   - Pos X: 200, Pos Y: 0
   - Width: 400, Height: 400
4. **初始状态**: SetActive = false

#### CGObject3（剧情CG - 中央）
1. 右键 `AsmontisCGPanel` → `UI` → `Image`
2. 重命名为 `CGObject3`
3. 配置RectTransform：
   - **建议位置**: 屏幕中央
   - Anchor: Center-Center
   - Pos X: 0, Pos Y: 0
   - Width: 400, Height: 400
4. **初始状态**: SetActive = false

#### CGObject4（剧情CG - 右侧）
1. 右键 `AsmontisCGPanel` → `UI` → `Image`
2. 重命名为 `CGObject4`
3. 配置RectTransform：
   - **建议位置**: 屏幕右侧
   - Anchor: Middle-Right
   - Pos X: -200, Pos Y: 0
   - Width: 400, Height: 400
4. **初始状态**: SetActive = false

#### CGObject5（结尾动画）
1. 右键 `AsmontisCGPanel` → `UI` → `Image`
2. 重命名为 `CGObject5`
3. 配置RectTransform：
   - **建议位置**: 全屏或屏幕中央
   - Anchor: Center-Center
   - Width: 800, Height: 600
4. **初始状态**: SetActive = false

**最终层级结构应该是：**
```
Canvas
└── BattleView
    └── AsmontisCGPanel [SetActive = false]
        ├── CGObject1 [SetActive = false]
        ├── CGObject2 [SetActive = false]
        ├── CGObject3 [SetActive = false]
        ├── CGObject4 [SetActive = false]
        └── CGObject5 [SetActive = false]
```

---

## 🎨 第二步：创建Animation动画

### 2.1 为CGObject1创建开场动画

#### 创建Animator Controller
1. 在Project窗口中，右键你的动画文件夹（比如 `Assets/Animations/BossCG/`）
2. `Create` → `Animator Controller`
3. 命名为 `AsmontisCG_Opening`

#### 创建Animation Clip
1. 在同一文件夹，右键 → `Create` → `Animation`
2. 命名为 `Opening`
3. 双击打开Animation窗口

#### 制作逐帧动画
1. 在Hierarchy中选中 `CGObject1`
2. 在Animation窗口中：
   - 点击左上角的红点开始录制
   - 在时间轴上点击 `Add Property`
   - 选择 `Image` → `Sprite`
3. 在时间轴上每0.1秒（或你需要的帧率）添加关键帧：
   - 在0:00秒，设置第一张图片
   - 在0:10秒，设置第二张图片
   - 依此类推
4. 设置动画总时长（比如2-3秒）
5. **重要**: 取消勾选 `Loop Time`（只播放一次）

#### 配置Animator Controller
1. 双击 `AsmontisCG_Opening` 打开Animator窗口
2. 将 `Opening` 动画拖入窗口
3. 右键 `Opening` → `Set as Layer Default State`（设为默认状态）

#### 给CGObject1添加Animator组件
1. 选中Hierarchy中的 `CGObject1`
2. 在Inspector中点击 `Add Component` → `Animator`
3. 将 `AsmontisCG_Opening` 拖到 `Controller` 字段

### 2.2 为CGObject2、3、4创建剧情CG动画

重复2.1的步骤，但注意：
- Animator Controller命名: `AsmontisCG_Story`
- Animation Clip命名: `StorySequence`
- **三个物体使用相同的Animator Controller和Animation**（因为它们时长一样）
- 或者为每个物体创建独立的动画（如果内容不同）

**关键**：确保这三个动画的时长相同（比如5-8秒）

### 2.3 为CGObject5创建结尾动画

重复2.1的步骤：
- Animator Controller命名: `AsmontisCG_Ending`
- Animation Clip命名: `Ending`
- 时长：1-2秒

---

## ⚙️ 第三步：配置BossDefeatedCGSequence组件

### 3.1 添加组件
1. 在Hierarchy中选中 `AsmontisCGPanel`
2. 在Inspector中点击 `Add Component`
3. 搜索并添加 `BossDefeatedCGSequence`

### 3.2 配置组件字段

在 `BossDefeatedCGSequence` 组件的Inspector中：

#### CG动画物体
- **Cg Object 1**: 拖入 `CGObject1`
- **Cg Object 2**: 拖入 `CGObject2`
- **Cg Object 3**: 拖入 `CGObject3`
- **Cg Object 4**: 拖入 `CGObject4`
- **Cg Object 5**: 拖入 `CGObject5`

#### 动画控制器
- **Animator 1**: 拖入 `CGObject1` 上的 `Animator` 组件
- **Animator 2**: 拖入 `CGObject2` 上的 `Animator` 组件
- **Animator 3**: 拖入 `CGObject3` 上的 `Animator` 组件
- **Animator 4**: 拖入 `CGObject4` 上的 `Animator` 组件
- **Animator 5**: 拖入 `CGObject5` 上的 `Animator` 组件

**技巧**：从Hierarchy拖动GameObject时，Unity会自动找到其Animator组件

#### 动画剪辑名称
- **Animation 1 Name**: `Opening`（必须与你的Animation Clip名称一致）
- **Animation 234 Name**: `StorySequence`（剧情CG的动画名称）
- **Animation 5 Name**: `Ending`（结尾动画名称）

#### CG容器
- **Cg Panel**: 拖入 `AsmontisCGPanel` 自己

#### 调试选项
- **Auto Play On Start**: 保持 `false`（仅测试时勾选）

---

## 🔗 第四步：在BattleView中引用CG序列

### 4.1 配置UI_BattleView组件
1. 在Hierarchy中找到并选中 `BattleView`（或你的战斗UI根节点）
2. 在Inspector中找到 `UI_BattleView (Script)` 组件
3. 滚动到底部，找到 **Special Boss CG** 区域
4. 将 `AsmontisCGPanel` 拖到 **Asmontis CG Sequence** 字段

---

## 🧪 第五步：测试动画播放

### 5.1 方法一：使用自动播放功能
1. 选中 `AsmontisCGPanel`
2. **确保 AsmontisCGPanel 是激活状态**（勾选左上角的复选框）
3. 在 `BossDefeatedCGSequence` 组件中：
   - 勾选 `Auto Play On Enable`
   - 设置 `Auto Play Delay` = 0.5秒（可以调整）
4. 运行游戏（Play按钮）
5. 等待0.5秒后，应该会自动播放完整的CG序列

### 5.2 方法二：使用编辑器测试按钮（推荐）
1. 选中 `AsmontisCGPanel`
2. **确保 AsmontisCGPanel 是激活状态**
3. 运行游戏（Play按钮）
4. 在Inspector最底部，你会看到：
   ```
   === 编辑器测试工具 ===
   [▶ 测试播放CG序列] 按钮
   [⬛ 停止播放] 按钮
   ```
5. 点击 **▶ 测试播放CG序列** 按钮
6. 动画应该立即开始播放

**编辑器按钮的优势**：
- ✅ 可以随时暂停游戏后点击按钮测试
- ✅ 可以反复测试而不需要重启游戏
- ✅ 有明确的状态提示

**如果动画没有播放**：
- ⚠️ **最常见原因**：AsmontisCGPanel 未激活（必须SetActive = true）
- 检查所有 Animator 是否正确配置
- 检查 Animation Clip 名称是否与组件中设置的一致
- 检查 Animation Clip 的 Loop Time 是否取消勾选
- 查看Console窗口的Debug日志（会显示 `[BossDefeatedCGSequence]` 标记）

### 5.2 测试完整战斗流程
1. 取消勾选 `Auto Play On Start`
2. 进入游戏，开始战斗
3. 击败名为"阿斯蒙蒂斯"的Boss
4. 应该会看到：
   - 播放开场动画（物体1）
   - 播放剧情CG动画（物体2、3、4同时）
   - 播放结尾动画（物体5）
   - 显示捕捉结果面板
   - 显示继续按钮

---

## ⚠️ 常见问题排查

### 问题1：CG没有显示
**可能原因**：
- AsmontisCGPanel 的Canvas Group 阻挡了显示
- 层级顺序错误（被其他UI遮挡）

**解决方法**：
- 在Hierarchy中将 AsmontisCGPanel 拖到顶部（在其他UI之上）
- 检查是否有Canvas Group组件，确保Alpha = 1

### 问题2：动画播放不完整就跳过了
**可能原因**：
- Animation Clip 的时长设置错误
- Animator 的状态机有多余的Transition

**解决方法**：
- 打开Animation窗口，检查时长
- 打开Animator窗口，确保只有一个状态且没有Exit Transition

### 问题3：三个剧情CG没有同时播放
**可能原因**：
- 代码逻辑错误（不太可能）
- 三个物体没有都设置Animator

**解决方法**：
- 确保CGObject2、3、4都有Animator组件
- 检查Console是否有警告信息

### 问题4：普通Boss也触发了CG
**可能原因**：
- Boss的DisplayName设置错误

**解决方法**：
- 检查Boss的EnemyData，确保DisplayName精确为"阿斯蒙蒂斯"（中文全角）
- 查看BattleController的Debug日志

---

## 📝 配置检查清单

完成配置后，请逐项检查：

- [ ] AsmontisCGPanel已创建并设为inactive
- [ ] 5个CGObject都已创建并设为inactive
- [ ] 每个CGObject都有Image组件
- [ ] 每个CGObject都有Animator组件
- [ ] 每个Animator都配置了Controller
- [ ] 每个Animation Clip都已创建并取消Loop Time
- [ ] BossDefeatedCGSequence组件已添加到AsmontisCGPanel
- [ ] BossDefeatedCGSequence的所有字段都已配置
- [ ] UI_BattleView中的Asmontis CG Sequence已引用
- [ ] Auto Play On Start已关闭（除非测试）
- [ ] 已测试动画播放流程

---

## 🎉 配置完成！

完成以上步骤后，当你击败"阿斯蒙蒂斯"Boss时，就会看到你制作的特殊CG动画了！

如果遇到问题，查看Unity Console窗口中的日志信息，所有关键步骤都有 `[BossDefeatedCGSequence]` 标记的Debug输出。

## 🔧 后续扩展

如果想为其他Boss添加特殊CG：
1. 复制AsmontisCGPanel，重命名为新Boss的CG Panel
2. 在UI_BattleView.cs中添加新字段
3. 在ShowSpecialBossCG方法中添加新的判断条件

祝你游戏开发顺利！✨
