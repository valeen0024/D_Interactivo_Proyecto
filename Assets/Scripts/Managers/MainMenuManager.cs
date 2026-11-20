using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Options UI Elements")]
    public Slider volumeMaster;
    public Toggle muteToggle;
    public TMP_Dropdown qualityDropdown;
    public AudioMixer mixer;
    [SerializeField] private string musicExposedParam = "VolumeMaster";

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject creditsPanel;

    [Header("Scenes")]
    [SerializeField] private string nameInputScene = "NameInput";

    private const string MUSIC_PREF_KEY = "MusicVolumeDB";

    private void Start()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);

        if (volumeMaster != null)
        {
            volumeMaster.onValueChanged.RemoveAllListeners();
            volumeMaster.onValueChanged.AddListener(ChangeVolumeMaster);
        }

        LoadSavedPreferences();
    }

    private void LoadSavedPreferences()
    {
        float savedMusicVol = PlayerPrefs.GetFloat(MUSIC_PREF_KEY, 0f);
        if (volumeMaster != null)
        {
            volumeMaster.SetValueWithoutNotify(savedMusicVol);
            ChangeVolumeMaster(savedMusicVol);
        }

        bool savedMute = PlayerPrefs.GetInt("GameMuted", 0) == 1;
        if (muteToggle != null)
        {
            muteToggle.SetIsOnWithoutNotify(savedMute);
        }

        int savedQuality = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
        if (qualityDropdown != null)
        {
            qualityDropdown.SetValueWithoutNotify(savedQuality);
            QualitySettings.SetQualityLevel(savedQuality);
        }
    }

    public void ChangeVolumeMaster(float sliderValue)
    {
        if (mixer != null && !string.IsNullOrEmpty(musicExposedParam))
        {
            bool ok = mixer.SetFloat(musicExposedParam, sliderValue);
            Debug.Log($"[MainMenuManager] SetFloat('{musicExposedParam}', {sliderValue}) -> {ok}");
        }

        PlayerPrefs.SetFloat(MUSIC_PREF_KEY, sliderValue);
        PlayerPrefs.Save();
    }

    // ==========================================
    // 2. CONTROL DE MUTE GLOBAL
    // ==========================================
    public void SetMute(bool isMuted)
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetMute(isMuted);
        }

        if (mixer != null && !string.IsNullOrEmpty(musicExposedParam))
        {
            float targetdB = isMuted ? -80f : PlayerPrefs.GetFloat(MUSIC_PREF_KEY, 0f);
            mixer.SetFloat(musicExposedParam, targetdB);
        }

        PlayerPrefs.SetInt("GameMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ==========================================
    // 3. CALIDAD GRÁFICA (Baja, Media, Alta)
    // ==========================================
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex, true);

        switch (qualityIndex)
        {
            case 0:
                QualitySettings.vSyncCount = 0;
                QualitySettings.antiAliasing = 0;
                Application.targetFrameRate = 30;
                break;

            case 1:
                QualitySettings.vSyncCount = 1;
                QualitySettings.antiAliasing = 2;
                Application.targetFrameRate = 60;
                break;

            case 2:
                QualitySettings.vSyncCount = 1;
                QualitySettings.antiAliasing = 4;
                Application.targetFrameRate = -1;
                break;
        }

        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
        PlayerPrefs.Save();

        Debug.Log("Calidad cambiada a índice: " + qualityIndex + " (AntiAliasing y FPS ajustados)");
    }

    // ==========================================
    // GESTIÓN DE PANELES Y ESCENAS
    // ==========================================
    public void OpenOptions()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);

        if (volumeMaster != null)
        {
            float currentVol = PlayerPrefs.GetFloat(MUSIC_PREF_KEY, 0f);
            volumeMaster.SetValueWithoutNotify(currentVol);
        }
    }

    public void OpenMainPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    public void ToggleCredits()
    {
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(!creditsPanel.activeSelf);
        }
    }

    public void CloseCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(nameInputScene);
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}