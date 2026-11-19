using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI Pausa y HUD")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject hudPauseButton;

    [Header("Audio (igual que en Main Menu)")]
    [SerializeField] private AudioMixer musicMixer;
    [SerializeField] private string musicExposedParam = "VolumeMaster"; // Pon aquí el nombre exacto que usa tu Main Menu
    [SerializeField] private AudioSource fallbackMusicSource;         // Opcional: si tu música va por AudioSource directo

    [SerializeField] private AudioMixer sfxMixer;
    [SerializeField] private string sfxExposedParam = "SFXVolume";     // Nombre expuesto en el grupo SFX

    [Header("Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Escena Menú")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private const string MUSIC_PREF_KEY = "MusicVolume";
    private const string SFX_PREF_KEY = "SFXVolume";
    private bool isPaused = false;

    private void Start()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (hudPauseButton != null) hudPauseButton.SetActive(true);

        float savedMusic = PlayerPrefs.GetFloat(MUSIC_PREF_KEY, 0.75f);
        float savedSfx = PlayerPrefs.GetFloat(SFX_PREF_KEY, 0.75f);

        if (musicSlider != null)
        {
            musicSlider.minValue = 0.0001f;
            musicSlider.maxValue = 1f;
            musicSlider.SetValueWithoutNotify(savedMusic);
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }
        ApplyMusicVolume(savedMusic);

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0.0001f;
            sfxSlider.maxValue = 1f;
            sfxSlider.SetValueWithoutNotify(savedSfx);
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }
        ApplySFXVolume(savedSfx);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        if (hudPauseButton != null) hudPauseButton.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (hudPauseButton != null) hudPauseButton.SetActive(true);
    }

    public void RestartLevel()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void GoToMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void SetMusicVolume(float value)
    {
        ApplyMusicVolume(value);
        PlayerPrefs.SetFloat(MUSIC_PREF_KEY, value);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        ApplySFXVolume(value);
        PlayerPrefs.SetFloat(SFX_PREF_KEY, value);
        PlayerPrefs.Save();
    }

    private void ApplyMusicVolume(float value)
    {
        float clamped = Mathf.Clamp(value, 0.0001f, 1f);
        if (musicMixer != null && !string.IsNullOrEmpty(musicExposedParam))
        {
            musicMixer.SetFloat(musicExposedParam, Mathf.Log10(clamped) * 20f);
        }
        if (fallbackMusicSource != null)
        {
            fallbackMusicSource.volume = value;
        }
    }

    private void ApplySFXVolume(float value)
    {
        float clamped = Mathf.Clamp(value, 0.0001f, 1f);
        if (sfxMixer != null && !string.IsNullOrEmpty(sfxExposedParam))
        {
            sfxMixer.SetFloat(sfxExposedParam, Mathf.Log10(clamped) * 20f);
        }
    }
}