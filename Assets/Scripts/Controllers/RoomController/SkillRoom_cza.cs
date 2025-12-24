using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        [SerializeField]
        private SkillPool skillPool;

        [Tooltip("是否使用权重随机（如果技能池配置了权重）")]
        [SerializeField]
        private bool useWeightedRandom = false;

        [Header("玩家数据")]
        [Tooltip("玩家数据引用，用于获取玩家拥有的精灵")]
        [SerializeField]
        private PlayerData playerData;

        [Header("展示设置")]
        [Tooltip("技能展示生成位置（可选）")]
        [SerializeField]
        private Transform spawnPoint;

        [Tooltip("技能展示预制体（可选）")]
        [SerializeField] private GameObject skillDisplayPrefab;

        [Header("UI 按钮绑定")]
        [Tooltip("获取技能按钮（可选）。若未手动绑定，将在运行时按名称尝试自动查找并绑定。")]
        [SerializeField] private UnityEngine.UI.Button getSkillButton;
        [Tooltip("按钮文本（可选）。未绑定时会自动查找按钮下的 TMP_Text 或 Text 组件")]
        [SerializeField] private TMP_Text getSkillButtonLabel;
        [SerializeField] private string defaultGetSkillButtonText = string.Empty;
        [Tooltip("显示抽取到技能名称的文本（可选）")]
        [SerializeField] private TMP_Text acquiredSkillNameText;
        [SerializeField] private string defaultAcquiredSkillNameText = string.Empty;

        // 运行时状态
        private ISkill selectedSkill;
        private ScriptableObject selectedSkillData;
        private SpiritData matchedSpirit; // 匹配到的精灵
        private GameObject displayInstance;
        private bool granted;
        private Text legacyGetSkillButtonLabel;
        private Text legacyAcquiredSkillNameText;
        // 备份原始技能列表，运行结束时还原
        private readonly Dictionary<SpiritData, ScriptableObject[]> originalSkillsBackup = new Dictionary<SpiritData, ScriptableObject[]>();

        private void Awake()
        {
            Type = RoomType_cza.Skill;

            Debug.Log("[SkillRoom] Awake: start binding GetSkill button and validating references");
            Debug.Log($"[SkillRoom] Refs -> skillPool={(skillPool != null)}, spawnPoint={(spawnPoint != null)}, skillDisplayPrefab={(skillDisplayPrefab != null)}");
            
            // 自动绑定获取按钮（名称包含 GetSkill）
            if (getSkillButton == null)
            {
                var btns = GetComponentsInChildren<UnityEngine.UI.Button>(true);
                foreach (var b in btns)
                {
                    var n = b.gameObject.name;
                    if (
                        !string.IsNullOrEmpty(n)
                        && n.IndexOf("GetSkill", System.StringComparison.OrdinalIgnoreCase) >= 0
                    )
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
                Debug.LogWarning(
                    "[SkillRoom] GetSkill button not found in children (name contains 'GetSkill'). You can bind it via Inspector."
                );
            }

            CacheGetSkillButtonLabel();
            CacheAcquiredSkillNameLabel();
        }

        public override void EnterRoom()
        {
            Debug.Log("[SkillRoom] EnterRoom");
            granted = false;
            matchedSpirit = null;
            if (getSkillButton != null)
            {
                getSkillButton.interactable = true;
            }
            UpdateGetSkillButtonText(defaultGetSkillButtonText);
            UpdateAcquiredSkillNameText(defaultAcquiredSkillNameText);
            
            // 从技能池中随机抽取技能，并匹配对应的精灵
            PickSkillFromPoolAndMatchSpirit();
            getSkillButton.interactable = true;
            // 可选：在房间中生成技能展示
            SpawnSkillDisplay();

            // 若没有按钮可触发获取，则直接发放以避免卡关
            if (getSkillButton == null)
            {
                Debug.LogWarning("[SkillRoom] 未找到 GetSkill 按钮，已自动发放技能。可在 Inspector 绑定或将按钮命名包含 'GetSkill' 以便自动绑定。");
                GrantSkillToMatchedSpirit();
            }
            
            string spiritName = matchedSpirit != null ? matchedSpirit.DisplayName : "null";
            Debug.Log(
                $"[SkillRoom] EnterRoom done: selectedSkill={(selectedSkill != null ? selectedSkill.DisplayName : "null")}, matchedSpirit={spiritName}, granted={granted}"
            );
        }

        public override void ExitRoom()
        {
            Debug.Log("[SkillRoom] ExitRoom");
            getSkillButton.interactable = true;
            
            
            // 清理展示物
            if (displayInstance != null)
            {
                Destroy(displayInstance);
                displayInstance = null;
            }
        }

        private void OnDestroy()
        {
            RestoreOriginalSkills();
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
                Debug.LogWarning(
                    $"[SkillRoom] skillPool 未配置或为空，无法抽取技能 (skillPool={(skillPool != null)} IsEmpty={(skillPool != null ? skillPool.IsEmpty : true)})"
                );
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
                if (spirit == null)
                    continue;
                string spiritName = string.IsNullOrWhiteSpace(spirit.DisplayName)
                    ? spirit.name
                    : spirit.DisplayName;
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
            string matchedSpiritName = string.IsNullOrWhiteSpace(matchedSpirit.DisplayName)
                ? matchedSpirit.name
                : matchedSpirit.DisplayName;

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
                Debug.Log(
                    $"[SkillRoom] 抽取到技能: {selectedSkill.DisplayName}，对应精灵: {matchedSpiritName}"
                );
            }
            else
            {
                Debug.LogWarning("[SkillRoom] 未能从技能池中抽取到有效技能");
            }

            ApplyButtonLabelFromSkill();
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

            ApplyButtonLabelFromSkill();
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

            displayInstance = Instantiate(
                skillDisplayPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                spawnPoint
            );
            Debug.Log($"[SkillRoom] 已生成技能展示物: {selectedSkill.DisplayName}");

            // TODO: 可以在这里设置展示物的UI显示，如技能名称、图标等
            // 例如：displayInstance.GetComponent<SkillDisplayUI>()?.Setup(selectedSkill);
        }

        /// <summary>
        /// 点击获取技能按钮的回调
        /// </summary>
        private void OnClickGetSkill()
        {
            if (selectedSkillData == null)
            {
                Debug.LogWarning("[SkillRoom] 点击按钮时未找到已抽取的技能，尝试重新抽取");
                PickSkillFromPoolAndMatchSpirit();
                if (selectedSkillData == null)
                {
                    Debug.LogWarning("[SkillRoom] 重新抽取仍然失败，无法发放技能");
                    return;
                }
            }

            // 如果技能尚未获取，先获取技能
            if (!granted)
            {
                GrantSkillToMatchedSpirit();
            }
            else
            {
                Debug.Log("[SkillRoom] 技能已被获取（可能是autoGrantOnEnter），直接进入路线选择");
            }

            // 禁用按钮
            if (getSkillButton != null)
            {
                getSkillButton.interactable = false;
            }

            // 触发路线选择
            if (RoomStateMachine_cza.Instance != null)
            {
                Debug.Log("[SkillRoom] 技能获取完成，触发路线选择");
                RoomStateMachine_cza.Instance.CompleteCurrentRoom();
            }
            else
            {
                Debug.LogWarning(
                    "[SkillRoom] RoomStateMachine_cza.Instance 为 null，无法触发路线选择"
                );
            }
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
                // 首次修改前备份原始技能，方便退出时还原
                BackupSkillsIfNeeded(matchedSpirit);

                // 将技能添加到匹配的精灵的 Skills 数组（直接作用于原始 ScriptableObject）
                bool success = AddSkillToSpiritData(matchedSpirit, selectedSkillData);
                if (success)
                {
                    granted = true;
                    string skillName =
                        selectedSkill != null ? selectedSkill.DisplayName : selectedSkillData.name;
                    string spiritName = string.IsNullOrWhiteSpace(matchedSpirit.DisplayName)
                        ? matchedSpirit.name
                        : matchedSpirit.DisplayName;
                    Debug.Log($"[SkillRoom] 成功将技能 [{skillName}] 添加给精灵 [{spiritName}] | 当前技能列表={FormatSkills(matchedSpirit.Skills)}");
                    UpdateAcquiredSkillNameText(skillName);
                    
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
                Debug.Log($"[SkillRoom] AddSkillToSpiritData: Spirit={spiritData.name} Before={FormatSkills(currentSkills)} Adding={skillData.name}");

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

                string spiritName = string.IsNullOrWhiteSpace(spiritData.DisplayName)
                    ? spiritData.name
                    : spiritData.DisplayName;
                Debug.Log(
                    $"[SkillRoom] 已将技能添加到精灵 [{spiritName}] 的技能列表，当前技能数量: {newLength} | Now={FormatSkills(spiritData.Skills)}"
                );

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

        private void CacheGetSkillButtonLabel()
        {
            if (getSkillButton == null)
                return;

            if (getSkillButtonLabel == null)
            {
                getSkillButtonLabel = getSkillButton.GetComponentInChildren<TMP_Text>(true);
            }

            if (getSkillButtonLabel == null && legacyGetSkillButtonLabel == null)
            {
                legacyGetSkillButtonLabel = getSkillButton.GetComponentInChildren<Text>(true);
            }

            if (!string.IsNullOrEmpty(defaultGetSkillButtonText))
                return;

            if (getSkillButtonLabel != null)
            {
                defaultGetSkillButtonText = getSkillButtonLabel.text;
            }
            else if (legacyGetSkillButtonLabel != null)
            {
                defaultGetSkillButtonText = legacyGetSkillButtonLabel.text;
            }
            else
            {
                defaultGetSkillButtonText = "获取技能";
            }
        }

        private void CacheAcquiredSkillNameLabel()
        {
            if (acquiredSkillNameText == null)
            {
                var tmpTexts = GetComponentsInChildren<TMP_Text>(true);
                foreach (var text in tmpTexts)
                {
                    var n = text.gameObject.name;
                    if (!string.IsNullOrEmpty(n) && n.IndexOf("SkillName", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        acquiredSkillNameText = text;
                        break;
                    }
                }
            }

            if (acquiredSkillNameText == null && legacyAcquiredSkillNameText == null)
            {
                var uiTexts = GetComponentsInChildren<Text>(true);
                foreach (var text in uiTexts)
                {
                    var n = text.gameObject.name;
                    if (!string.IsNullOrEmpty(n) && n.IndexOf("SkillName", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        legacyAcquiredSkillNameText = text;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(defaultAcquiredSkillNameText))
                return;

            if (acquiredSkillNameText != null)
            {
                defaultAcquiredSkillNameText = acquiredSkillNameText.text;
            }
            else if (legacyAcquiredSkillNameText != null)
            {
                defaultAcquiredSkillNameText = legacyAcquiredSkillNameText.text;
            }
            else
            {
                defaultAcquiredSkillNameText = string.Empty;
            }
        }

        private void UpdateGetSkillButtonText(string text)
        {
            string finalText = string.IsNullOrWhiteSpace(text) ? defaultGetSkillButtonText : text;
            if (getSkillButtonLabel != null)
            {
                getSkillButtonLabel.text = finalText;
            }
            else if (legacyGetSkillButtonLabel != null)
            {
                legacyGetSkillButtonLabel.text = finalText;
            }
        }

        private void UpdateAcquiredSkillNameText(string text)
        {
            string namePart = string.IsNullOrWhiteSpace(text) ? defaultAcquiredSkillNameText : text;
            string finalText = string.IsNullOrWhiteSpace(namePart) ? string.Empty : $"获得技能：{namePart}！";
            if (acquiredSkillNameText != null)
            {
                acquiredSkillNameText.text = finalText;
            }
            else if (legacyAcquiredSkillNameText != null)
            {
                legacyAcquiredSkillNameText.text = finalText;
            }
        }

        // 首次修改前备份原始技能列表
        private void BackupSkillsIfNeeded(SpiritData spirit)
        {
            if (spirit == null)
                return;
            if (originalSkillsBackup.ContainsKey(spirit))
                return;

            var backup = spirit.Skills != null ? (ScriptableObject[])spirit.Skills.Clone() : null;
            originalSkillsBackup[spirit] = backup;
            Debug.Log($"[SkillRoom] 备份技能列表: Spirit={spirit.name} 原始技能={FormatSkills(backup)}");
        }

        // 退出/销毁时恢复原始技能列表
        private void RestoreOriginalSkills()
        {
            if (originalSkillsBackup.Count == 0)
                return;

            foreach (var kv in originalSkillsBackup)
            {
                var spirit = kv.Key;
                var backup = kv.Value;
                spirit.Skills = backup;
                Debug.Log($"[SkillRoom] 恢复技能列表: Spirit={spirit.name} -> {FormatSkills(backup)}");
            }

            originalSkillsBackup.Clear();
            Debug.Log("[SkillRoom] 已恢复 SpiritData 的技能列表到运行前状态");
        }

        private string FormatSkills(ScriptableObject[] skills)
        {
            if (skills == null || skills.Length == 0)
                return "(empty)";

            var names = new List<string>(skills.Length);
            for (int i = 0; i < skills.Length; i++)
            {
                var s = skills[i];
                names.Add(s != null ? s.name : "null");
            }
            return string.Join(",", names);
        }

        private void ApplyButtonLabelFromSkill()
        {
            string label = null;
            if (selectedSkill != null)
            {
                label = selectedSkill.DisplayName;
            }
            else if (selectedSkillData != null)
            {
                label = selectedSkillData.name;
            }

            UpdateAcquiredSkillNameText(label);
        }
    }
}
