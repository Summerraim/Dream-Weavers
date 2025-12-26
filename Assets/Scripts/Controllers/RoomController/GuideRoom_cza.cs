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
		[SerializeField] private PlayerData playerData;
		[SerializeField] private BattleController battleController;
		[SerializeField] private UI_BattleView battleView;

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
		private int chosenIndex = -1;
		private SpiritData enemySpiritToCapture;
		private bool capturedEnemySpirit;
		private SpiritData chosenSpiritPending;
		private EnemyData enemyPending;

		private void Awake()
		{
			Type = RoomType_cza.Combat; // 仅用于标记，无实际地图参与

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

					// 捕捉敌方对应精灵
					CaptureEnemySpiritIfPossible();

					// 尝试收起战斗UI，避免留在屏幕上
					if (battleView != null)
					{
						battleView.HideEnemyDeathPanel();
						battleView.HideCapturePanel();
						battleView.HideBattlePanel();
					}

					FinishGuide();
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

				// 触发新手引导房间对话
				try
				{
					var dialogService = DreamWeavers.Services.DialogControllerService.Instance;
					if (dialogService != null)
					{
						dialogService.StartDialogueForRoomWithEnemy(RoomType_cza.Guide, null);
					}
					else
					{
						Debug.LogWarning("[GuideRoom] 无法找到DialogControllerService，跳过对话触发");
					}
				}
				catch (Exception e)
				{
					Debug.LogWarning($"[GuideRoom] 触发对话时发生错误: {e.Message}");
				}

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
			enemySpiritToCapture = candidateSpirits.Length > 1 ? candidateSpirits[1 - index] : null;
			chosenSpiritPending = chosenSpirit;
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

		private void AddSpiritToPlayer(SpiritData spirit)
		{
			// 更新运行时 PlayerManager
			if (PlayerManager.Instance != null)
			{
				PlayerManager.Instance.CaptureSpirit(spirit);
				PlayerManager.Instance.DeploySpirit(spirit);
			}

			// 同步到 PlayerData（Owned 与 Deployed 只保留该引导精灵）
			if (playerData != null)
			{
				playerData.OwnedSpirits = new[] { spirit };
				playerData.DeployedSpirits = new[] { spirit };
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

			if (playerData == null)
			{
				Debug.LogError("[GuideRoom] PlayerData 未配置，无法开始引导战斗");
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
			if (capturedEnemySpirit)
			{
				return;
			}

			if (battleController != null && battleController.State != BattleState.Victory)
			{
				// 仅在胜利时捕捉
				return;
			}

			var spirit = enemySpiritToCapture;
			if (spirit == null)
			{
				Debug.LogWarning("[GuideRoom] 未配置可捕捉的敌方精灵，跳过捕捉");
				return;
			}

			bool success = false;
			if (PlayerManager.Instance != null)
			{
				success = PlayerManager.Instance.CaptureSpirit(spirit);
			}
			else if (playerData != null)
			{
				var owned = playerData.GetOwnedSpirits();
				if (!owned.Contains(spirit))
				{
					owned.Add(spirit);
					playerData.OwnedSpirits = owned.ToArray();
					success = true;
				}

				// 尝试加入出场列表（若有空位且未在列表中）
				var deployedList = new List<SpiritData>();
				if (playerData.DeployedSpirits != null)
					deployedList.AddRange(playerData.DeployedSpirits);
				if (!deployedList.Contains(spirit) && deployedList.Count < playerData.MaxDeployedSpirits)
				{
					deployedList.Add(spirit);
					playerData.DeployedSpirits = deployedList.ToArray();
				}
			}

			if (success)
			{
				capturedEnemySpirit = true;
				Debug.Log($"[GuideRoom] 捕捉敌方精灵成功: {spirit.DisplayName}");
			}
			else
			{
				Debug.LogWarning("[GuideRoom] 捕捉敌方精灵失败（可能已拥有或数据未配置）");
			}
		}

		private void FinishGuide()
		{
			if (guideCompleted)
			{
				return;
			}

			guideCompleted = true;
			Debug.Log("[GuideRoom] 引导战斗结束，准备进入正式楼层");

			onGuideFinished?.Invoke();
		}
	}
}