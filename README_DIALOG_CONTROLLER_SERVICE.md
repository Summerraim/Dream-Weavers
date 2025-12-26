# DialogControllerService 使用说明

## 概述

DialogControllerService 是一个专业的对话数据管理服务，用于统一管理游戏中的对话数据。它提供了对话数据的加载、缓存、映射和错误处理功能。

## 主要功能

### 1. 对话数据管理
- 支持从 Resources 文件夹加载对话数据
- 支持自定义对话数据列表
- 提供对话数据缓存机制
- 自动创建备用对话数据

### 2. 房间类型映射
- 将 RoomType_cza 枚举映射到对应的对话ID
- 支持动态添加和修改映射关系
- 提供预加载功能

### 3. 错误处理
- 自动创建备用对话数据
- 详细的日志记录
- 向后兼容的备用机制

## 使用方法

### 基本使用

```csharp
// 获取服务实例
DialogControllerService dialogService = DialogControllerService.Instance;

// 根据房间类型获取对话数据
DialogueData dialogueData = dialogService.GetDialogueForRoom(RoomType_cza.Combat);

// 根据对话ID获取对话数据
DialogueData dialogueData = dialogService.GetDialogueData("Room_Combat_Enter");
```

### 在 RoomManager 中使用

DialogControllerService 已经集成到 RoomManager 中。当进入房间时，会自动使用 DialogControllerService 来获取对话数据：

```csharp
// 进入房间时自动触发对话
RoomManager.Instance.EnterRoom(roomId);
```

### 预加载对话数据

```csharp
// 预加载所有房间类型的对话数据
dialogService.PreloadAllRoomDialogues();

// 预加载指定的对话数据
dialogService.PreloadDialogueData("Room_Combat_Enter");
```

### 自定义映射关系

```csharp
// 添加或修改房间类型映射
dialogService.AddRoomDialogueMapping(RoomType_cza.Combat, "Custom_Combat_Dialogue");
```

## 配置说明

### DialogControllerService 配置

在 Unity Inspector 中可以配置以下参数：

- **enableDialogueCaching**: 是否启用对话缓存（默认启用）
- **enableDebugLog**: 是否启用调试日志（默认启用）
- **roomDialogueMappings**: 房间类型到对话ID的映射列表
- **customDialogueData**: 自定义对话数据列表

### 默认映射关系

| 房间类型 | 对话ID |
|---------|--------|
| Combat | Room_Combat_Enter |
| Rest | Room_Rest_Enter |
| Props | Room_Props_Enter |
| Events | Room_Events_Enter |
| Boss | Room_Boss_Enter |
| Skill | Room_Skill_Enter |

## 对话数据文件

### 创建对话数据

1. 在 Unity 编辑器中右键点击 Project 窗口
2. 选择 Create -> Dialogue System -> Dialogue Data
3. 将文件保存到 `Assets/Resources/Dialogues/` 目录
4. 文件名应与对话ID一致（如：Room_Combat_Enter）

### 对话数据结构

每个对话数据文件包含：
- **dialogueId**: 对话的唯一标识符
- **dialogueEntries**: 对话条目数组
  - speakerName: 说话者名字
  - dialogueText: 对话文本
  - portrait: 角色立绘（可选）
  - portraitPosition: 立绘位置（左/右/无）

## 集成示例

### 手动触发对话

```csharp
public class DialogueTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            DialogControllerService dialogService = DialogControllerService.Instance;
            DialogController dialogController = FindObjectOfType<DialogController>();
            
            DialogueData dialogueData = dialogService.GetDialogueForRoom(RoomType_cza.Events);
            dialogController.StartDialogue(dialogueData);
        }
    }
}
```

### 事件触发对话

```csharp
public class EventManager : MonoBehaviour
{
    private void OnGameEventTriggered(string eventId)
    {
        DialogControllerService dialogService = DialogControllerService.Instance;
        DialogController dialogController = FindObjectOfType<DialogController>();
        
        // 根据事件ID获取对话数据
        DialogueData dialogueData = dialogService.GetDialogueData($"Event_{eventId}");
        if (dialogueData != null)
        {
            dialogController.StartDialogue(dialogueData);
        }
    }
}
```

## 错误处理

### 对话数据缺失

如果找不到对应的对话数据，DialogControllerService 会自动创建备用对话数据：

```csharp
// 如果找不到 "Room_Combat_Enter" 对话数据，会创建包含以下内容的备用数据：
// 说话者: 系统
// 文本: 这是Room_Combat_Enter的对话内容。请创建对应的对话数据文件。
```

### 服务初始化失败

如果 DialogControllerService 初始化失败，RoomManager 会使用原有的备用方法加载对话数据，确保功能正常。

## 性能优化

### 缓存机制

启用对话缓存后，首次加载的对话数据会被缓存，后续请求会直接从缓存中获取，提高性能。

### 预加载

在游戏启动时预加载常用对话数据，避免运行时加载延迟：

```csharp
void Start()
{
    DialogControllerService.Instance.PreloadAllRoomDialogues();
}
```

## 调试和日志

### 启用调试日志

在 Inspector 中启用 `enableDebugLog` 可以查看详细的调试信息：

```
[DialogControllerService] 从缓存获取对话数据: Room_Combat_Enter
[DialogControllerService] 预加载自定义对话数据: Room_Boss_Enter
```

### 日志级别

- **Debug**: 一般操作信息
- **Warning**: 警告信息（如对话数据缺失）
- **Error**: 错误信息（如服务初始化失败）

## 向后兼容性

DialogControllerService 设计为向后兼容。如果服务不可用，系统会回退到原有的对话数据加载方式，确保游戏功能正常。

## 总结

DialogControllerService 提供了一个统一、高效、可靠的对话数据管理解决方案。通过使用这个服务，你可以：

1. 简化对话数据管理
2. 提高对话系统性能
3. 增强错误处理能力
4. 保持系统向后兼容

建议在新的对话功能开发中使用 DialogControllerService，并逐步迁移现有的对话系统。
