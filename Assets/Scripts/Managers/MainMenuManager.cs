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

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject creditsPanel;

    [Header("Scenes")]
    [SerializeField] private string nameInputScene = "NameInput";

    private void Start()
    {
        // Estado inicial de los paneles
        if (mainPanel != null) mainPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);

        // Cargar valores guardados al iniciar el juego
        LoadSavedPreferences();
    }

    private void LoadSavedPreferences()
    {
        // Por defecto lo ponemos en 0 (volumen máximo/normal en decibelios) o el valor guardado
        float savedMasterVol = PlayerPrefs.GetFloat("MasterVolume", 0f);
        if (volumeMaster != null)
        {
            volumeMaster.SetValueWithoutNotify(savedMasterVol);
            ChangeVolumeMaster(savedMasterVol);
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
        if (mixer != null)
        {
            mixer.SetFloat("MasterVolume", sliderValue);
        }

        PlayerPrefs.SetFloat("MasterVolume", sliderValue);
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

        if (mixer != null)
        {
            float targetdB = isMuted ? -80f : 0f;
            mixer.SetFloat("MasterVolume", targetdB);
        }

        PlayerPrefs.SetInt("GameMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ==========================================
    // 3. CALIDAD GRÁFICA (Baja, Media, Alta)
    // ==========================================
    // ==========================================
    // CALIDAD GRÁFICA (Baja, Media, Alta)
    // ==========================================
    public void SetQuality(int qualityIndex)
    {
        // Cambia el nivel de calidad global de Unity y fuerza su aplicación
        QualitySettings.SetQualityLevel(qualityIndex, true);

        // Aplicamos cambios reales y perceptibles según la opción elegida
        switch (qualityIndex)
        {
            case 0: // Baja
                QualitySettings.vSyncCount = 0;           // Desactiva sincronización vertical
                QualitySettings.antiAliasing = 0;         // Sin suavizado de bordes (mejora rendimiento, bordes más pixelados)
                Application.targetFrameRate = 30;         // Limita a 30 FPS para ahorrar recursos
                break;

            case 1: // Media
                QualitySettings.vSyncCount = 1;           // Activa VSync estándar
                QualitySettings.antiAliasing = 2;         // Suavizado de bordes medio (2x)
                Application.targetFrameRate = 60;         // Limita a 60 FPS estables
                break;

            case 2: // Alta
                QualitySettings.vSyncCount = 1;           // Activa VSync
                QualitySettings.antiAliasing = 4;         // Suavizado de bordes alto (4x, máxima definición)
                Application.targetFrameRate = -1;         // Sin límite de FPS (máxima fluidez)
                break;
        }

        // Guardar la preferencia en el equipo
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
