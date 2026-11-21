using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    [Header("Paneles y Botón HUD")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject hudPauseButton;

    [Header("Respaldo AudioMixer (si no hay AudioManager)")]
    [SerializeField] private AudioMixer fallbackMixer;
    [SerializeField] private string musicExposedParam = "VolumeMaster";
    [SerializeField] private string sfxExposedParam = "SFXVolume";

    [Header("Sliders de Audio")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Escena Menú")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private const string MUSIC_PREF_KEY = "MusicVolumeDB";
    private const string SFX_PREF_KEY = "SFXVolumeDB";
    private bool isPaused = false;
    private bool isTransitioning = false;

    private void Start()
    {
        Time.timeScale = 1f;
        isPaused = false;
        isTransitioning = false;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (hudPauseButton != null) hudPauseButton.SetActive(true);

        float savedMusic = PlayerPrefs.GetFloat(MUSIC_PREF_KEY, 0f);
        float savedSfx = PlayerPrefs.GetFloat(SFX_PREF_KEY, 0f);

        Debug.Log($"[PauseManager Start] savedMusic={savedMusic} | savedSfx={savedSfx} | AudioManager.Instance={(AudioManager.Instance != null ? "EXISTE" : "NULL")} | fallbackMixer={(fallbackMixer != null ? fallbackMixer.name : "NULL")}");

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(savedMusic);
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }
        SetMusicVolume(savedMusic);

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(savedSfx);
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }
        SetSFXVolume(savedSfx);
    }

    private void Update()
    {
        if (isTransitioning) return;

        if (Keyboard.current != null &&
            (Keyboard.current.escapeKey.wasPressedThisFrame ||
             Keyboard.current.pKey.wasPressedThisFrame))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused || isTransitioning) return;

        isPaused = true;
        Time.timeScale = 0f;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        if (hudPauseButton != null) hudPauseButton.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("[PauseManager] Juego pausado.");
    }

    public void ResumeGame()
    {
        if (isTransitioning) return;

        isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (hudPauseButton != null) hudPauseButton.SetActive(true);
        Debug.Log("[PauseManager] Juego reanudado.");
    }

    public void RestartLevel()
    {
        if (isTransitioning) return;

        isTransitioning = true;
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        if (isTransitioning) return;

        isTransitioning = true;
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void SetMusicVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolumeDB(value);
            Debug.Log($"[SetMusicVolume] Llamó a AudioManager.Instance.SetMusicVolumeDB({value})");
        }
        else if (fallbackMixer != null && !string.IsNullOrEmpty(musicExposedParam))
        {
            bool ok = fallbackMixer.SetFloat(musicExposedParam, value);
            Debug.Log($"[SetMusicVolume] fallbackMixer.SetFloat('{musicExposedParam}', {value}) -> resultado: {ok}");
        }
        else
        {
            Debug.LogWarning("[SetMusicVolume] No hay AudioManager ni fallbackMixer asignado.");
        }

        PlayerPrefs.SetFloat(MUSIC_PREF_KEY, value);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolumeDB(value);
            Debug.Log($"[SetSFXVolume] Llamó a AudioManager.Instance.SetSFXVolumeDB({value})");
        }
        else if (fallbackMixer != null && !string.IsNullOrEmpty(sfxExposedParam))
        {
            bool ok = fallbackMixer.SetFloat(sfxExposedParam, value);
            Debug.Log($"[SetSFXVolume] fallbackMixer.SetFloat('{sfxExposedParam}', {value}) -> resultado: {ok}");
        }
        else
        {
            Debug.LogWarning("[SetSFXVolume] No hay AudioManager ni fallbackMixer asignado.");
        }

        PlayerPrefs.SetFloat(SFX_PREF_KEY, value);
        PlayerPrefs.Save();
    }
}