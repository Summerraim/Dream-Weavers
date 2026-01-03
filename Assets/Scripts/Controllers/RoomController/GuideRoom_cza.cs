using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DreamWeavers.Rooms
{
	/// <summary>
	/// 新手引导房间：独立于地图生成，在正式进入楼层之前运行。
	/// 玩家从两个备选 SpiritData 中选择其一，
	/// - 选中的 Spirit 添加到玩家数据并部署
	/// - 未选中的 Spirit 所绑定的 EnemyData 作为引导战的敌人
	/// 战斗结束后回调通知外部（MapManager）继续正常楼层流程。
	/// </summary>
	public class GuideRoom_cza : RoomBase_cza
	{
		[Header("玩家与战斗引用")]
		// PlayerData 现在从 PlayerManager 获取，无需手动赋予
		[SerializeField] private BattleController battleController;
		[SerializeField] private UI_BattleView battleView;

		[Header("胜利后：路线/奖励")]
		[Tooltip("引导战胜利后初始化的楼层（默认1）")]
		[SerializeField] private int floorToInitAfterGuide = 1;

		[Tooltip("胜利后触发的对话数据（可选，优先使用此字段）")]
		[SerializeField] private DialogueData victoryDialogueData;

		[Tooltip("胜利后触发的对话ID（可选；当 victoryDialogueData 未配置时，尝试通过 DialogControllerService 获取）")]
		[SerializeField] private string victoryDialogueId = string.Empty;

		[Tooltip("胜利后发放的技能数据（可选，支持 SkillData 或实现 ISkill 的 ScriptableObject）")]
		[SerializeField] private ScriptableObject victorySkillData;

		[Tooltip("将胜利技能发放给这些精灵（按 DisplayName 匹配，大小写不敏感）")]
		[SerializeField] private string[] victorySkillTargetSpiritDisplayNames;

		[Header("引导选项（索引0/1）")]
		[SerializeField] private SpiritData[] candidateSpirits = new SpiritData[2];
		[SerializeField] private EnemyData[] enemyMappings = new EnemyData[2];

		[Header("可选：按钮绑定（用于UI点击选择）")]
		[SerializeField] private Button option0Button;
		[SerializeField] private Button option1Button;
		[SerializeField] private GameObject selectionPanel;
		[SerializeField] private Button confirmButton;

		private Action onGuideFinished;
		[Tooltip("是否已启动引导，可在Inspector中手动清零以重玩引导")]
		public bool guideStarted;
		private bool guideCompleted;
		private bool battleStarted;
		private bool battleEnded;
		private bool battleVictory;
		private int chosenIndex = -1;
		private SpiritData chosenSpiritPending;
		private SpiritData unchosenSpirit; // 未被选择的Spirit，战斗胜利后也会添加到玩家背包
		private EnemyData enemyPending;

		private void Awake()
		{
			Type = RoomType_cza.Guide; // 仅用于标记，无实际地图参与

			// 绑定按钮事件（若存在）
			if (option0Button != null)
			{
				option0Button.onClick.RemoveListener(OnClickOption0);
				option0Button.onClick.AddListener(OnClickOption0);
			}
			if (option1Button != null)
			{
				option1Button.onClick.RemoveListener(OnClickOption1);
				option1Button.onClick.AddListener(OnClickOption1);
			}

			if (confirmButton != null)
			{
				confirmButton.onClick.RemoveListener(OnClickConfirm);
				confirmButton.onClick.AddListener(OnClickConfirm);
			}
		}

		private void Update()
		{
			// 监听战斗结果，一旦结束即触发回调并退出引导
			if (battleStarted && !battleEnded && battleController != null)
			{
				if (battleController.State == BattleState.Victory || battleController.State == BattleState.Defeat)
				{
					battleEnded = true;
					battleVictory = battleController.State == BattleState.Victory;

					// 尝试收起战斗UI，避免留在屏幕上
					if (battleView != null)
					{
						battleView.HideEnemyDeathPanel();
						battleView.HideCapturePanel();
						battleView.HideBattlePanel();
					}

					FinishGuide(battleVictory);
				}
			}
		}

		/// <summary>
		/// MapManager在游戏开始时调用，启动引导房流程。
		/// </summary>
		public void StartGuide(Action onFinished)
		{
			if (guideCompleted)
			{
				onFinished?.Invoke();
				return;
			}

			onGuideFinished = onFinished;
			EnterRoom();
		}

		public override void EnterRoom()
		{
			if (guideStarted)
			{
				Debug.Log("[GuideRoom] 已经启动过引导，跳过重复进入");
				return;
			}

			guideStarted = true;
			Debug.Log("[GuideRoom] 进入新手引导房间，等待玩家选择精灵");

			if (selectionPanel != null)
			{
				selectionPanel.SetActive(true);
			}

			// 如果没有UI按钮，默认选择第0个
			if (option0Button == null && option1Button == null)
			{
				Debug.Log("[GuideRoom] 未绑定按钮，使用默认选项0");
				SelectOption(0);
			}
		}

		public override void ExitRoom()
		{
			// 引导房独立存在，无需特殊退出逻辑
		}

		private void OnClickOption0() => SelectOption(0);
		private void OnClickOption1() => SelectOption(1);
		private void OnClickConfirm()
		{
			if (chosenIndex < 0)
			{
				if (candidateSpirits.Length >= 1 && enemyMappings.Length >= 1)
				{
					Debug.Log("[GuideRoom] 未选择，默认使用选项0");
					SelectOption(0);
				}
				else
				{
					Debug.LogError("[GuideRoom] 未选择且候选数据不足，无法开始");
					return;
				}
			}

			if (selectionPanel != null)
			{
				selectionPanel.SetActive(false);
			}

			if (chosenSpiritPending == null || enemyPending == null)
			{
				Debug.LogError("[GuideRoom] 确认时数据缺失，无法开始引导战斗");
				return;
			}

			AddSpiritToPlayer(chosenSpiritPending);
			StartGuideBattle(enemyPending);
			chosenSpiritPending = null;
			enemyPending = null;
		}

		private void SelectOption(int index)
		{
			if (guideCompleted || battleStarted)
			{
				Debug.Log("[GuideRoom] 已开始或完成，引导选择被忽略");
				return;
			}

			if (index < 0 || index > 1)
			{
				Debug.LogError("[GuideRoom] 选项索引非法，必须为0或1");
				return;
			}

			if (candidateSpirits.Length < 2 || enemyMappings.Length < 2)
			{
				Debug.LogError("[GuideRoom] 请在Inspector中配置两组 SpiritData 与 EnemyData 映射");
				return;
			}

			var chosenSpirit = candidateSpirits[index];
			var enemyData = enemyMappings[1 - index];

			if (chosenSpirit == null || enemyData == null)
			{
				Debug.LogError("[GuideRoom] 选中的Spirit或对应敌人未配置");
				return;
			}

			chosenIndex = index;
			chosenSpiritPending = chosenSpirit;
			unchosenSpirit = candidateSpirits[1 - index]; // 保存未选择的Spirit
			enemyPending = enemyData;

			// 先将两个按钮都恢复为可交互状态
			if (option0Button != null)
			{
				option0Button.interactable = true;
			}
			if (option1Button != null)
			{
				option1Button.interactable = true;
			}

			// 只将被点击的按钮设置为灰色（不可交互），让玩家知道选择了哪个
			if (index == 0 && option0Button != null)
			{
				option0Button.interactable = false;
			}
			else if (index == 1 && option1Button != null)
			{
				option1Button.interactable = false;
			}

			bool deferStart = confirmButton != null || selectionPanel != null;
			if (deferStart)
			{
				Debug.Log("[GuideRoom] 已选择精灵，等待确认按钮开始战斗");
				return;
			}

			// 无确认按钮/面板时立即开始
			AddSpiritToPlayer(chosenSpirit);
			StartGuideBattle(enemyData);
			chosenSpiritPending = null;
			enemyPending = null;
		}

		/// <summary>
		/// 获取PlayerData（优先从PlayerManager，降级到本地引用）
		/// </summary>
		private PlayerData GetPlayerData()
		{
			if (PlayerManager.Instance != null && PlayerManager.Instance.CurrentPlayerData != null)
			{
				return PlayerManager.Instance.CurrentPlayerData;
			}

			Debug.LogWarning("[GuideRoom] PlayerManager.Instance 或 CurrentPlayerData 为 null，无法获取 PlayerData");
			return null;
		}

		private void AddSpiritToPlayer(SpiritData spirit)
		{
			// 更新运行时 PlayerManager
			if (PlayerManager.Instance != null)
			{
				PlayerManager.Instance.CaptureSpirit(spirit);
				PlayerManager.Instance.DeploySpirit(spirit);
			}

			// 同步到 PlayerData（Owned 与 Deployed 只保留该引导精灵）
			var playerData = GetPlayerData();
			if (playerData != null)
			{
				playerData.OwnedSpirits = new[] { spirit };
				playerData.DeployedSpirits = new[] { spirit };
			}
		}

		/// <summary>
		/// 将未选择的Spirit添加到玩家背包，但不部署
		/// </summary>
		private void AddUnchosenSpiritToPlayer(SpiritData spirit)
		{
			// 更新运行时 PlayerManager
			if (PlayerManager.Instance != null)
			{
				PlayerManager.Instance.CaptureSpirit(spirit);
				// 注意：这里不调用 DeploySpirit，只添加到背包
			}

			// 同步到 PlayerData：将两个Spirit都添加到 OwnedSpirits
			var playerData = GetPlayerData();
			if (playerData != null)
			{
				var currentOwned = playerData.GetOwnedSpirits();
				var ownedList = new List<SpiritData>(currentOwned);
				if (!ownedList.Contains(spirit))
				{
					ownedList.Add(spirit);
					playerData.OwnedSpirits = ownedList.ToArray();
				}
			}
		}

		private void StartGuideBattle(EnemyData enemyData)
		{
			if (battleController == null)
			{
				battleController = FindObjectOfType<BattleController>(true);
			}

			if (battleController == null)
			{
				Debug.LogError("[GuideRoom] 未找到 BattleController，无法开始引导战斗");
				return;
			}

			var playerData = GetPlayerData();
			if (playerData == null)
			{
				Debug.LogError("[GuideRoom] 无法从 PlayerManager 获取 PlayerData，无法开始引导战斗");
				return;
			}

			if (!battleController.gameObject.activeInHierarchy)
			{
				battleController.gameObject.SetActive(true);
			}

			Debug.Log(
				$"[GuideRoom] 开始引导战斗: PlayerData={playerData.name}, EnemyData={enemyData.name}, 选项={chosenIndex}"
			);

			battleStarted = true;
			battleController.BeginBattleWith(playerData, enemyData);
		}

		private void CaptureEnemySpiritIfPossible()
		{
		}

		private void FinishGuide(bool victory)
		{
			if (guideCompleted)
			{
				return;
			}

			guideCompleted = true;
			Debug.Log($"[GuideRoom] 引导战斗结束，victory={victory}");

			// 离开引导战斗后停用 BattleController，保持与 CombatRoom/BossRoom 一致：仅在战斗房间激活
			if (battleController != null)
			{
				battleController.EndBattleAndDeactivate();
			}

			if (!victory)
			{
				onGuideFinished?.Invoke();
				return;
			}

			// 1) 胜利后：将未选择的Spirit也添加到玩家背包
			if (unchosenSpirit != null)
			{
				Debug.Log($"[GuideRoom] 战斗胜利，将未选择的Spirit {unchosenSpirit.DisplayName} 添加到玩家背包");
				AddUnchosenSpiritToPlayer(unchosenSpirit);
			}

			// 2) 胜利后：恢复玩家所有 OwnedSpirit 的HP/MP
			RestoreAllOwnedSpiritsToFull();

			// 3) 胜利后：发放技能（参考 SkillRoom 的做法，直接追加到 SpiritData.Skills）
			GrantVictorySkillToConfiguredSpirits();

			// 4) 胜利后：准备正式楼层，但不自动进入任意房间（改为进入路线选择）
			PrepareMainFloorForRouteSelection();

			// 通知 MapManager：引导已完成（让其继续后续流程，如切换敌人池等）
			onGuideFinished?.Invoke();

			// 4) 胜利后：可选对话（对话结束后进入路线选择）
			if (!TryStartVictoryDialogueThenBeginRouteSelection())
			{
				BeginRouteSelectionNow();
			}
		}

		private void RestoreAllOwnedSpiritsToFull()
		{
			var bc = battleController != null ? battleController : FindObjectOfType<BattleController>(true);
			if (bc == null)
			{
				Debug.LogWarning("[GuideRoom] RestoreAllOwnedSpiritsToFull: 未找到 BattleController");
				return;
			}

			var owned = CollectOwnedSpirits();
			if (owned.Count == 0)
			{
				Debug.LogWarning("[GuideRoom] RestoreAllOwnedSpiritsToFull: 未找到任何 OwnedSpirits");
				return;
			}

			bc.RestoreSpiritsToFull(owned);
		}

		private void GrantVictorySkillToConfiguredSpirits()
		{
			if (victorySkillData == null)
			{
				return;
			}

			if (victorySkillTargetSpiritDisplayNames == null || victorySkillTargetSpiritDisplayNames.Length == 0)
			{
				Debug.LogWarning("[GuideRoom] 已配置胜利技能，但未配置目标精灵 DisplayName 列表");
				return;
			}

			var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var raw in victorySkillTargetSpiritDisplayNames)
			{
				var name = string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
				if (!string.IsNullOrEmpty(name))
				{
					targets.Add(name);
				}
			}

			if (targets.Count == 0)
			{
				Debug.LogWarning("[GuideRoom] 胜利技能目标列表为空（全是空字符串）");
				return;
			}

			var spiritCandidates = new HashSet<SpiritData>();
			var owned = CollectOwnedSpirits();
			for (int i = 0; i < owned.Count; i++)
			{
				if (owned[i] != null) spiritCandidates.Add(owned[i]);
			}

			// 额外：尝试从 Resources/Spirits 加载（参考 SpiritPortraitManager 的做法）
			var allSpiritData = Resources.LoadAll<SpiritData>("Spirits");
			if (allSpiritData != null)
			{
				for (int i = 0; i < allSpiritData.Length; i++)
				{
					if (allSpiritData[i] != null) spiritCandidates.Add(allSpiritData[i]);
				}
			}

			int grantedCount = 0;
			var matchedTargetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var spirit in spiritCandidates)
			{
				if (spirit == null)
				{
					continue;
				}

				var spiritName = string.IsNullOrWhiteSpace(spirit.DisplayName) ? spirit.name : spirit.DisplayName;
				if (!targets.Contains(spiritName))
				{
					continue;
				}

				if (AddSkillToSpiritData(spirit, victorySkillData))
				{
					grantedCount++;
					matchedTargetNames.Add(spiritName);
					Debug.Log($"[GuideRoom] 已将胜利技能 {victorySkillData.name} 发放给精灵 {spiritName}");
				}
			}

			if (grantedCount == 0)
			{
				Debug.LogWarning("[GuideRoom] 未找到任何匹配的 SpiritData 来发放胜利技能（请检查 DisplayName 是否一致，或 SpiritData 是否位于 Resources/Spirits）");
				return;
			}

			// 提示：如果有目标名字没有匹配到任何 SpiritData
			foreach (var targetName in targets)
			{
				if (!matchedTargetNames.Contains(targetName))
				{
					Debug.LogWarning($"[GuideRoom] 未找到目标精灵: {targetName}（未发放胜利技能）");
				}
			}
		}

		private List<SpiritData> CollectOwnedSpirits()
		{
			var uniq = new HashSet<SpiritData>();

			var playerData = GetPlayerData();
			if (playerData != null)
			{
				var ownedFromData = playerData.GetOwnedSpirits();
				if (ownedFromData != null)
				{
					for (int i = 0; i < ownedFromData.Count; i++)
					{
						if (ownedFromData[i] != null) uniq.Add(ownedFromData[i]);
					}
				}
			}

			// PlayerManager.Instance 可能在场景中不存在时会被懒创建，但项目内其他逻辑也普遍依赖该行为
			var pm = PlayerManager.Instance;
			if (pm != null)
			{
				var ownedFromManager = pm.GetOwnedSpirits();
				if (ownedFromManager != null)
				{
					for (int i = 0; i < ownedFromManager.Count; i++)
					{
						if (ownedFromManager[i] != null) uniq.Add(ownedFromManager[i]);
					}
				}
			}

			return new List<SpiritData>(uniq);
		}

		private static bool AddSkillToSpiritData(SpiritData spiritData, ScriptableObject skillData)
		{
			if (spiritData == null || skillData == null)
			{
				return false;
			}

			var allSkills = SpiritRuntimeSkills.GetAllSkillObjects(spiritData);
			for (int i = 0; i < allSkills.Count; i++)
			{
				if (allSkills[i] == skillData)
				{
					return true;
				}
			}

			return SpiritRuntimeSkills.EnsureSkill(spiritData, skillData);
		}

		private void PrepareMainFloorForRouteSelection()
		{
			var sm = RoomStateMachine_cza.Instance;
			if (sm == null)
			{
				var go = new GameObject("RoomStateMachine");
				sm = go.AddComponent<RoomStateMachine_cza>();
				Debug.LogWarning("[GuideRoom] RoomStateMachine_cza.Instance 不存在，已临时创建");
			}

			// 若已初始化过楼层，则不重复生成地图
			if (sm.CurrentMap == null)
			{
				int floor = Mathf.Max(1, floorToInitAfterGuide);
				sm.InitFloor(floor, enterStartRoom: false);
			}
		}

		private bool TryStartVictoryDialogueThenBeginRouteSelection()
		{
			DialogueData data = victoryDialogueData;

			if (data == null && !string.IsNullOrWhiteSpace(victoryDialogueId))
			{
				var service = DreamWeavers.Services.DialogControllerService.Instance;
				if (service != null)
				{
					data = service.GetDialogueData(victoryDialogueId.Trim());
				}
			}

			if (data == null)
			{
				return false;
			}

			var dialogController = FindObjectOfType<DialogController>(true);
			if (dialogController == null)
			{
				Debug.LogWarning("[GuideRoom] 未找到 DialogController，跳过胜利对话");
				return false;
			}

			void OnEnd()
			{
				dialogController.OnDialogueEnd -= OnEnd;
				BeginRouteSelectionNow();
			}

			dialogController.OnDialogueEnd += OnEnd;
			dialogController.StartDialogue(data);
			return true;
		}

		private void BeginRouteSelectionNow()
		{
			var sm = RoomStateMachine_cza.Instance;
			if (sm == null)
			{
				Debug.LogWarning("[GuideRoom] BeginRouteSelectionNow: RoomStateMachine_cza.Instance 为 null");
				return;
			}

			sm.BeginRouteSelection();
		}
	}
}
