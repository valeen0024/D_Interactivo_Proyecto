using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("AudioMixer Principal")]
    [SerializeField] private AudioMixer mainAudioMixer;
    [SerializeField] private string musicExposedParam = "VolumeMaster";
    [SerializeField] private AudioMixerGroup musicMixerGroup;

    [SerializeField] private string sfxExposedParam = "SFXVolume";
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Música (dejar en None si ya la reproduce MusicManager)")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private string gameplaySceneName = "Gameplay";
    [Range(0f, 1f)][SerializeField] private float musicBaseVolume = 0.7f;

    [Header("Efectos (SFX)")]
    [SerializeField] private AudioClip fuelPickupClip;
    [SerializeField] private AudioClip oilSlipClip;
    [SerializeField] private AudioClip turboClip;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private const string MUSIC_PREF_KEY = "MusicVolumeDB";
    private const string SFX_PREF_KEY = "SFXVolumeDB";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicBaseVolume;
        musicSource.outputAudioMixerGroup = musicMixerGroup;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.outputAudioMixerGroup = sfxMixerGroup;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        float savedMusic = PlayerPrefs.GetFloat(MUSIC_PREF_KEY, 0f);
        float savedSfx = PlayerPrefs.GetFloat(SFX_PREF_KEY, 0f);

        SetMusicVolumeDB(savedMusic);
        SetSFXVolumeDB(savedSfx);

        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    public void PlayMusicForScene(string sceneName)
    {
        bool isGameplayScene = sceneName == gameplaySceneName || sceneName == "Level2";
        AudioClip target = isGameplayScene ? gameplayMusic : mainMenuMusic;

        if (target == null)
        {
            if (musicSource.isPlaying) musicSource.Stop();
            return;
        }

        if (musicSource.clip == target && musicSource.isPlaying) return;

        musicSource.clip = target;
        musicSource.volume = musicBaseVolume;
        musicSource.outputAudioMixerGroup = musicMixerGroup;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.outputAudioMixerGroup = sfxMixerGroup;
        sfxSource.PlayOneShot(clip, volumeScale);
    }

    public void PlayFuelPickup(float volumeScale = 1f) => PlaySFX(fuelPickupClip, volumeScale);
    public void PlayOilSlip(float volumeScale = 1f) => PlaySFX(oilSlipClip, volumeScale);
    public void PlayTurbo(float volumeScale = 1f) => PlaySFX(turboClip, volumeScale);

    public void SetMusicVolumeDB(float dbValue)
    {
        if (mainAudioMixer != null && !string.IsNullOrEmpty(musicExposedParam))
        {
            mainAudioMixer.SetFloat(musicExposedParam, dbValue);
        }

        PlayerPrefs.SetFloat(MUSIC_PREF_KEY, dbValue);
        PlayerPrefs.Save();
    }

    public void SetSFXVolumeDB(float dbValue)
    {
        if (mainAudioMixer != null && !string.IsNullOrEmpty(sfxExposedParam))
        {
            mainAudioMixer.SetFloat(sfxExposedParam, dbValue);
        }

        PlayerPrefs.SetFloat(SFX_PREF_KEY, dbValue);
        PlayerPrefs.Save();
    }
}