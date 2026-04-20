using UnityEngine;

/// <summary>
/// Runtime-only audio settings for the current play session.
/// </summary>
public static class AudioRuntimeSettings
{
    private const float DEFAULT_VOLUME = 0.5f;

    private static float masterVolume = DEFAULT_VOLUME;

    public static float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp01(value);
            Debug.Log($"[AudioRuntimeSettings] MasterVolume = {masterVolume:F2}");
        }
    }

    public static void ResetToDefault()
    {
        masterVolume = DEFAULT_VOLUME;
        Debug.Log($"[AudioRuntimeSettings] Reset master volume to {DEFAULT_VOLUME:F2}");
    }

    public static void ClearAll()
    {
        ResetToDefault();
    }

    public static string GetDebugInfo()
    {
        return $"MasterVolume: {masterVolume:F2}";
    }
}
