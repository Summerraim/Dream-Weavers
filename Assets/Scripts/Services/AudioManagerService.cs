using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 音频管理器 - 单例模式
/// 统一管理背景音乐、音效的播放、暂停和音量控制
/// </summary>
public class AudioManagerService : MonoBehaviour
{
    #region 单例实例
    
    private static AudioManagerService _instance;
    public static AudioManagerService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioManagerService>();
                
                if (_instance == null)
                {
                    GameObject audioManager = new GameObject("AudioManagerService");
                    _instance = audioManager.AddComponent<AudioManagerService>();
                    DontDestroyOnLoad(audioManager);
                }
            }
            return _instance;
        }
    }
    
    #endregion

    #region 音频混合器
    
    [Header("音频混合器")]
    [SerializeField] private AudioMixer audioMixer;
    
    [Header("音频混合器参数")]
    [SerializeField] private string masterVolumeParam = "MasterVolume";
    [SerializeField] private string bgmVolumeParam = "BGMVolume";
    [SerializeField] private string sfxVolumeParam = "SFXVolume";
    [SerializeField] private string uiVolumeParam = "UIVolume";
    
    #endregion
    
    #region 音频源配置
    
    [Header("背景音乐配置")]
    [SerializeField] private AudioSource bgmSource;         // 主背景音乐源
    [SerializeField] private AudioSource bgmCrossfadeSource; // 交叉淡入淡出背景音乐源
    [SerializeField] private float crossfadeDuration = 1.5f; // 交叉淡入淡出时间
    
    [Header("音效配置")]
    [SerializeField] private int sfxPoolSize = 20;          // 音效池大小
    [SerializeField] private AudioSource sfxPrefab;         // 音效预制体
    [SerializeField] private Transform sfxPoolParent;       // 音效池父物体
    
    [Header("UI音效配置")]
    [SerializeField] private AudioSource uiSfxSource;       // UI音效源（用于播放UI音效，保证立即响应）
    
    #endregion
    
    #region 音频剪辑
    
    [Header("音频资源")]
    [SerializeField] private AudioClip defaultBGM;
    [SerializeField] private AudioClip menuBGM;
    [SerializeField] private AudioClip gameplayBGM;
    [SerializeField] private AudioClip battleBGM;
    
    [Header("常用音效")]
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private AudioClip buttonHoverSFX;
    [SerializeField] private AudioClip notificationSFX;
    [SerializeField] private AudioClip victorySFX;
    [SerializeField] private AudioClip defeatSFX;
    [SerializeField] private AudioClip levelUpSFX;
    
    #endregion
    
    #region 音频池
    
    private Queue<AudioSource> sfxPool = new Queue<AudioSource>();
    private List<AudioSource> activeSFX = new List<AudioSource>();
    private Dictionary<string, AudioClip> audioClipCache = new Dictionary<string, AudioClip>();
    
    #endregion
    
    #region 音频状态
    
    [Header("音量设置")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float uiVolume = 1f;
    
    [Header("其他设置")]
    public bool isMuted = false;
    public bool isPaused = false;
    public bool enableCrossfade = true;
    
    private float bgmFadeTimer = 0f;
    private bool isCrossfading = false;
    private AudioClip nextBGM;
    
    #endregion
    
    #region Unity生命周期
    
    private void Awake()
    {
        // 确保单例
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // 初始化音频源
        InitializeAudioSources();
        
        // 初始化音效池
        InitializeSFXPool();
        
        // 加载音量设置
        LoadVolumeSettings();
    }
    
    private void Start()
    {
        // 订阅游戏状态变化
        if (GameManagerService.Instance != null)
        {
            GameManagerService.Instance.OnGameStateChanged += OnGameStateChanged;
            GameManagerService.Instance.OnSceneLoaded += OnSceneLoaded;
        }
        
        // 播放默认背景音乐
        if (defaultBGM != null && bgmSource.clip == null)
        {
            PlayBGM(defaultBGM);
        }
    }
    
    private void Update()
    {
        // 处理交叉淡入淡出
        UpdateCrossfade();
        
        // 清理完成的音效
        CleanupFinishedSFX();
    }
    
    private void OnDestroy()
    {
        // 清理事件订阅
        if (GameManagerService.Instance != null)
        {
            GameManagerService.Instance.OnGameStateChanged -= OnGameStateChanged;
            GameManagerService.Instance.OnSceneLoaded -= OnSceneLoaded;
        }
    }
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化音频源
    /// </summary>
    private void InitializeAudioSources()
    {
        // 确保有背景音乐源
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        
        // 确保有交叉淡入淡出源
        if (bgmCrossfadeSource == null)
        {
            bgmCrossfadeSource = gameObject.AddComponent<AudioSource>();
            bgmCrossfadeSource.loop = true;
            bgmCrossfadeSource.playOnAwake = false;
        }
        
        // 确保有UI音效源
        if (uiSfxSource == null)
        {
            uiSfxSource = gameObject.AddComponent<AudioSource>();
            uiSfxSource.playOnAwake = false;
        }
        
        // 确保有音效池父物体
        if (sfxPoolParent == null)
        {
            GameObject poolObj = new GameObject("SFX_Pool");
            poolObj.transform.SetParent(transform);
            sfxPoolParent = poolObj.transform;
        }
    }
    
    /// <summary>
    /// 初始化音效池
    /// </summary>
    private void InitializeSFXPool()
    {
        // 创建音效预制体（如果未指定）
        if (sfxPrefab == null)
        {
            GameObject sfxObj = new GameObject("SFX_Prefab");
            sfxPrefab = sfxObj.AddComponent<AudioSource>();
            sfxPrefab.playOnAwake = false;
            DontDestroyOnLoad(sfxObj);
            sfxObj.SetActive(false);
        }
        
        // 初始化音效池
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource sfxSource = Instantiate(sfxPrefab, sfxPoolParent);
            sfxSource.gameObject.name = $"SFX_{i}";
            sfxSource.gameObject.SetActive(false);
            sfxPool.Enqueue(sfxSource);
        }
    }
    
    #endregion
    
    #region 背景音乐控制
    
    /// <summary>
    /// 播放背景音乐
    /// </summary>
    public void PlayBGM(AudioClip clip, bool forcePlay = false)
    {
        if (clip == null) return;
        
        // 如果正在播放相同的音乐且不是强制播放
        if (!forcePlay && bgmSource.clip == clip && bgmSource.isPlaying)
            return;
        
        // 使用交叉淡入淡出
        if (enableCrossfade && bgmSource.isPlaying && bgmSource.clip != null)
        {
            StartCrossfade(clip);
        }
        else
        {
            // 直接播放
            bgmSource.clip = clip;
            bgmSource.Play();
            bgmSource.volume = bgmVolume;
        }
    }
    
    /// <summary>
    /// 播放背景音乐（通过名称）
    /// </summary>
    public void PlayBGM(string clipName)
    {
        AudioClip clip = LoadAudioClip($"BGM/{clipName}");
        if (clip != null)
        {
            PlayBGM(clip);
        }
    }
    
    /// <summary>
    /// 停止背景音乐
    /// </summary>
    public void StopBGM(bool fadeOut = true)
    {
        if (fadeOut && bgmSource.isPlaying)
        {
            StartCoroutine(FadeOutBGM(1f));
        }
        else
        {
            bgmSource.Stop();
        }
    }
    
    /// <summary>
    /// 暂停背景音乐
    /// </summary>
    public void PauseBGM()
    {
        if (bgmSource.isPlaying)
        {
            bgmSource.Pause();
        }
    }
    
    /// <summary>
    /// 恢复背景音乐
    /// </summary>
    public void ResumeBGM()
    {
        if (!bgmSource.isPlaying && bgmSource.clip != null)
        {
            bgmSource.UnPause();
        }
    }
    
    /// <summary>
    /// 淡出背景音乐协程
    /// </summary>
    private System.Collections.IEnumerator FadeOutBGM(float duration)
    {
        float startVolume = bgmSource.volume;
        float timer = 0f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }
        
        bgmSource.Stop();
        bgmSource.volume = startVolume;
    }
    
    #endregion
    
    #region 音效控制
    
    /// <summary>
    /// 播放音效
    /// </summary>
    public AudioSource PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (clip == null || isMuted) return null;
        
        // 从池中获取音效源
        AudioSource sfxSource = GetSFXSourceFromPool();
        if (sfxSource == null) return null;
        
        // 配置音效
        sfxSource.clip = clip;
        sfxSource.volume = volume * sfxVolume;
        sfxSource.pitch = pitch;
        sfxSource.loop = loop;
        sfxSource.Play();
        
        // 添加到活动列表
        activeSFX.Add(sfxSource);
        
        return sfxSource;
    }
    
    /// <summary>
    /// 播放音效（通过名称）
    /// </summary>
    public AudioSource PlaySFX(string clipName, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        AudioClip clip = LoadAudioClip($"SFX/{clipName}");
        if (clip != null)
        {
            return PlaySFX(clip, volume, pitch, loop);
        }
        return null;
    }
    
    /// <summary>
    /// 播放UI音效（立即响应，不经过池）
    /// </summary>
    public void PlayUISFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || isMuted) return;
        
        if (uiSfxSource != null)
        {
            uiSfxSource.PlayOneShot(clip, volume * uiVolume);
        }
        else
        {
            // 备用方案
            PlaySFX(clip, volume);
        }
    }
    
    /// <summary>
    /// 播放UI音效（通过名称）
    /// </summary>
    public void PlayUISFX(string clipName, float volume = 1f)
    {
        AudioClip clip = LoadAudioClip($"UI/{clipName}");
        if (clip != null)
        {
            PlayUISFX(clip, volume);
        }
    }
    
    /// <summary>
    /// 停止所有音效
    /// </summary>
    public void StopAllSFX()
    {
        foreach (AudioSource sfx in activeSFX)
        {
            if (sfx != null && sfx.isPlaying)
            {
                sfx.Stop();
                ReturnSFXSourceToPool(sfx);
            }
        }
        activeSFX.Clear();
    }
    
    /// <summary>
    /// 停止特定音效
    /// </summary>
    public void StopSFX(AudioSource sfxSource)
    {
        if (sfxSource != null && sfxSource.isPlaying)
        {
            sfxSource.Stop();
            if (activeSFX.Contains(sfxSource))
            {
                activeSFX.Remove(sfxSource);
                ReturnSFXSourceToPool(sfxSource);
            }
        }
    }
    
    #endregion
    
    #region 音效池管理
    
    /// <summary>
    /// 从池中获取音效源
    /// </summary>
    private AudioSource GetSFXSourceFromPool()
    {
        // 如果池中有可用源，直接使用
        if (sfxPool.Count > 0)
        {
            AudioSource source = sfxPool.Dequeue();
            source.gameObject.SetActive(true);
            return source;
        }
        
        // 池为空，创建新的（动态扩展池大小）
        AudioSource newSource = Instantiate(sfxPrefab, sfxPoolParent);
        newSource.gameObject.name = $"SFX_Dynamic_{Time.time}";
        sfxPoolSize++;
        
        Debug.LogWarning($"音效池已满，动态扩展到: {sfxPoolSize}");
        
        return newSource;
    }
    
    /// <summary>
    /// 将音效源返回到池中
    /// </summary>
    private void ReturnSFXSourceToPool(AudioSource source)
    {
        if (source == null) return;
        
        source.gameObject.SetActive(false);
        source.Stop();
        source.clip = null;
        
        // 如果池大小超过限制，销毁多余的
        if (sfxPool.Count >= sfxPoolSize)
        {
            Destroy(source.gameObject);
        }
        else
        {
            sfxPool.Enqueue(source);
        }
    }
    
    /// <summary>
    /// 清理已完成的音效
    /// </summary>
    private void CleanupFinishedSFX()
    {
        for (int i = activeSFX.Count - 1; i >= 0; i--)
        {
            AudioSource sfx = activeSFX[i];
            
            if (sfx == null || !sfx.isPlaying)
            {
                if (sfx != null)
                {
                    ReturnSFXSourceToPool(sfx);
                }
                activeSFX.RemoveAt(i);
            }
        }
    }
    
    #endregion
    
    #region 音量控制
    
    /// <summary>
    /// 设置主音量
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAudioMixer();
        SaveVolumeSettings();
    }
    
    /// <summary>
    /// 设置背景音乐音量
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        UpdateAudioMixer();
        SaveVolumeSettings();
    }
    
    /// <summary>
    /// 设置音效音量
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateAudioMixer();
        SaveVolumeSettings();
    }
    
    /// <summary>
    /// 设置UI音量
    /// </summary>
    public void SetUIVolume(float volume)
    {
        uiVolume = Mathf.Clamp01(volume);
        UpdateAudioMixer();
        SaveVolumeSettings();
    }
    
    /// <summary>
    /// 切换静音
    /// </summary>
    public void ToggleMute()
    {
        isMuted = !isMuted;
        
        if (isMuted)
        {
            // 保存当前音量设置
            PlayerPrefs.SetFloat("SavedMasterVolume", masterVolume);
            SetMasterVolume(0f);
        }
        else
        {
            // 恢复音量设置
            float savedVolume = PlayerPrefs.GetFloat("SavedMasterVolume", 1f);
            SetMasterVolume(savedVolume);
        }
    }
    
    /// <summary>
    /// 更新音频混合器
    /// </summary>
    private void UpdateAudioMixer()
    {
        if (audioMixer == null) return;
        
        // 转换线性音量到分贝（dB）
        // 公式: dB = 20 * log10(volume)
        // 注意：当volume=0时，我们设置为-80dB（静音）
        
        float masterDB = masterVolume > 0.0001f ? 20f * Mathf.Log10(masterVolume) : -80f;
        float bgmDB = bgmVolume > 0.0001f ? 20f * Mathf.Log10(bgmVolume) : -80f;
        float sfxDB = sfxVolume > 0.0001f ? 20f * Mathf.Log10(sfxVolume) : -80f;
        float uiDB = uiVolume > 0.0001f ? 20f * Mathf.Log10(uiVolume) : -80f;
        
        audioMixer.SetFloat(masterVolumeParam, masterDB);
        audioMixer.SetFloat(bgmVolumeParam, bgmDB);
        audioMixer.SetFloat(sfxVolumeParam, sfxDB);
        audioMixer.SetFloat(uiVolumeParam, uiDB);
        
        // 更新背景音乐源音量（如果未使用混合器）
        if (bgmSource != null && !isMuted)
        {
            bgmSource.volume = bgmVolume * masterVolume;
        }
    }
    
    /// <summary>
    /// 保存音量设置
    /// </summary>
    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetFloat("UIVolume", uiVolume);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 加载音量设置
    /// </summary>
    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        uiVolume = PlayerPrefs.GetFloat("UIVolume", 1f);
        
        UpdateAudioMixer();
    }
    
    #endregion
    
    #region 交叉淡入淡出
    
    /// <summary>
    /// 开始交叉淡入淡出
    /// </summary>
    private void StartCrossfade(AudioClip newClip)
    {
        if (!enableCrossfade || bgmCrossfadeSource == null) return;
        
        nextBGM = newClip;
        isCrossfading = true;
        bgmFadeTimer = 0f;
        
        // 设置交叉淡入淡出源
        bgmCrossfadeSource.clip = newClip;
        bgmCrossfadeSource.volume = 0f;
        bgmCrossfadeSource.Play();
    }
    
    /// <summary>
    /// 更新交叉淡入淡出
    /// </summary>
    private void UpdateCrossfade()
    {
        if (!isCrossfading) return;
        
        bgmFadeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(bgmFadeTimer / crossfadeDuration);
        
        // 更新音量
        bgmSource.volume = Mathf.Lerp(bgmVolume, 0f, t) * masterVolume;
        bgmCrossfadeSource.volume = Mathf.Lerp(0f, bgmVolume, t) * masterVolume;
        
        // 淡入淡出完成
        if (t >= 1f)
        {
            // 交换音频源
            AudioSource temp = bgmSource;
            bgmSource = bgmCrossfadeSource;
            bgmCrossfadeSource = temp;
            
            // 停止旧的音频源
            bgmCrossfadeSource.Stop();
            bgmCrossfadeSource.clip = null;
            
            isCrossfading = false;
        }
    }
    
    #endregion
    
    #region 资源加载
    
    /// <summary>
    /// 加载音频剪辑
    /// </summary>
    private AudioClip LoadAudioClip(string path)
    {
        // 检查缓存
        if (audioClipCache.ContainsKey(path))
        {
            return audioClipCache[path];
        }
        
        // 从Resources加载
        AudioClip clip = Resources.Load<AudioClip>($"Audio/{path}");
        
        if (clip != null)
        {
            audioClipCache[path] = clip;
        }
        else
        {
            Debug.LogWarning($"音频资源未找到: Audio/{path}");
        }
        
        return clip;
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 游戏状态变化回调
    /// </summary>
    private void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Paused:
                PauseAllAudio();
                break;
                
            case GameState.Playing:
                ResumeAllAudio();
                break;
                
            case GameState.GameOver:
                // 游戏结束时播放相应音效
                PlaySFX(defeatSFX);
                break;
        }
    }
    
    /// <summary>
    /// 场景加载回调
    /// </summary>
    private void OnSceneLoaded(SceneType sceneType)
    {
        // 根据场景类型播放不同的背景音乐
        switch (sceneType)
        {
            case SceneType.MainMenu:
                if (menuBGM != null) PlayBGM(menuBGM);
                break;
                
            case SceneType.Gameplay:
                if (gameplayBGM != null) PlayBGM(gameplayBGM);
                break;
                
            case SceneType.Battle:
                if (battleBGM != null) PlayBGM(battleBGM);
                break;
        }
    }
    
    #endregion
    
    #region 全局控制
    
    /// <summary>
    /// 暂停所有音频
    /// </summary>
    public void PauseAllAudio()
    {
        isPaused = true;
        
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Pause();
        }
        
        foreach (AudioSource sfx in activeSFX)
        {
            if (sfx != null && sfx.isPlaying)
            {
                sfx.Pause();
            }
        }
    }
    
    /// <summary>
    /// 恢复所有音频
    /// </summary>
    public void ResumeAllAudio()
    {
        isPaused = false;
        
        if (bgmSource != null && !bgmSource.isPlaying && bgmSource.clip != null)
        {
            bgmSource.UnPause();
        }
        
        foreach (AudioSource sfx in activeSFX)
        {
            if (sfx != null && !sfx.isPlaying && sfx.clip != null)
            {
                sfx.UnPause();
            }
        }
    }
    
    /// <summary>
    /// 停止所有音频
    /// </summary>
    public void StopAllAudio()
    {
        StopBGM(false);
        StopAllSFX();
    }
    
    #endregion
    
    #region 常用音效快捷方法
    
    /// <summary>
    /// 播放按钮点击音效
    /// </summary>
    public void PlayButtonClick()
    {
        if (buttonClickSFX != null)
        {
            PlayUISFX(buttonClickSFX);
        }
    }
    
    /// <summary>
    /// 播放按钮悬停音效
    /// </summary>
    public void PlayButtonHover()
    {
        if (buttonHoverSFX != null)
        {
            PlayUISFX(buttonHoverSFX, 0.7f);
        }
    }
    
    /// <summary>
    /// 播放通知音效
    /// </summary>
    public void PlayNotification()
    {
        if (notificationSFX != null)
        {
            PlayUISFX(notificationSFX);
        }
    }
    
    /// <summary>
    /// 播放胜利音效
    /// </summary>
    public void PlayVictory()
    {
        if (victorySFX != null)
        {
            PlaySFX(victorySFX);
        }
    }
    
    /// <summary>
    /// 播放升级音效
    /// </summary>
    public void PlayLevelUp()
    {
        if (levelUpSFX != null)
        {
            PlaySFX(levelUpSFX);
        }
    }
    
    #endregion
    
    #region 调试功能
    
    /// <summary>
    /// 打印音频状态
    /// </summary>
    public void PrintAudioStatus()
    {
        Debug.Log("=== 音频状态 ===");
        Debug.Log($"主音量: {masterVolume}");
        Debug.Log($"BGM音量: {bgmVolume}");
        Debug.Log($"SFX音量: {sfxVolume}");
        Debug.Log($"静音: {isMuted}");
        Debug.Log($"暂停: {isPaused}");
        Debug.Log($"BGM正在播放: {(bgmSource.isPlaying ? bgmSource.clip.name : "无")}");
        Debug.Log($"活动SFX数量: {activeSFX.Count}");
        Debug.Log($"音效池大小: {sfxPoolSize} (可用: {sfxPool.Count})");
        Debug.Log("==============");
    }
    
    #endregion
}