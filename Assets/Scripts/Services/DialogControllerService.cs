using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamWeavers.Services
{
    /// <summary>
    /// 对话控制器服务 - 管理对话数据的加载、缓存和映射
    /// </summary>
    public class DialogControllerService : MonoBehaviour
    {
        [Header("对话配置")]
        [SerializeField] private bool enableDialogueCaching = true;
        [SerializeField] private bool enableDebugLog = true;
        
        [Header("房间对话映射")]
        [SerializeField] private List<RoomDialogueMapping> roomDialogueMappings = new List<RoomDialogueMapping>
        {
            new RoomDialogueMapping { roomType = RoomType_cza.Combat, dialogueId = "Room_Combat_Enter" },
            new RoomDialogueMapping { roomType = RoomType_cza.Rest, dialogueId = "Room_Rest_Enter" },
            new RoomDialogueMapping { roomType = RoomType_cza.Props, dialogueId = "Room_Props_Enter" },
            // new RoomDialogueMapping { roomType = RoomType_cza.Events, dialogueId = "Room_Events_Enter" },
            new RoomDialogueMapping { roomType = RoomType_cza.Boss, dialogueId = "Room_Boss_Enter" },
            new RoomDialogueMapping { roomType = RoomType_cza.Skill, dialogueId = "Room_Skill_Enter" },
            new RoomDialogueMapping { roomType = RoomType_cza.Guide, dialogueId = "First Dialog" } // 暂时使用First Dialog作为引导房间对话
        };

        [Header("自定义对话数据")]
        [SerializeField] private List<DialogueData> customDialogueData = new List<DialogueData>();

        // 对话数据缓存
        private Dictionary<string, DialogueData> dialogueCache = new Dictionary<string, DialogueData>();
        
        // 单例实例
        private static DialogControllerService instance;
        public static DialogControllerService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<DialogControllerService>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("DialogControllerService");
                        instance = go.AddComponent<DialogControllerService>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// 根据房间类型和敌人数据触发对话
        /// </summary>
        /// <param name="roomType">房间类型</param>
        /// <param name="enemyData">敌人数据</param>
        public void StartDialogueForRoomWithEnemy(RoomType_cza roomType, EnemyData enemyData)
        {
            LogDebug($"开始根据房间类型和敌人数据触发对话 - 房间类型: {roomType}, 敌人: {(enemyData != null ? enemyData.DisplayName : "null")}");
            
            // 获取对应的对话数据
            DialogueData dialogueData = GetDialogueForRoomWithEnemy(roomType, enemyData);
            if (dialogueData == null)
            {
                LogWarning($"无法获取房间类型 {roomType} 和敌人 {enemyData?.DisplayName} 的对话数据");
                return;
            }
            
            // 查找DialogController并触发对话
            DialogController dialogController = FindObjectOfType<DialogController>();
            if (dialogController == null)
            {
                LogError("无法找到DialogController组件！请确保场景中有DialogController组件");
                return;
            }
            
            // 开始对话
            dialogController.StartDialogue(dialogueData);
            LogDebug($"成功触发对话 - 房间类型: {roomType}, 敌人: {enemyData?.DisplayName}");
        }

        #region 初始化

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeService();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeService()
        {
            LogDebug("DialogControllerService 初始化");
            
            // 预加载自定义对话数据到缓存
            PreloadCustomDialogueData();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 根据房间类型获取对话数据
        /// </summary>
        public DialogueData GetDialogueForRoom(RoomType_cza roomType)
        {
            string dialogueId = GetDialogueIdForRoomType(roomType);
            if (string.IsNullOrEmpty(dialogueId))
            {
                LogWarning($"未找到房间类型 {roomType} 对应的对话ID");
                return null;
            }

            return GetDialogueData(dialogueId);
        }

        /// <summary>
        /// 根据对话ID获取对话数据
        /// </summary>
        public DialogueData GetDialogueData(string dialogueId)
        {
            if (string.IsNullOrEmpty(dialogueId))
            {
                LogWarning("对话ID为空");
                return null;
            }

            // 首先检查缓存
            if (enableDialogueCaching && dialogueCache.ContainsKey(dialogueId))
            {
                LogDebug($"从缓存获取对话数据: {dialogueId}");
                return dialogueCache[dialogueId];
            }

            // 检查自定义对话数据
            DialogueData dialogueData = GetCustomDialogueData(dialogueId);
            if (dialogueData != null)
            {
                CacheDialogueData(dialogueId, dialogueData);
                return dialogueData;
            }

            // 从Resources加载
            dialogueData = LoadDialogueDataFromResources(dialogueId);
            if (dialogueData != null)
            {
                CacheDialogueData(dialogueId, dialogueData);
                return dialogueData;
            }

            // 找不到对话数据，返回null
            LogWarning($"未找到对话数据: {dialogueId}");
            return null;
        }

        /// <summary>
        /// 预加载指定的对话数据到缓存
        /// </summary>
        public void PreloadDialogueData(string dialogueId)
        {
            if (string.IsNullOrEmpty(dialogueId)) return;

            if (!dialogueCache.ContainsKey(dialogueId))
            {
                DialogueData data = GetDialogueData(dialogueId);
                if (data != null)
                {
                    dialogueCache[dialogueId] = data;
                    LogDebug($"预加载对话数据: {dialogueId}");
                }
            }
        }

        /// <summary>
        /// 预加载所有房间类型的对话数据
        /// </summary>
        public void PreloadAllRoomDialogues()
        {
            LogDebug("开始预加载所有房间对话数据");
            
            foreach (var mapping in roomDialogueMappings)
            {
                PreloadDialogueData(mapping.dialogueId);
            }
            
            LogDebug("预加载所有房间对话数据完成");
        }

        /// <summary>
        /// 清除对话缓存
        /// </summary>
        public void ClearCache()
        {
            dialogueCache.Clear();
            LogDebug("对话缓存已清除");
        }

        /// <summary>
        /// 添加自定义对话映射
        /// </summary>
        public void AddRoomDialogueMapping(RoomType_cza roomType, string dialogueId)
        {
            var existingMapping = roomDialogueMappings.Find(m => m.roomType == roomType);
            if (existingMapping != null)
            {
                existingMapping.dialogueId = dialogueId;
                LogDebug($"更新房间类型 {roomType} 的对话映射: {dialogueId}");
            }
            else
            {
                roomDialogueMappings.Add(new RoomDialogueMapping { roomType = roomType, dialogueId = dialogueId });
                LogDebug($"添加房间类型 {roomType} 的对话映射: {dialogueId}");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 根据房间类型获取对话ID
        /// </summary>
        private string GetDialogueIdForRoomType(RoomType_cza roomType)
        {
            var mapping = roomDialogueMappings.Find(m => m.roomType == roomType);
            return mapping?.dialogueId;
        }

        /// <summary>
        /// 从自定义对话数据列表中获取对话数据
        /// </summary>
        private DialogueData GetCustomDialogueData(string dialogueId)
        {
            foreach (var dialogueData in customDialogueData)
            {
                if (dialogueData != null && dialogueData.dialogueId == dialogueId)
                {
                    LogDebug($"从自定义数据获取对话: {dialogueId}");
                    return dialogueData;
                }
            }
            return null;
        }

        /// <summary>
        /// 从Resources加载对话数据
        /// </summary>
        private DialogueData LoadDialogueDataFromResources(string dialogueId)
        {
            // 首先尝试从Resources/Dialogues/目录加载
            string resourcePath = $"Dialogues/{dialogueId}";
            DialogueData dialogueData = Resources.Load<DialogueData>(resourcePath);
            
            if (dialogueData != null)
            {
                LogDebug($"从Resources加载对话数据: {resourcePath}");
                return dialogueData;
            }
            
            // 如果找不到，尝试从Data/Dialog/目录加载（兼容旧路径）
            string dataDialogPath = $"Data/Dialog/{dialogueId}";
            dialogueData = Resources.Load<DialogueData>(dataDialogPath);
            
            if (dialogueData != null)
            {
                LogDebug($"从Data/Dialog加载对话数据: {dataDialogPath}");
                return dialogueData;
            }
            
            return null;
        }

        /// <summary>
        /// 创建备用对话数据
        /// </summary>
        private DialogueData CreateFallbackDialogueData(string dialogueId)
        {
            DialogueData fallbackData = ScriptableObject.CreateInstance<DialogueData>();
            fallbackData.dialogueId = dialogueId;
            fallbackData.dialogueEntries = new DialogueEntry[]
            {
                new DialogueEntry
                {
                    speakerName = "系统",
                    dialogueText = $"对话数据缺失：{dialogueId}",
                    portraitPosition = UI_DialogView.PortraitPosition.None
                }
            };
            
            return fallbackData;
        }

        /// <summary>
        /// 根据敌人数据查找对应的对话数据
        /// </summary>
        /// <param name="enemyData">敌人数据</param>
        /// <returns>对应的对话数据，如果未找到则返回null</returns>
        private DialogueData FindDialogueDataForEnemy(EnemyData enemyData)
        {
            if (enemyData == null)
            {
                LogWarning("查找敌人对话数据失败：敌人数据为空");
                return null;
            }

            string enemyName = enemyData.DisplayName ?? enemyData.name;
            LogDebug($"查找敌人对话数据: {enemyName}");

            // 1. 首先检查自定义对话数据列表中的关联敌人
            foreach (var dialogueData in customDialogueData)
            {
                if (dialogueData != null)
                {
                    // 检查直接关联的敌人数据
                    if (dialogueData.associatedEnemy == enemyData)
                    {
                        LogDebug($"找到直接关联的对话数据: {dialogueData.dialogueId} -> {enemyName}");
                        return dialogueData;
                    }

                    // 检查敌人名称过滤器
                    if (!string.IsNullOrEmpty(dialogueData.enemyNameFilter) && 
                        enemyName.Contains(dialogueData.enemyNameFilter))
                    {
                        LogDebug($"找到名称匹配的对话数据: {dialogueData.dialogueId} -> {enemyName}");
                        return dialogueData;
                    }
                }
            }

            // 2. 从Resources文件夹加载敌人特定的对话数据
            string enemyDialogueId = $"Enemy_{enemyData.name.Replace(" ", "_")}";
            DialogueData enemyDialogue = LoadDialogueDataFromResources(enemyDialogueId);
            if (enemyDialogue != null)
            {
                LogDebug($"从Resources加载敌人对话数据: {enemyDialogueId}");
                return enemyDialogue;
            }

            // 3. 尝试加载通用敌人对话
            DialogueData genericEnemyDialogue = LoadDialogueDataFromResources("Enemy_Generic");
            if (genericEnemyDialogue != null)
            {
                LogDebug($"使用通用敌人对话数据: Enemy_Generic");
                return genericEnemyDialogue;
            }

            LogWarning($"未找到敌人 {enemyName} 对应的对话数据");
            return null;
        }

        /// <summary>
        /// 根据房间类型和敌人数据获取对话数据
        /// </summary>
        /// <param name="roomType">房间类型</param>
        /// <param name="enemyData">敌人数据</param>
        /// <returns>对话数据</returns>
        private DialogueData GetDialogueForRoomWithEnemy(RoomType_cza roomType, EnemyData enemyData)
        {
            // 1. 首先尝试获取敌人特定的对话数据
            DialogueData enemyDialogue = FindDialogueDataForEnemy(enemyData);
            if (enemyDialogue != null)
            {
                return enemyDialogue;
            }

            // 2. 回退到房间类型的默认对话
            DialogueData roomDialogue = GetDialogueForRoom(roomType);
            if (roomDialogue != null)
            {
                LogDebug($"使用房间类型默认对话: {roomType}");
                return roomDialogue;
            }

            // 3. 找不到任何对话数据，返回null
            LogWarning($"未找到房间 {roomType} 和敌人 {enemyData?.DisplayName} 的对话数据");
            return null;
        }

        /// <summary>
        /// 缓存对话数据
        /// </summary>
        private void CacheDialogueData(string dialogueId, DialogueData dialogueData)
        {
            if (enableDialogueCaching && dialogueData != null)
            {
                dialogueCache[dialogueId] = dialogueData;
            }
        }

        /// <summary>
        /// 预加载自定义对话数据
        /// </summary>
        private void PreloadCustomDialogueData()
        {
            foreach (var dialogueData in customDialogueData)
            {
                if (dialogueData != null && !string.IsNullOrEmpty(dialogueData.dialogueId))
                {
                    CacheDialogueData(dialogueData.dialogueId, dialogueData);
                    LogDebug($"预加载自定义对话数据: {dialogueData.dialogueId}");
                }
            }
        }

        #endregion

        #region 日志方法

        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[DialogControllerService] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[DialogControllerService] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[DialogControllerService] {message}");
        }

        #endregion
    }

    /// <summary>
    /// 房间对话映射结构
    /// </summary>
    [System.Serializable]
    public class RoomDialogueMapping
    {
        public RoomType_cza roomType;
        public string dialogueId;
    }
}
