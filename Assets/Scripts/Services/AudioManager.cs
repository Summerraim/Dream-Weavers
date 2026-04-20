using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private AudioSource audioSource;

    [Header("Background Music")]
    [SerializeField]
    private AudioClip[] backgroundMusic;

    [Header("Menu Music")]
    [SerializeField]
    private AudioClip menuMusic;

    private void Awake()
    {
        EnsureAudioSource();
    }

    private void Start()
    {
        EnsureAudioSource();

        if (backgroundMusic == null || backgroundMusic.Length == 0)
        {
            Debug.LogError("AudioManager: backgroundMusic is empty.");
        }

        StartCoroutine(WaitForRoomStateMachine());
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
        audioSource.volume = AudioRuntimeSettings.MasterVolume;
    }

    private IEnumerator WaitForRoomStateMachine()
    {
        yield return new WaitUntil(() => RoomStateMachine_cza.Instance != null);

        RoomStateMachine_cza.Instance.OnCurrentFloorChanged += OnCurrentFloorChanged;

        var sceneName = SceneManager.GetActiveScene().name;
        if (!sceneName.Contains("Menu") && !sceneName.Contains("MainMenu"))
        {
            PlayFloorMusic(RoomStateMachine_cza.Instance.CurrentFloor);
        }
    }

    private void OnCurrentFloorChanged(int oldFloor, int newFloor)
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.Contains("Menu") || sceneName.Contains("MainMenu"))
        {
            return;
        }

        PlayFloorMusic(newFloor);
    }

    private void PlayFloorMusic(int floor)
    {
        EnsureAudioSource();

        if (backgroundMusic == null || backgroundMusic.Length == 0)
        {
            Debug.LogError("AudioManager: backgroundMusic is empty.");
            return;
        }

        if (floor < 1 || floor > 4)
        {
            Debug.LogWarning($"AudioManager: invalid floor {floor}, fallback to floor 1.");
            floor = 1;
        }

        int musicIndex = floor - 1;
        if (musicIndex < 0 || musicIndex >= backgroundMusic.Length)
        {
            Debug.LogError($"AudioManager: missing music for floor {floor}, index {musicIndex}.");
            return;
        }

        AudioClip musicToPlay = backgroundMusic[musicIndex];
        if (musicToPlay == null)
        {
            Debug.LogError($"AudioManager: clip is null for floor {floor}.");
            return;
        }

        if (audioSource.clip == musicToPlay && audioSource.isPlaying)
        {
            return;
        }

        audioSource.Stop();
        audioSource.clip = musicToPlay;
        audioSource.Play();
    }

    public void PlayMenuMusic()
    {
        if (menuMusic == null)
        {
            Debug.LogWarning("AudioManager: menuMusic is not assigned.");
            return;
        }

        EnsureAudioSource();

        if (audioSource.clip == menuMusic && audioSource.isPlaying)
        {
            return;
        }

        audioSource.Stop();
        audioSource.clip = menuMusic;
        audioSource.Play();
    }

    public void SetMasterVolume(float volume)
    {
        EnsureAudioSource();
        audioSource.volume = Mathf.Clamp01(volume);
    }

    private void OnDestroy()
    {
        RoomStateMachine_cza roomStateMachine = RoomStateMachine_cza.Instance;
        if (roomStateMachine != null)
        {
            roomStateMachine.OnCurrentFloorChanged -= OnCurrentFloorChanged;
        }
    }
}
