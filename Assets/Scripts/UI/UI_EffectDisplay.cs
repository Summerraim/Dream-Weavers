using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Effect展示器 - 显示玩家和敌人身上的所有Buff/Debuff
/// 参考UI_SpiritSwitcher的架构实现
/// </summary>
public class UI_EffectDisplay : MonoBehaviour
{
    [Header("Effect Slot Prefab")]
    [SerializeField]
    private GameObject effectSlotPrefab;

    [Header("Player Effects Container")]
    [SerializeField]
    private Transform playerEffectsContainer;

    [SerializeField]
    private GameObject playerEffectsPanel;

    [Header("Enemy Effects Container")]
    [SerializeField]
    private Transform enemyEffectsContainer;

    [SerializeField]
    private GameObject enemyEffectsPanel;

    [Header("Settings")]
    [SerializeField]
    private bool hideWhenEmpty = true;

    [SerializeField]
    private int maxEffectsPerUnit = 10; // 每个单位最多显示的Effect数量

    private BattleController battleController;
    private BattleModel battleModel;

    private List<EffectSlot> playerEffectSlots = new List<EffectSlot>();
    private List<EffectSlot> enemyEffectSlots = new List<EffectSlot>();

    private void Awake()
    {
        // 初始化面板状态
        if (playerEffectsPanel != null && hideWhenEmpty)
            playerEffectsPanel.SetActive(false);

        if (enemyEffectsPanel != null && hideWhenEmpty)
            enemyEffectsPanel.SetActive(false);
    }

    /// <summary>
    /// 绑定BattleController和BattleModel
    /// </summary>
    public void Bind(BattleController controller, BattleModel model)
    {
        battleController = controller;
        battleModel = model;

        Debug.Log($"[UI_EffectDisplay] Bind完成: controller={(controller != null ? "存在" : "null")}, model={(model != null ? "存在" : "null")}");

        // 初始化槽位池
        InitializeSlotPools();

        // 首次刷新
        RefreshDisplay();
    }

    /// <summary>
    /// 解除绑定
    /// </summary>
    public void Unbind()
    {
        battleController = null;
        battleModel = null;

        ClearAllSlots();
    }

    /// <summary>
    /// 初始化槽位对象池
    /// </summary>
    private void InitializeSlotPools()
    {
        Debug.Log("[UI_EffectDisplay] InitializeSlotPools开始");
        Debug.Log($"[UI_EffectDisplay] playerEffectsContainer={(playerEffectsContainer != null ? "存在" : "null")}");
        Debug.Log($"[UI_EffectDisplay] enemyEffectsContainer={(enemyEffectsContainer != null ? "存在" : "null")}");
        Debug.Log($"[UI_EffectDisplay] effectSlotPrefab={(effectSlotPrefab != null ? "存在" : "null")}");

        // 预创建槽位对象，避免运行时频繁创建销毁
        CreateSlotPool(playerEffectsContainer, playerEffectSlots, "Player");
        CreateSlotPool(enemyEffectsContainer, enemyEffectSlots, "Enemy");

        Debug.Log($"[UI_EffectDisplay] InitializeSlotPools完成: 玩家槽位数={playerEffectSlots.Count}, 敌人槽位数={enemyEffectSlots.Count}");
    }

    /// <summary>
    /// 创建槽位池
    /// </summary>
    private void CreateSlotPool(Transform container, List<EffectSlot> slotList, string poolName)
    {
        Debug.Log($"[UI_EffectDisplay] CreateSlotPool开始: {poolName}");

        if (container == null)
        {
            Debug.LogError($"[UI_EffectDisplay] CreateSlotPool失败: {poolName} container为null! 请在Inspector中设置{poolName}EffectsContainer");
            return;
        }

        Debug.Log($"[UI_EffectDisplay] {poolName}: 开始创建{maxEffectsPerUnit}个槽位，使用{(effectSlotPrefab != null ? "Prefab" : "默认样式")}");

        for (int i = 0; i < maxEffectsPerUnit; i++)
        {
            GameObject slotObj;

            if (effectSlotPrefab != null)
            {
                slotObj = Instantiate(effectSlotPrefab, container);
            }
            else
            {
                slotObj = CreateDefaultSlot();
                slotObj.transform.SetParent(container, false);
            }

            var slot = slotObj.GetComponent<EffectSlot>();
            if (slot == null)
            {
                slot = slotObj.AddComponent<EffectSlot>();
            }

            slot.Clear(); // 初始时隐藏
            slotList.Add(slot);
        }

        Debug.Log($"[UI_EffectDisplay] CreateSlotPool完成: {poolName}, 成功创建{slotList.Count}个槽位");
    }

    /// <summary>
    /// 创建默认槽位（如果没有提供预制体）
    /// </summary>
    private GameObject CreateDefaultSlot()
    {
        GameObject slotObj = new GameObject("EffectSlot");

        // 添加Image组件作为背景
        var image = slotObj.AddComponent<Image>();
        image.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        // 设置RectTransform
        var rectTransform = slotObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(80, 80);

        return slotObj;
    }

    /// <summary>
    /// 刷新所有Effect显示
    /// </summary>
    public void RefreshDisplay()
    {
        if (battleModel == null)
        {
            Debug.LogWarning("[UI_EffectDisplay] RefreshDisplay: battleModel is null!");
            return;
        }

        Debug.Log("[UI_EffectDisplay] RefreshDisplay开始");

        // 刷新玩家Effect
        if (battleModel.PlayerUnit != null)
        {
            Debug.Log($"[UI_EffectDisplay] 刷新玩家Effect: {battleModel.PlayerUnit.DisplayName}");
            RefreshUnitEffects(battleModel.PlayerUnit, playerEffectSlots, playerEffectsPanel);
        }
        else
        {
            ClearSlots(playerEffectSlots, playerEffectsPanel);
        }

        // 刷新敌人Effect
        if (battleModel.EnemyUnits != null && battleModel.EnemyUnits.Count > 0)
        {
            var enemy = battleModel.EnemyUnits[0];
            Debug.Log($"[UI_EffectDisplay] 刷新敌人Effect: {enemy.DisplayName}");
            RefreshUnitEffects(enemy, enemyEffectSlots, enemyEffectsPanel);
        }
        else if (battleController != null && battleController.Enemy != null)
        {
            Debug.Log($"[UI_EffectDisplay] 刷新敌人Effect (from controller): {battleController.Enemy.DisplayName}");
            RefreshUnitEffects(battleController.Enemy, enemyEffectSlots, enemyEffectsPanel);
        }
        else
        {
            ClearSlots(enemyEffectSlots, enemyEffectsPanel);
        }

        Debug.Log("[UI_EffectDisplay] RefreshDisplay完成");
    }

    /// <summary>
    /// 刷新单个单位的Effect显示
    /// </summary>
    private void RefreshUnitEffects(
        IBattleUnit unit,
        List<EffectSlot> slotList,
        GameObject panel
    )
    {
        if (unit == null || battleModel == null)
        {
            ClearSlots(slotList, panel);
            return;
        }

        // 获取单位身上的所有Buff
        var buffs = battleModel.GetBuffsForUnit(unit);

        Debug.Log($"[UI_EffectDisplay] RefreshUnitEffects: {unit.DisplayName}, Buff数量={(buffs != null ? buffs.Count : 0)}");

        // 如果没有Effect且设置了隐藏空面板
        if ((buffs == null || buffs.Count == 0) && hideWhenEmpty)
        {
            Debug.Log($"[UI_EffectDisplay] 没有Buff，隐藏面板");
            ClearSlots(slotList, panel);
            if (panel != null)
                panel.SetActive(false);
            return;
        }

        // 显示面板
        if (panel != null)
        {
            panel.SetActive(true);
            Debug.Log($"[UI_EffectDisplay] 显示面板: {panel.name}");
        }

        // 更新槽位显示
        int effectIndex = 0;
        if (buffs != null)
        {
            for (int i = 0; i < buffs.Count && effectIndex < slotList.Count; i++)
            {
                var buff = buffs[i];
                if (buff != null && !buff.IsExpired)
                {
                    Debug.Log($"[UI_EffectDisplay] 设置槽位{effectIndex}: {buff.DisplayName}");
                    slotList[effectIndex].SetEffect(buff);
                    effectIndex++;
                }
            }
        }

        Debug.Log($"[UI_EffectDisplay] 总共设置了{effectIndex}个槽位");

        // 清空未使用的槽位
        for (int i = effectIndex; i < slotList.Count; i++)
        {
            slotList[i].Clear();
        }
    }

    /// <summary>
    /// 清空指定槽位列表
    /// </summary>
    private void ClearSlots(List<EffectSlot> slotList, GameObject panel)
    {
        if (slotList != null)
        {
            foreach (var slot in slotList)
            {
                if (slot != null)
                    slot.Clear();
            }
        }

        if (panel != null && hideWhenEmpty)
        {
            panel.SetActive(false);
        }
    }

    /// <summary>
    /// 清空所有槽位
    /// </summary>
    private void ClearAllSlots()
    {
        ClearSlots(playerEffectSlots, playerEffectsPanel);
        ClearSlots(enemyEffectSlots, enemyEffectsPanel);
    }

    /// <summary>
    /// 手动触发刷新（供外部调用）
    /// </summary>
    public void UpdateEffects()
    {
        RefreshDisplay();
    }

    /// <summary>
    /// 显示玩家Effect面板
    /// </summary>
    public void ShowPlayerEffects()
    {
        if (playerEffectsPanel != null)
            playerEffectsPanel.SetActive(true);
    }

    /// <summary>
    /// 隐藏玩家Effect面板
    /// </summary>
    public void HidePlayerEffects()
    {
        if (playerEffectsPanel != null)
            playerEffectsPanel.SetActive(false);
    }

    /// <summary>
    /// 显示敌人Effect面板
    /// </summary>
    public void ShowEnemyEffects()
    {
        if (enemyEffectsPanel != null)
            enemyEffectsPanel.SetActive(true);
    }

    /// <summary>
    /// 隐藏敌人Effect面板
    /// </summary>
    public void HideEnemyEffects()
    {
        if (enemyEffectsPanel != null)
            enemyEffectsPanel.SetActive(false);
    }

    /// <summary>
    /// 获取玩家当前Effect数量
    /// </summary>
    public int GetPlayerEffectCount()
    {
        if (battleModel == null || battleModel.PlayerUnit == null)
            return 0;

        var buffs = battleModel.GetBuffsForUnit(battleModel.PlayerUnit);
        return buffs != null ? buffs.Count : 0;
    }

    /// <summary>
    /// 获取敌人当前Effect数量
    /// </summary>
    public int GetEnemyEffectCount()
    {
        if (battleModel == null)
            return 0;

        IBattleUnit enemy = null;
        if (battleModel.EnemyUnits != null && battleModel.EnemyUnits.Count > 0)
        {
            enemy = battleModel.EnemyUnits[0];
        }
        else if (battleController != null)
        {
            enemy = battleController.Enemy;
        }

        if (enemy == null)
            return 0;

        var buffs = battleModel.GetBuffsForUnit(enemy);
        return buffs != null ? buffs.Count : 0;
    }
}
