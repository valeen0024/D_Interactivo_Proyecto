using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Options")]
    public Slider volumeFX;
    public Slider volumeMaster;
    public Toggle mute;
    public AudioMixer mixer; // Referencia al AudioMixer del video

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject levelSelectPanel;

    [Header("Scenes")]
    [SerializeField] private string characterSelectionScene = "CharacterSelection";

    private void Start()
    {
        // Estado inicial de los paneles
        if (mainPanel != null) mainPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);

        // Inicializar la barra en 100 por defecto si existe
        if (volumeMaster != null)
        {
            volumeMaster.SetValueWithoutNotify(100f);
        }
    }

    // Método universal del video para cambiar de paneles
    public void OpenPanel(GameObject panel)
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);

        if (panel != null) panel.SetActive(true);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(characterSelectionScene);
    }

    // ==========================================
    // CONTROL DE VOLUMEN MAESTRO CON AUDIOMIXER
    // ==========================================
    public void ChangeVolumeMaster(float sliderValue)
    {
        // Convertimos el valor de la barra (0 a 100) a decibelios reales (-80dB a 0dB)
        // Esto evita que el volumen se apague bruscamente y le da una escala suave al jugador
        float volumeIndB = sliderValue > 0 ? Mathf.Log10(sliderValue / 100f) * 20f : -80f;

        if (mixer != null)
        {
            mixer.SetFloat("VolMaster", volumeIndB);
        }
    }

    // ==========================================
    // CONTROL DE VOLUMEN FX CON AUDIOMIXER
    // ==========================================
    public void ChangeVolumeFX(float sliderValue)
    {
        float volumeIndB = sliderValue > 0 ? Mathf.Log10(sliderValue / 100f) * 20f : -80f;

        if (mixer != null)
        {
            mixer.SetFloat("VolFX", volumeIndB);
        }
    }

    // ==========================================
    // CONTROL DE MUTE
    // ==========================================
    public void SetMute(bool isMuted)
    {
        if (MusicManager.Instance != null)
        {
            AudioSource source = MusicManager.Instance.GetComponent<AudioSource>();
            if (source != null)
            {
                source.mute = isMuted;
            }
        }
    }

    // ==========================================
    // CALIDAD GRÁFICA
    // ==========================================
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
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