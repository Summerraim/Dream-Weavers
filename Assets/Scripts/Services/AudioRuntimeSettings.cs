using UnityEngine;

/// <summary>
/// 运行时音量设置管理器（仅在当前游戏会话中有效）
/// 每次游戏开始时自动重置为默认值
/// </summary>
public static class AudioRuntimeSettings
{
    private static float masterVolume = 0.5f;
    private static float musicVolume = 0.5f;
    private static float sfxVolume = 0.5f;

    // 默认音量值
    private const float DEFAULT_VOLUME = 0.5f;

    /// <summary>
    /// 主音量
    /// </summary>
    public static float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp01(value);
            Debug.Log($"[AudioRuntimeSettings] 设置主音量: {masterVolume:F2}");
        }
    }

    /// <summary>
    /// 背景音乐音量
    /// </summary>
    public static float MusicVolume
    {
        get => musicVolume;
        set
        {
            musicVolume = Mathf.Clamp01(value);
            Debug.Log($"[AudioRuntimeSettings] 设置音乐音量: {musicVolume:F2}");
        }
    }

    /// <summary>
    /// 音效音量
    /// </summary>
    public static float SFXVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            Debug.Log($"[AudioRuntimeSettings] 设置音效音量: {sfxVolume:F2}");
        }
    }

    /// <summary>
    /// 重置所有音量到默认值
    /// </summary>
    public static void ResetToDefault()
    {
        masterVolume = DEFAULT_VOLUME;
        musicVolume = DEFAULT_VOLUME;
        sfxVolume = DEFAULT_VOLUME;
        Debug.Log($"[AudioRuntimeSettings] 已重置所有音量到默认值: {DEFAULT_VOLUME:F2}");
    }

    /// <summary>
    /// 清除所有设置（与ResetToDefault相同）
    /// </summary>
    public static void ClearAll()
    {
        ResetToDefault();
    }

    /// <summary>
    /// 获取当前所有音量设置（用于调试）
    /// </summary>
    public static string GetDebugInfo()
    {
        return $"MasterVolume: {masterVolume:F2}, MusicVolume: {musicVolume:F2}, SFXVolume: {sfxVolume:F2}";
    }
}
