using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Options UI Elements")]
    public Slider volumeMaster;
    public Slider volumeFX;
    public Toggle muteToggle;
    public TMPro.TMP_Dropdown qualityDropdown;
    public AudioMixer mixer;

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject creditsPanel;

    [Header("Scenes")]
    [SerializeField] private string characterSelectionScene = "CharacterSelection";

    private void Start()
    {
        // Estado inicial de los paneles
        if (mainPanel != null) mainPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);

        // Cargar valores guardados al iniciar
        LoadSavedPreferences();
    }

    private void LoadSavedPreferences()
    {
        float savedMasterVol = PlayerPrefs.GetFloat("MasterVolume", 100f);
        if (volumeMaster != null)
        {
            volumeMaster.SetValueWithoutNotify(savedMasterVol);
            ChangeVolumeMaster(savedMasterVol);
        }

        float savedFXVol = PlayerPrefs.GetFloat("FXVolume", 100f);
        if (volumeFX != null)
        {
            volumeFX.SetValueWithoutNotify(savedFXVol);
            ChangeVolumeFX(savedFXVol);
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

    // ==========================================
    // CONTROL DE VOLUMEN CON AUDIOMIXER
    // ==========================================
    public void ChangeVolumeMaster(float sliderValue)
    {
        float volumeIndB = sliderValue > 0 ? Mathf.Log10(sliderValue / 100f) * 20f : -80f;

        if (mixer != null)
        {
            mixer.SetFloat("VolMaster", volumeIndB);
        }

        PlayerPrefs.SetFloat("MasterVolume", sliderValue);
        PlayerPrefs.Save();
    }

    public void ChangeVolumeFX(float sliderValue)
    {
        float volumeIndB = sliderValue > 0 ? Mathf.Log10(sliderValue / 100f) * 20f : -80f;

        if (mixer != null)
        {
            mixer.SetFloat("VolFX", volumeIndB);
        }

        PlayerPrefs.SetFloat("FXVolume", sliderValue);
        PlayerPrefs.Save();
    }

    // ==========================================
    // CONTROL DE MUTE GLOBAL
    // ==========================================
    public void SetMute(bool isMuted)
    {
        // 1. Silenciar o activar el MusicManager global
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetMute(isMuted);
        }

        // 2. Opcional pero recomendado: Bajar el AudioMixer a -80dB si se mutea, o restaurarlo a 0dB si se desmutea
        if (mixer != null)
        {
            float targetdB = isMuted ? -80f : 0f;
            mixer.SetFloat("MasterVolume", targetdB);
        }

        // Guardar la preferencia
        PlayerPrefs.SetInt("GameMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ==========================================
    // CALIDAD GRÁFICA (Baja, Media, Alta)
    // ==========================================
    public void SetQuality(int qualityIndex)
    {
        // 1. Cambiar el nivel de calidad global de Unity
        QualitySettings.SetQualityLevel(qualityIndex);

        // 2. Ajustar texturas y resolución según la calidad seleccionada para forzar el cambio visual
        if (qualityIndex == 0) // Baja
        {
            QualitySettings.globalTextureMipmapLimit = 1; // Baja la resolución de texturas a la mitad
            QualitySettings.vSyncCount = 0;              // Desactiva VSync para ganar rendimiento
        }
        else if (qualityIndex == 1) // Media
        {
            QualitySettings.globalTextureMipmapLimit = 0;
            QualitySettings.vSyncCount = 1;
        }
        else if (qualityIndex == 2) // Alta
        {
            QualitySettings.globalTextureMipmapLimit = 0;
            QualitySettings.vSyncCount = 1;
        }

        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
        PlayerPrefs.Save();

        Debug.Log("Calidad cambiada a índice: " + qualityIndex);
    }

    // GESTIÓN DE PANELES Y ESCENAS
    // ==========================================
    public void OpenOptions()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);

        // Forzar a que el slider lea el valor actual de las preferencias al abrir el panel
        if (volumeMaster != null)
        {
            float currentVol = PlayerPrefs.GetFloat("MasterVolume", 100f);
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
        SceneManager.LoadScene(characterSelectionScene);
    }

    public void ExitGame()
    {
        Debug.Log("Cerrando juego...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}