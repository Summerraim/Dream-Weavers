using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private AudioSource audioSource;
    
    [Header("背景音乐设置")]
    [SerializeField] private AudioClip[] backgroundMusic; // 音乐列表，索引对应楼层（0-3对应1-4层）
    
    [Header("主菜单音乐")]
    [SerializeField] private AudioClip menuMusic; // 主菜单音乐

    private void Awake()
    {
        EnsureAudioSource();
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = 0.5f; // 初始音量设置为50%
    }
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("AudioManager: Start方法开始执行");

        EnsureAudioSource();
        
        // 检查音乐列表配置
        if (backgroundMusic == null || backgroundMusic.Length == 0)
        {
            Debug.LogError("AudioManager: 背景音乐列表为空或未配置，请在Inspector中配置backgroundMusic数组");
        }
        else
        {
            Debug.Log($"AudioManager: 已配置 {backgroundMusic.Length} 首背景音乐");
        }
        
        // 使用协程等待RoomStateMachine_cza实例创建
        StartCoroutine(WaitForRoomStateMachine());
    }
    
    /// <summary>
    /// 等待RoomStateMachine_cza实例创建
    /// </summary>
    private IEnumerator WaitForRoomStateMachine()
    {
        Debug.Log("AudioManager: 开始等待RoomStateMachine_cza实例创建");
        
        // 等待RoomStateMachine_cza实例创建
        yield return new WaitUntil(() => RoomStateMachine_cza.Instance != null);
        
        Debug.Log($"AudioManager: RoomStateMachine_cza实例已创建，当前楼层: {RoomStateMachine_cza.Instance.CurrentFloor}");
        
        // 订阅楼层变化事件
        RoomStateMachine_cza.Instance.OnCurrentFloorChanged += OnCurrentFloorChanged;
        Debug.Log("AudioManager: 已订阅楼层变化事件");
        
        // 播放初始楼层音乐
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.Contains("Menu") || sceneName.Contains("MainMenu"))
        {
            Debug.Log("AudioManager: 当前为主菜单场景，跳过楼层背景音乐自动播放");
        }
        else
        {
            PlayFloorMusic(RoomStateMachine_cza.Instance.CurrentFloor);
        }
    }
    
    /// <summary>
    /// 楼层变化事件处理
    /// </summary>
    private void OnCurrentFloorChanged(int oldFloor, int newFloor)
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.Contains("Menu") || sceneName.Contains("MainMenu"))
        {
            return;
        }

        Debug.Log($"AudioManager: 楼层发生变化 - 从 {oldFloor} 层到 {newFloor} 层");
        PlayFloorMusic(newFloor);
    }
    
    /// <summary>
    /// 播放对应楼层的音乐
    /// </summary>
    private void PlayFloorMusic(int floor)
    {
        Debug.Log($"AudioManager: PlayFloorMusic被调用，楼层: {floor}");

        EnsureAudioSource();
        
        if (backgroundMusic == null || backgroundMusic.Length == 0)
        {
            Debug.LogError("AudioManager: 背景音乐列表为空");
            return;
        }
        
        // 检查楼层是否有效（1-4层）
        if (floor < 1 || floor > 4)
        {
            Debug.LogWarning($"AudioManager: 楼层 {floor} 无效，有效范围为1-4层");
            // 使用默认楼层1
            floor = 1;
        }
        
        // 楼层从1开始，数组索引从0开始，所以需要减1
        int musicIndex = floor - 1;
        
        // 检查索引是否有效
        if (musicIndex < 0 || musicIndex >= backgroundMusic.Length)
        {
            Debug.LogError($"AudioManager: 楼层 {floor} 对应的音乐索引 {musicIndex} 无效（音乐列表长度: {backgroundMusic.Length}）");
            return;
        }
        
        AudioClip musicToPlay = backgroundMusic[musicIndex];
        if (musicToPlay == null)
        {
            Debug.LogError($"AudioManager: 楼层 {floor} 对应的音乐为空（索引: {musicIndex}）");
            return;
        }
        
        // 如果正在播放相同的音乐，则不重复播放
        if (audioSource != null && audioSource.clip == musicToPlay && audioSource.isPlaying)
        {
            Debug.Log($"AudioManager: 楼层 {floor} 的音乐已在播放，跳过切换");
            return;
        }
        
        // 切换音乐
        Debug.Log($"AudioManager: 开始切换音乐 - 停止当前音乐，设置新音乐: {musicToPlay.name}");
        audioSource.Stop();
        audioSource.clip = musicToPlay;
        audioSource.Play();
        
        // 检查是否成功播放
        if (audioSource.isPlaying)
        {
            Debug.Log($"AudioManager: 成功切换到楼层 {floor} 的背景音乐: {musicToPlay.name}，音量: {audioSource.volume}");
        }
        else
        {
            Debug.LogError($"AudioManager: 音乐切换失败，AudioSource未播放 - 剪辑: {musicToPlay.name}");
        }
    }
    
    /// <summary>
    /// 播放主菜单音乐
    /// </summary>
    public void PlayMenuMusic()
    {
        if (menuMusic == null)
        {
            Debug.LogWarning("AudioManager: 主菜单音乐未配置");
            return;
        }

        EnsureAudioSource();
        
        // 如果正在播放相同的音乐，则不重复播放
        if (audioSource.clip == menuMusic && audioSource.isPlaying)
        {
            Debug.Log("AudioManager: 主菜单音乐已在播放，跳过切换");
            return;
        }
        
        // 切换音乐
        audioSource.Stop();
        audioSource.clip = menuMusic;
        audioSource.Play();
        
        Debug.Log($"AudioManager: 开始播放主菜单音乐: {menuMusic.name}");
    }
    
    /// <summary>
    /// 播放音效（按钮点击等）
    /// </summary>
    public void PlaySFX(AudioClip sfxClip)
    {
        if (sfxClip == null)
        {
            Debug.LogWarning("AudioManager: 音效剪辑为空");
            return;
        }
        
        // 创建一个临时的AudioSource来播放音效，不影响背景音乐
        AudioSource tempSource = gameObject.AddComponent<AudioSource>();
        tempSource.clip = sfxClip;
        tempSource.volume = audioSource != null ? audioSource.volume : 1.0f;
        tempSource.Play();
        
        // 播放完成后销毁临时AudioSource
        Destroy(tempSource, sfxClip.length);
        
        Debug.Log($"AudioManager: 播放音效: {sfxClip.name}");
    }
    
    /// <summary>
    /// 设置主音量
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }
    
    /// <summary>
    /// 设置背景音乐音量
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }
    
    /// <summary>
    /// 设置音效音量
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        // 音效音量设置会影响后续播放的音效
        // 这里可以存储音量设置，供PlaySFX方法使用
        if (audioSource != null)
        {
            // 暂时使用主音量设置
            audioSource.volume = volume;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 可以在这里添加其他音频控制逻辑
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        RoomStateMachine_cza roomStateMachine = RoomStateMachine_cza.Instance;
        if (roomStateMachine != null)
        {
            roomStateMachine.OnCurrentFloorChanged -= OnCurrentFloorChanged;
        }
    }
}
