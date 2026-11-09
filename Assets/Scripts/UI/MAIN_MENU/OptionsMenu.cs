using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    // Referencia al panel completo de opciones
    public GameObject optionsPanel;

    // 1. Control de Volumen General
    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    // 2. Control de Mute (Silenciar)
    public void SetMute(bool isMuted)
    {
        AudioListener.pause = isMuted;
    }

    // 3. Control de Calidad Gráfica
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    // 4. Abrir / Cerrar el Panel de Opciones
    public void OpenOptions()
    {
        optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
    }
}
