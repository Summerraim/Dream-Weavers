using UnityEngine;

namespace DreamWeavers.Rooms
{
    /// <summary>
    /// 道具房：进入房间后从 ItemPool 随机一个道具，
    /// - 可在房间中生成一个拾取展示（可选）
    /// - 将道具添加到玩家数据（PlayerData.InitialItems 作为当前存储）
    /// - 同时添加到运行时背包（InventoryManager）以更新 UI
    /// </summary>
    public class PropsRoom_cza : RoomBase_cza
    {
        [Header("道具池与随机设置")]
        [SerializeField] private ItemPool itemPool;
        [SerializeField] private bool useWeightedRandom = false;
        [Min(1)] [SerializeField] private int quantity = 1;

        [Header("玩家与展示设置")]
        [SerializeField] private PlayerData playerData; // 用于持久化玩家拥有的道具（沿用 PlayerData 作为运行时存储）
        [SerializeField] private Transform spawnPoint; // 展示位置（可选）
        [SerializeField] private GameObject pickupPrefab; // 展示用预制体（可选）
        [Tooltip("进入房间时自动将道具加入背包并写入玩家数据")]
        [SerializeField] private bool autoGrantOnEnter = false;

        [Header("UI 按钮绑定")]
        [Tooltip("获取道具按钮（可选）。若未手动绑定，将在运行时按名称尝试自动查找并绑定。")]
        [SerializeField] private UnityEngine.UI.Button getPropButton;
        private void Awake()
        {
            Debug.Log("[PropsRoom] Awake: start binding GetProp button and validating references");
            Debug.Log($"[PropsRoom] Refs -> itemPool={(itemPool!=null)}, playerData={(playerData!=null)}, spawnPoint={(spawnPoint!=null)}, pickupPrefab={(pickupPrefab!=null)}, autoGrantOnEnter={autoGrantOnEnter}");
            // 自动绑定获取按钮（名称包含 GetProp）
            if (getPropButton == null)
            {
                var btns = GetComponentsInChildren<UnityEngine.UI.Button>(true);
                foreach (var b in btns)
                {
                    var n = b.gameObject.name;
                    if (!string.IsNullOrEmpty(n) && n.IndexOf("GetProp", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        getPropButton = b;
                        break;
                    }
                }
            }
            if (getPropButton != null)
            {
                getPropButton.onClick.RemoveListener(OnClickGetProp);
                getPropButton.onClick.AddListener(OnClickGetProp);
                Debug.Log($"[PropsRoom] Bound GetProp button -> {getPropButton.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("[PropsRoom] GetProp button not found in children (name contains 'GetProp'). You can bind it via Inspector.");
            }
        }

        // 运行时状态
        private ItemData selectedItem;
        private GameObject pickupInstance;
        private bool granted;

        public override void EnterRoom()
        {
            Debug.Log("[PropsRoom] EnterRoom");
            // 抽取道具
            PickItemFromPool();

            // 可选：在房间中生成一个展示预制体
            SpawnPickupVisual();

            // 自动发放到玩家数据与背包
            if (autoGrantOnEnter)
            {
                GrantItemToPlayer();
            }
            else if (getPropButton == null)
            {
                Debug.LogWarning("[PropsRoom] 未找到 GetProp 按钮。可在 Inspector 绑定或将按钮命名包含 'GetProp' 以便自动绑定。");
            }
            Debug.Log($"[PropsRoom] EnterRoom done: selectedItem={(selectedItem!=null ? selectedItem.DisplayName : "null")}, granted={granted}");
        }

        public override void ExitRoom()
        {
            // 道具房无特定离开逻辑；如需在离开时再发放，可在此调用 GrantItemToPlayer()
        }

        private void PickItemFromPool()
        {
            selectedItem = null;
            if (itemPool == null || itemPool.IsEmpty)
            {
                Debug.LogWarning($"[PropsRoom] itemPool 未配置或为空，无法抽取道具 (itemPool={(itemPool!=null)} IsEmpty={(itemPool!=null?itemPool.IsEmpty:true)})");
                return;
            }

            selectedItem = useWeightedRandom ? itemPool.GetWeightedRandomItem() : itemPool.GetRandomItem();
            if (selectedItem == null)
            {
                Debug.LogWarning("[PropsRoom] 未能从对象池抽到道具 (selectedItem=null)");
            }
            else
            {
                Debug.Log($"[PropsRoom] 抽取道具：{selectedItem.DisplayName} x{quantity}");
            }
        }

        private void SpawnPickupVisual()
        {
            if (pickupPrefab == null || selectedItem == null)
            {
                Debug.Log($"[PropsRoom] SpawnPickupVisual skipped: pickupPrefab={(pickupPrefab!=null)} selectedItem={(selectedItem!=null)}");
                return;
            }

            var sp = spawnPoint != null ? spawnPoint : transform;
            pickupInstance = Instantiate(pickupPrefab, sp.position, sp.rotation);
            Debug.Log($"[PropsRoom] Spawned pickup visual -> {pickupInstance.name} at {(spawnPoint!=null?spawnPoint.name:transform.name)}");

                // 若是纯展示预制体（无交互），尝试使用 ItemDisplay 刷新外观
                var display = pickupInstance.GetComponent<ItemDisplay>();
                if (display != null)
                {
                    display.item = selectedItem;
                    display.Refresh();
                    Debug.Log("[PropsRoom] ItemDisplay refreshed");
                }
            
        }

        /// <summary>
        /// 将抽取的道具添加到玩家数据与运行时背包
        /// </summary>
        public void GrantItemToPlayer()
        {
            if (granted) return;
            if (selectedItem == null)
            {
                Debug.LogWarning("[PropsRoom] GrantItemToPlayer: selectedItem 为 null");
                return;
            }

            // 1) 运行时背包：更新 UI
            var inv = InventoryManager.Instance;
            if (inv != null)
            {
                bool ok = inv.AddItem(selectedItem, quantity);
                if (!ok)
                {
                    Debug.LogWarning("[PropsRoom] 添加到背包失败，可能背包已满或参数非法");
                }
                else
                {
                    Debug.Log($"[PropsRoom] 已添加到背包：{selectedItem.DisplayName} x{quantity}");
                }
            }
            else
            {
                Debug.LogWarning("[PropsRoom] 未找到 InventoryManager.Instance，背包未更新");
            }

            // 2) 玩家数据：沿用 PlayerData 作为运行时存储（参考 CombatRoom_cza 更新 OwnedSpirits 的做法）
            if (playerData != null)
            {
                var list = playerData.GetInitialItems(); // 复用初始道具数组作为当前持有的道具集合
                list.Add(new ItemStackConfig { Item = selectedItem, Count = Mathf.Max(1, quantity) });
                playerData.InitialItems = list.ToArray();
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(playerData);
#endif
                Debug.Log($"[PropsRoom] 已写入 PlayerData.InitialItems: {selectedItem.DisplayName} x{quantity}");
            }
            else
            {
                Debug.LogWarning("[PropsRoom] PlayerData 未配置，未写入玩家数据资产");
            }

            granted = true;
            Debug.Log("[PropsRoom] GrantItemToPlayer complete");
        }

        /// <summary>
        /// UI按钮回调：点击“获取道具(GetProp)”时调用，将当前选中的道具加入背包与玩家数据。
        /// 可在 Inspector 或按钮的 OnClick 里绑定此方法。
        /// </summary>
        public void OnClickGetProp()
        {
            if (selectedItem == null)
            {
                Debug.LogWarning("[PropsRoom] OnClickGetProp: 尚未抽取到道具，无法发放");
                return;
            }
            Debug.Log($"[PropsRoom] OnClickGetProp: granting {selectedItem.DisplayName} x{quantity}");
            GrantItemToPlayer();
        }
    }
}
