# CG动画系统故障排查指南

## 🔍 如何排查问题

当CG动画不工作时，**Console窗口的日志是你最好的朋友**！

### 第一步：运行测试并查看Console

1. 运行游戏
2. 点击测试按钮
3. **立即打开Console窗口**（Window → General → Console）
4. 根据日志判断问题

---

## 📋 常见问题和解决方案

### 问题1：Panel没有显示

**症状**：点击测试按钮后，屏幕上什么都没显示

**Console日志**：
```
[编辑器] 开始播放CG序列测试
[BossDefeatedCGSequence] ❌ CG Panel 未设置！请在Inspector中配置 'Cg Panel' 字段
```

**原因**：`Cg Panel` 字段没有配置

**解决方法**：
1. 选中 `AsmontisCGPanel`
2. 在 `BossDefeatedCGSequence` 组件中找到 `Cg Panel` 字段
3. **把 AsmontisCGPanel 拖到这个字段上**（自己拖自己）

---

### 问题2：GameObject没有被激活

**症状**：Panel显示了，但是里面的CG物体看不见

**Console日志**：
```
[BossDefeatedCGSequence] ✅ 显示CG Panel
[BossDefeatedCGSequence] === 阶段1：播放开场动画 ===
[BossDefeatedCGSequence] 尝试激活 cgObject1: CGObject1
[BossDefeatedCGSequence] cgObject1 激活状态: activeSelf=true, activeInHierarchy=false
```

**关键指标**：`activeInHierarchy=false`

**原因**：父物体（可能是cgPanel或其上层）处于未激活状态

**解决方法**：
1. 在Hierarchy中检查整个层级链：
   ```
   Canvas (必须是active)
   └── BattleView (必须是active)
       └── AsmontisCGPanel (必须是active)
           └── CGObject1 (应该被脚本激活)
   ```
2. 确保从Canvas到AsmontisCGPanel的所有父物体都是激活状态
3. **测试时，AsmontisCGPanel必须是激活状态**

---

### 问题3：动画没有播放

**症状**：GameObject显示了，但是图片不动

**Console日志（情况A - 动画Clip名称错误）**：
```
[BossDefeatedCGSequence] 开始播放动画: Opening
[BossDefeatedCGSequence] 动画状态检查: IsName(Opening)=false, normalizedTime=0.000
[BossDefeatedCGSequence] ❌ 动画 Opening 没有开始播放！可能的原因：
1. Animation Clip名称错误（应该是: 'Opening'）
2. Animator Controller未正确配置
```

**原因**：Animation Clip的名称与代码中设置的不一致

**解决方法**：
1. 打开 Animator 窗口（双击Animator Controller）
2. 查看状态的**确切名称**（可能是"Opening"、"Base Layer.Opening"等）
3. 在 `BossDefeatedCGSequence` 组件中：
   - 修改 `Animation 1 Name` 字段，使其与Animator中的状态名称**完全一致**
   - 注意大小写和空格

**Console日志（情况B - Animator Controller未配置）**：
```
[BossDefeatedCGSequence] ⚠️ cgObject1(CGObject1) 或 animator1 未设置，跳过开场动画
```

**原因**：Animator组件或字段未配置

**解决方法**：
1. 检查CGObject1是否有 `Animator` 组件
2. Animator组件的 `Controller` 字段是否已设置
3. 在 `BossDefeatedCGSequence` 组件中，`Animator 1` 字段是否已配置

---

### 问题4：动画播放了但立即结束

**症状**：看到一帧图片闪过，然后就没了

**Console日志**：
```
[BossDefeatedCGSequence] ✅ 动画 Opening 已开始播放，等待完成...
[BossDefeatedCGSequence] 动画播放进度: 0.0%
[BossDefeatedCGSequence] ✅ 动画 Opening 播放完成 (normalizedTime: 1.000)
```
（进度从0%直接到100%）

**原因**：动画时长太短，或者Animation Clip没有正确配置

**解决方法**：
1. 打开 Animation 窗口
2. 选中你的 Animation Clip
3. 检查：
   - 时间轴上是否有关键帧？
   - 总时长是多少？（建议至少1-2秒）
   - 是否正确设置了 Image.sprite 属性的关键帧？
4. 如果是逐帧动画：
   - 确保每一帧都添加了不同的Sprite
   - 帧率设置合理（通常30或60）

---

### 问题5：第一个动画播放完后没有播放后续动画

**症状**：只播放了开场动画，然后就停了

**Console日志（正常情况）**：
```
[BossDefeatedCGSequence] ✅ 开场动画播放完成
[BossDefeatedCGSequence] === 阶段2：同时播放剧情CG（物体2、3、4）===
[BossDefeatedCGSequence] 激活并播放 cgObject2: CGObject2
```

**Console日志（异常情况）**：
```
[BossDefeatedCGSequence] ✅ 开场动画播放完成
[BossDefeatedCGSequence] === 阶段2：同时播放剧情CG（物体2、3、4）===
[BossDefeatedCGSequence] ⚠️ 没有配置任何中间剧情CG，跳过阶段2
```

**原因**：中间的3个CG物体或Animator没有配置

**解决方法**：
1. 检查 `BossDefeatedCGSequence` 组件：
   - `Cg Object 2/3/4` 是否已配置
   - `Animator 2/3/4` 是否已配置
2. 至少配置一个用于测试
3. 确保对应的Animation Clip已创建

---

### 问题6：动画卡住不动了

**症状**：播放到一半就停了，不继续也不结束

**Console日志**：
```
[BossDefeatedCGSequence] ✅ 动画 Opening 已开始播放，等待完成...
[BossDefeatedCGSequence] 动画播放进度: 50.0%
[BossDefeatedCGSequence] 动画播放进度: 50.0%
[BossDefeatedCGSequence] 动画播放进度: 50.0%
（一直卡在50%）
```

**原因**：Animation Clip的 `Loop Time` 被勾选了，导致动画循环播放永不结束

**解决方法**：
1. 在Project窗口选中你的 Animation Clip
2. 在Inspector中**取消勾选** `Loop Time`
3. 点击 Apply

---

### 问题7：看到了fallback日志

**Console日志**：
```
[BossDefeatedCGSequence] 使用fallback：等待2秒
```

**含义**：动画系统检测失败，脚本自动等待2秒作为替代方案

**这意味着**：
- Animation Clip名称不对
- Animator Controller配置有问题
- 动画状态机没有正确设置

**解决方法**：按照"问题3"的步骤检查动画配置

---

## ✅ 正常日志示例

当一切正常时，Console应该显示类似这样的日志：

```
[编辑器] 开始播放CG序列测试
[BossDefeatedCGSequence] ✅ CG Panel 已配置: AsmontisCGPanel
[BossDefeatedCGSequence] ✅ 配置了 3 个中间剧情CG
[BossDefeatedCGSequence] 开始播放Boss击败CG序列
[BossDefeatedCGSequence] ✅ 显示CG Panel

[BossDefeatedCGSequence] === 阶段1：播放开场动画 ===
[BossDefeatedCGSequence] 尝试激活 cgObject1: CGObject1
[BossDefeatedCGSequence] cgObject1 激活状态: activeSelf=true, activeInHierarchy=true
[BossDefeatedCGSequence] 开始播放动画: Opening
[BossDefeatedCGSequence] 等待动画 Opening 播放完成...
[BossDefeatedCGSequence] 动画状态检查: IsName(Opening)=true, normalizedTime=0.000
[BossDefeatedCGSequence] ✅ 动画 Opening 已开始播放，等待完成...
[BossDefeatedCGSequence] 动画播放进度: 0.0%
[BossDefeatedCGSequence] 动画播放进度: 33.3%
[BossDefeatedCGSequence] 动画播放进度: 66.6%
[BossDefeatedCGSequence] ✅ 动画 Opening 播放完成 (normalizedTime: 1.000)
[BossDefeatedCGSequence] ✅ 开场动画播放完成

[BossDefeatedCGSequence] === 阶段2：同时播放剧情CG（物体2、3、4）===
[BossDefeatedCGSequence] 激活并播放 cgObject2: CGObject2
[BossDefeatedCGSequence] 激活并播放 cgObject3: CGObject3
[BossDefeatedCGSequence] 激活并播放 cgObject4: CGObject4
[BossDefeatedCGSequence] 已启动 3 个剧情CG动画，等待播放完成...
（等待动画播放...）
[BossDefeatedCGSequence] ✅ 剧情CG播放完成

[BossDefeatedCGSequence] === 阶段3：播放结尾动画 ===
[BossDefeatedCGSequence] 激活并播放 cgObject5: CGObject5
（等待动画播放...）
[BossDefeatedCGSequence] ✅ 结尾动画播放完成

[BossDefeatedCGSequence] 隐藏CG Panel
[BossDefeatedCGSequence] CG序列播放完成，触发回调
```

---

## 🛠 快速检查清单

遇到问题时，按顺序检查：

1. ✅ AsmontisCGPanel 是否激活？
2. ✅ BossDefeatedCGSequence 组件的 `Cg Panel` 字段是否配置？
3. ✅ 5个CG物体是否都创建了？（至少一个用于测试）
4. ✅ 每个CG物体是否有 Image 组件？
5. ✅ 每个CG物体是否有 Animator 组件？
6. ✅ 每个Animator的 Controller 字段是否配置？
7. ✅ Animation Clip 是否已创建并添加到Controller？
8. ✅ Animation Clip 的名称是否与组件中的设置一致？
9. ✅ Animation Clip 的 `Loop Time` 是否取消勾选？
10. ✅ Animation Clip 是否有实际内容（关键帧）？

---

## 💡 调试技巧

### 1. 使用简化测试
如果不确定问题在哪，先简化测试：
- 只配置 cgObject1
- 创建一个超简单的动画（2-3帧）
- 确保这个能工作后，再逐步添加其他物体

### 2. 观察Hierarchy窗口
运行游戏后，在Hierarchy中展开AsmontisCGPanel：
- 播放时，应该能看到CGObject1被激活（变亮）
- 播放完后，应该能看到它被禁用（变灰）

### 3. 使用Frame Debugger
如果GameObject激活了但看不见：
- Window → Analysis → Frame Debugger
- 点击 Enable
- 查看渲染层级，确认Image是否被渲染

### 4. 检查Canvas设置
如果整个Panel都看不见：
- 选中Canvas
- 确保 Render Mode 设置正确
- 确保Canvas Scaler 设置合理
- 确保没有被其他UI挡住

---

## 📞 仍然无法解决？

如果按照上述步骤仍无法解决：

1. **截图发送**：
   - Console窗口的完整日志
   - Hierarchy窗口的层级结构
   - BossDefeatedCGSequence组件的Inspector截图
   - Animator窗口的状态机截图

2. **描述问题**：
   - 具体现象是什么？
   - 你期望看到什么？
   - 你实际看到了什么？
   - Console有什么特殊的错误或警告？

祝调试顺利！🚀
