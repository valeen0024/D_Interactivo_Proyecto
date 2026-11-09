using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    public AudioClip menuMusic;
    public AudioClip gameplayMusic;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();

            if (audioSource != null)
            {
                audioSource.volume = 1f;

                // Cargar el estado de Mute guardado previamente (global para todas las escenas)
                bool isMuted = PlayerPrefs.GetInt("GameMuted", 0) == 1;
                audioSource.mute = isMuted;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PlayMusic(menuMusic);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Gameplay")
        {
            PlayMusic(gameplayMusic);
        }
        else
        {
            PlayMusic(menuMusic);
        }
    }

    void PlayMusic(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;

        if (audioSource.clip == clip && audioSource.isPlaying)
            return;

        audioSource.clip = clip;
        audioSource.Play();
    }

    // Método público que llamará tu MainMenuManager para mutear/desmutear globalmente
    public void SetMute(bool isMuted)
    {
        if (audioSource != null)
        {
            audioSource.mute = isMuted;
            PlayerPrefs.SetInt("GameMuted", isMuted ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}