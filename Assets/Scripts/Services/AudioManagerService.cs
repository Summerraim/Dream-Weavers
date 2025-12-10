using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManagerService : Singleton<AudioManagerService>
{
    private AudioSource bgmSource;

    protected override void Awake()
    {
        base.Awake();
        // 确保有 AudioSource 用于 BGM
        bgmSource = gameObject.GetComponent<AudioSource>();
        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();
        DontDestroyOnLoad(gameObject);
    }

    public void PlayBGM(string clipName)
    {
        AudioClip clip = Resources.Load<AudioClip>($"Audio/BGM/{clipName}");
        if (clip == null)
            return;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void PlaySFX(string clipName)
    {
        AudioClip clip = Resources.Load<AudioClip>($"Audio/SFX/{clipName}");
        if (clip == null)
            return;
        AudioSource.PlayClipAtPoint(clip, Vector3.zero);
    }

    public void PauseSFX()
    {
        // 简单处理：暂停 BGM
        if (bgmSource.isPlaying)
            bgmSource.Pause();
    }

    public void ResumeSFX()
    {
        if (!bgmSource.isPlaying && bgmSource.clip != null)
            bgmSource.UnPause();
    }
}
