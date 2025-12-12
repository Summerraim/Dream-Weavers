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
        [SerializeField] private bool autoGrantOnEnter = true;

        // 运行时状态
        private ItemData selectedItem;
        private GameObject pickupInstance;
        private bool granted;

        public override void EnterRoom()
        {
            // 抽取道具
            PickItemFromPool();

            // 可选：在房间中生成一个展示预制体
            SpawnPickupVisual();

            // 自动发放到玩家数据与背包
            if (autoGrantOnEnter)
            {
                GrantItemToPlayer();
            }
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
                Debug.LogWarning("[PropsRoom] itemPool 未配置或为空，无法抽取道具");
                return;
            }

            selectedItem = useWeightedRandom ? itemPool.GetWeightedRandomItem() : itemPool.GetRandomItem();
            if (selectedItem == null)
            {
                Debug.LogWarning("[PropsRoom] 未能从对象池抽到道具");
            }
            else
            {
                Debug.Log($"[PropsRoom] 抽取道具：{selectedItem.DisplayName} x{quantity}");
            }
        }

        private void SpawnPickupVisual()
        {
            if (pickupPrefab == null || selectedItem == null)
                return;

            var sp = spawnPoint != null ? spawnPoint : transform;
            pickupInstance = Instantiate(pickupPrefab, sp.position, sp.rotation);

                // 若是纯展示预制体（无交互），尝试使用 ItemDisplay 刷新外观
                var display = pickupInstance.GetComponent<ItemDisplay>();
                if (display != null)
                {
                    display.item = selectedItem;
                    display.Refresh();
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
        }
    }
}
