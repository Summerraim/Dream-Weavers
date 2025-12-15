using System.Collections.Generic;
using UnityEngine;

namespace DreamWeavers.Rooms
{
    /// <summary>
    /// 技能房：进入房间后从 SkillPool 随机生成一个技能，
    /// - 根据技能池的精灵映射，自动将技能添加到对应精灵的技能列表
    /// - 可在房间中生成一个技能展示（可选）
    /// </summary>
    public class SkillRoom_cza : RoomBase_cza
    {
        [Header("技能池配置")]
        [Tooltip("技能池资源，用于随机抽取技能")]
        [SerializeField] private SkillPool skillPool;
        
        [Tooltip("是否使用权重随机（如果技能池配置了权重）")]
        [SerializeField] private bool useWeightedRandom = false;

        [Header("玩家数据")]
        [Tooltip("玩家数据引用，用于获取玩家拥有的精灵")]
        [SerializeField] private PlayerData playerData;

        [Header("展示设置")]
        [Tooltip("技能展示生成位置（可选）")]
        [SerializeField] private Transform spawnPoint;
        
        [Tooltip("技能展示预制体（可选）")]
        [SerializeField] private GameObject skillDisplayPrefab;

        [Header("自动获取设置")]
        [Tooltip("进入房间时自动将技能添加给玩家对应的精灵")]
        [SerializeField] private bool autoGrantOnEnter = true;

        [Header("UI 按钮绑定")]
        [Tooltip("获取技能按钮（可选）。若未手动绑定，将在运行时按名称尝试自动查找并绑定。")]
        [SerializeField] private UnityEngine.UI.Button getSkillButton;

        // 运行时状态
        private ISkill selectedSkill;
        private ScriptableObject selectedSkillData;
        private SpiritData matchedSpirit; // 匹配到的精灵
        private GameObject displayInstance;
        private bool granted;

        private void Awake()
        {
            Type = RoomType_cza.Skill;
            
            Debug.Log("[SkillRoom] Awake: start binding GetSkill button and validating references");
            Debug.Log($"[SkillRoom] Refs -> skillPool={(skillPool != null)}, spawnPoint={(spawnPoint != null)}, skillDisplayPrefab={(skillDisplayPrefab != null)}, autoGrantOnEnter={autoGrantOnEnter}");
            
            // 自动绑定获取按钮（名称包含 GetSkill）
            if (getSkillButton == null)
            {
                var btns = GetComponentsInChildren<UnityEngine.UI.Button>(true);
                foreach (var b in btns)
                {
                    var n = b.gameObject.name;
                    if (!string.IsNullOrEmpty(n) && n.IndexOf("GetSkill", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        getSkillButton = b;
                        break;
                    }
                }
            }
            
            if (getSkillButton != null)
            {
                getSkillButton.onClick.RemoveListener(OnClickGetSkill);
                getSkillButton.onClick.AddListener(OnClickGetSkill);
                Debug.Log($"[SkillRoom] Bound GetSkill button -> {getSkillButton.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("[SkillRoom] GetSkill button not found in children (name contains 'GetSkill'). You can bind it via Inspector.");
            }
        }

        public override void EnterRoom()
        {
            Debug.Log("[SkillRoom] EnterRoom");
            granted = false;
            matchedSpirit = null;
            
            // 从技能池中随机抽取技能，并匹配对应的精灵
            PickSkillFromPoolAndMatchSpirit();

            // 可选：在房间中生成技能展示
            SpawnSkillDisplay();

            // 自动发放技能给对应的精灵
            if (autoGrantOnEnter)
            {
                GrantSkillToMatchedSpirit();
            }
            else if (getSkillButton == null)
            {
                Debug.LogWarning("[SkillRoom] 未找到 GetSkill 按钮。可在 Inspector 绑定或将按钮命名包含 'GetSkill' 以便自动绑定。");
            }
            
            string spiritName = matchedSpirit != null ? matchedSpirit.DisplayName : "null";
            Debug.Log($"[SkillRoom] EnterRoom done: selectedSkill={(selectedSkill != null ? selectedSkill.DisplayName : "null")}, matchedSpirit={spiritName}, granted={granted}");
        }

        public override void ExitRoom()
        {
            Debug.Log("[SkillRoom] ExitRoom");
            
            // 清理展示物
            if (displayInstance != null)
            {
                Destroy(displayInstance);
                displayInstance = null;
            }
        }

        /// <summary>
        /// 从技能池中随机抽取一个技能，并根据精灵名称映射匹配玩家拥有的精灵
        /// </summary>
        private void PickSkillFromPoolAndMatchSpirit()
        {
            selectedSkill = null;
            selectedSkillData = null;
            matchedSpirit = null;
            
            if (skillPool == null || skillPool.IsEmpty)
            {
                Debug.LogWarning($"[SkillRoom] skillPool 未配置或为空，无法抽取技能 (skillPool={(skillPool != null)} IsEmpty={(skillPool != null ? skillPool.IsEmpty : true)})");
                return;
            }

            // 获取玩家拥有的所有精灵
            List<SpiritData> playerSpirits = GetPlayerOwnedSpirits();
            if (playerSpirits == null || playerSpirits.Count == 0)
            {
                Debug.LogWarning("[SkillRoom] 玩家没有拥有任何精灵，无法匹配技能");
                return;
            }

            // 获取技能池中精灵名称与技能的映射
            var spiritSkillMapping = skillPool.GetSpiritSkillMapping();
            if (spiritSkillMapping == null || spiritSkillMapping.Count == 0)
            {
                Debug.LogWarning("[SkillRoom] 技能池未配置精灵映射，将使用随机抽取");
                // 退回到普通随机抽取
                FallbackRandomPick();
                return;
            }

            // 找出玩家拥有的精灵中，在技能池中有对应技能的精灵
            List<SpiritData> matchableSpirits = new List<SpiritData>();
            foreach (var spirit in playerSpirits)
            {
                if (spirit == null) continue;
                string spiritName = string.IsNullOrWhiteSpace(spirit.DisplayName) ? spirit.name : spirit.DisplayName;
                if (spiritSkillMapping.ContainsKey(spiritName))
                {
                    matchableSpirits.Add(spirit);
                }
            }

            if (matchableSpirits.Count == 0)
            {
                Debug.LogWarning("[SkillRoom] 玩家拥有的精灵中没有与技能池匹配的，将使用随机抽取");
                FallbackRandomPick();
                return;
            }

            // 从可匹配的精灵中随机选择一个
            int randomIndex = Random.Range(0, matchableSpirits.Count);
            matchedSpirit = matchableSpirits[randomIndex];
            string matchedSpiritName = string.IsNullOrWhiteSpace(matchedSpirit.DisplayName) ? matchedSpirit.name : matchedSpirit.DisplayName;

            // 获取该精灵对应的技能
            selectedSkillData = spiritSkillMapping[matchedSpiritName];
            
            // 转换为 ISkill
            if (selectedSkillData is ISkill skill)
            {
                selectedSkill = skill;
            }
            else if (selectedSkillData is SkillData skillData)
            {
                selectedSkill = new Skill(skillData);
            }

            if (selectedSkill != null)
            {
                Debug.Log($"[SkillRoom] 抽取到技能: {selectedSkill.DisplayName}，对应精灵: {matchedSpiritName}");
            }
            else
            {
                Debug.LogWarning("[SkillRoom] 未能从技能池中抽取到有效技能");
            }
        }

        /// <summary>
        /// 退回到普通随机抽取（当没有精灵映射时使用）
        /// </summary>
        private void FallbackRandomPick()
        {
            if (useWeightedRandom)
            {
                selectedSkill = skillPool.GetWeightedRandomISkill();
                selectedSkillData = skillPool.GetWeightedRandomSkill();
            }
            else
            {
                selectedSkill = skillPool.GetRandomISkill();
                selectedSkillData = skillPool.GetRandomSkill();
            }

            if (selectedSkill != null)
            {
                Debug.Log($"[SkillRoom] (随机抽取) 抽取到技能: {selectedSkill.DisplayName}");
            }
        }

        /// <summary>
        /// 获取玩家拥有的所有精灵
        /// </summary>
        private List<SpiritData> GetPlayerOwnedSpirits()
        {
            // 从 PlayerData 获取
            if (playerData != null)
            {
                var spirits = playerData.GetOwnedSpirits();
                if (spirits != null && spirits.Count > 0)
                {
                    Debug.Log($"[SkillRoom] 从 PlayerData 获取到 {spirits.Count} 个精灵");
                    return spirits;
                }
            }

            Debug.LogWarning("[SkillRoom] PlayerData 未配置或没有精灵数据");
            return new List<SpiritData>();
        }

        /// <summary>
        /// 在房间中生成技能展示物（可选）
        /// </summary>
        private void SpawnSkillDisplay()
        {
            if (skillDisplayPrefab == null || spawnPoint == null)
            {
                return;
            }

            if (selectedSkill == null)
            {
                Debug.LogWarning("[SkillRoom] 无法生成展示：未抽取到有效技能");
                return;
            }

            displayInstance = Instantiate(skillDisplayPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            Debug.Log($"[SkillRoom] 已生成技能展示物: {selectedSkill.DisplayName}");
            
            // TODO: 可以在这里设置展示物的UI显示，如技能名称、图标等
            // 例如：displayInstance.GetComponent<SkillDisplayUI>()?.Setup(selectedSkill);
        }

        /// <summary>
        /// 点击获取技能按钮的回调
        /// </summary>
        private void OnClickGetSkill()
        {
            if (granted)
            {
                Debug.Log("[SkillRoom] 技能已被获取，无法重复获取");
                return;
            }
            
            GrantSkillToMatchedSpirit();
        }

        /// <summary>
        /// 将技能添加给匹配的精灵
        /// </summary>
        private void GrantSkillToMatchedSpirit()
        {
            if (selectedSkillData == null)
            {
                Debug.LogWarning("[SkillRoom] 无技能可添加");
                return;
            }

            if (granted)
            {
                Debug.Log("[SkillRoom] 技能已被获取");
                return;
            }

            if (matchedSpirit != null)
            {
                // 将技能添加到匹配的精灵的 Skills 数组
                bool success = AddSkillToSpiritData(matchedSpirit, selectedSkillData);
                if (success)
                {
                    granted = true;
                    string skillName = selectedSkill != null ? selectedSkill.DisplayName : selectedSkillData.name;
                    string spiritName = string.IsNullOrWhiteSpace(matchedSpirit.DisplayName) ? matchedSpirit.name : matchedSpirit.DisplayName;
                    Debug.Log($"[SkillRoom] 成功将技能 [{skillName}] 添加给精灵 [{spiritName}]");
                    
                    // 禁用获取按钮
                    if (getSkillButton != null)
                    {
                        getSkillButton.interactable = false;
                    }
                }
                else
                {
                    Debug.LogWarning($"[SkillRoom] 无法将技能添加给精灵");
                }
            }
            else
            {
                Debug.LogWarning("[SkillRoom] 没有匹配的精灵，无法添加技能");
            }
        }

        /// <summary>
        /// 将技能添加到 SpiritData 的 Skills 数组
        /// </summary>
        private bool AddSkillToSpiritData(SpiritData spiritData, ScriptableObject skillData)
        {
            if (spiritData == null || skillData == null)
            {
                return false;
            }

            try
            {
                // 获取当前技能列表
                var currentSkills = spiritData.Skills;
                
                // 检查技能是否已存在
                if (currentSkills != null)
                {
                    foreach (var existingSkill in currentSkills)
                    {
                        if (existingSkill == skillData)
                        {
                            Debug.Log($"[SkillRoom] 技能已存在于精灵的技能列表中");
                            return true; // 视为成功，因为技能已经存在
                        }
                    }
                }

                // 创建新的技能数组，包含原有技能和新技能
                int newLength = (currentSkills?.Length ?? 0) + 1;
                var newSkills = new ScriptableObject[newLength];
                
                // 复制原有技能
                if (currentSkills != null)
                {
                    for (int i = 0; i < currentSkills.Length; i++)
                    {
                        newSkills[i] = currentSkills[i];
                    }
                }
                
                // 添加新技能
                newSkills[newLength - 1] = skillData;
                
                // 更新精灵的技能数组
                spiritData.Skills = newSkills;
                
                string spiritName = string.IsNullOrWhiteSpace(spiritData.DisplayName) ? spiritData.name : spiritData.DisplayName;
                Debug.Log($"[SkillRoom] 已将技能添加到精灵 [{spiritName}] 的技能列表，当前技能数量: {newLength}");
                
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SkillRoom] 添加技能时发生错误: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取当前选中的技能（供外部查询）
        /// </summary>
        public ISkill GetSelectedSkill()
        {
            return selectedSkill;
        }

        /// <summary>
        /// 获取当前选中的技能数据（供外部查询）
        /// </summary>
        public ScriptableObject GetSelectedSkillData()
        {
            return selectedSkillData;
        }

        /// <summary>
        /// 获取匹配的精灵（供外部查询）
        /// </summary>
        public SpiritData GetMatchedSpirit()
        {
            return matchedSpirit;
        }

        /// <summary>
        /// 检查技能是否已被获取
        /// </summary>
        public bool IsSkillGranted()
        {
            return granted;
        }
    }
}
