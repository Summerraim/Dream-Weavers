using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 精灵立绘管理器
/// 负责管理精灵对话时的立绘显示
/// </summary>
public class SpiritPortraitManager : MonoBehaviour
{
    #region 单例实例
    
    private static SpiritPortraitManager _instance;
    public static SpiritPortraitManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SpiritPortraitManager>();
                
                if (_instance == null)
                {
                    GameObject manager = new GameObject("SpiritPortraitManager");
                    _instance = manager.AddComponent<SpiritPortraitManager>();
                }
            }
            return _instance;
        }
    }
    
    #endregion
    
    #region 立绘存储
    
    private Dictionary<string, Sprite> spiritPortraits = new Dictionary<string, Sprite>();
    
    #endregion
    
    #region Unity生命周期
    
    private void Awake()
    {
        // 确保单例
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 初始化精灵立绘
            InitializeSpiritPortraits();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化精灵立绘
    /// </summary>
    private void InitializeSpiritPortraits()
    {
        // 加载所有精灵数据并提取立绘
        SpiritData[] allSpiritData = Resources.LoadAll<SpiritData>("Spirits");
        
        foreach (SpiritData spiritData in allSpiritData)
        {
            if (spiritData != null && spiritData.Image != null)
            {
                spiritPortraits[spiritData.DisplayName] = spiritData.Image;
                Debug.Log($"加载精灵立绘: {spiritData.DisplayName}");
            }
        }
        
        // 加载主角立绘
        Sprite playerPortrait = Resources.Load<Sprite>("Art/Spirits/主角");
        if (playerPortrait != null)
        {
            spiritPortraits["主角"] = playerPortrait;
            spiritPortraits["玩家"] = playerPortrait;
            spiritPortraits["Player"] = playerPortrait;
            Debug.Log("加载主角立绘");
        }
        
        Debug.Log($"精灵立绘管理器初始化完成，共加载 {spiritPortraits.Count} 个立绘");
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 获取精灵立绘
    /// </summary>
    public Sprite GetSpiritPortrait(string spiritName)
    {
        if (spiritPortraits.ContainsKey(spiritName))
        {
            return spiritPortraits[spiritName];
        }
        
        // 尝试模糊匹配
        foreach (var pair in spiritPortraits)
        {
            if (pair.Key.Contains(spiritName) || spiritName.Contains(pair.Key))
            {
                return pair.Value;
            }
        }
        
        Debug.LogWarning($"未找到精灵立绘: {spiritName}");
        return null;
    }
    
    /// <summary>
    /// 检查是否有精灵立绘
    /// </summary>
    public bool HasSpiritPortrait(string spiritName)
    {
        return spiritPortraits.ContainsKey(spiritName);
    }
    
    /// <summary>
    /// 添加精灵立绘
    /// </summary>
    public void AddSpiritPortrait(string spiritName, Sprite portrait)
    {
        if (spiritPortraits.ContainsKey(spiritName))
        {
            Debug.LogWarning($"精灵立绘已存在: {spiritName}，将被覆盖");
        }
        
        spiritPortraits[spiritName] = portrait;
        Debug.Log($"添加精灵立绘: {spiritName}");
    }
    
    /// <summary>
    /// 获取所有精灵名称
    /// </summary>
    public List<string> GetAllSpiritNames()
    {
        return new List<string>(spiritPortraits.Keys);
    }
    
    #endregion
    
    #region 对话立绘辅助方法
    
    /// <summary>
    /// 为对话条目获取合适的立绘
    /// </summary>
    public Sprite GetDialoguePortrait(string speakerName, string dialogueText = "")
    {
        // 如果是系统消息，返回null（不显示立绘）
        if (speakerName == "系统" || speakerName == "System")
        {
            return null;
        }
        
        // 尝试获取精灵立绘
        Sprite spiritPortrait = GetSpiritPortrait(speakerName);
        if (spiritPortrait != null)
        {
            return spiritPortrait;
        }
        
        // 如果是主角相关对话，返回主角立绘
        if (speakerName == "主角" || speakerName == "玩家" || speakerName == "Player")
        {
            return GetSpiritPortrait("主角");
        }
        
        // 如果没有找到立绘，返回null
        return null;
    }
    
    /// <summary>
    /// 检查说话者是否为精灵
    /// </summary>
    public bool IsSpiritSpeaker(string speakerName)
    {
        return HasSpiritPortrait(speakerName) || 
               speakerName == "主角" || 
               speakerName == "玩家" || 
               speakerName == "Player";
    }
    
    #endregion
}
