using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MainMenuKeyboard : MonoBehaviour
{
    [Header("Botones del Menú Principal")]
    [SerializeField] private Button buttonStart;
    [SerializeField] private Button buttonExit;

    [Header("Paneles secundarios (opcional)")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionsMenuPanel;
    [SerializeField] private GameObject creditsPanel;

    private void Update()
    {
        // Evita activar Comenzar o Salir si el jugador está dentro de Opciones o Créditos
        if (mainPanel != null && !mainPanel.activeInHierarchy) return;
        if (optionsMenuPanel != null && optionsMenuPanel.activeInHierarchy) return;
        if (creditsPanel != null && creditsPanel.activeInHierarchy) return;

        if (IsEnterPressed() && buttonStart != null && buttonStart.interactable)
        {
            buttonStart.onClick.Invoke();
        }
        else if (IsEscapePressed() && buttonExit != null && buttonExit.interactable)
        {
            buttonExit.onClick.Invoke();
        }
    }

    private bool IsEnterPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            return true;
        }
#endif
        return false;
    }

    private bool IsEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            return true;
        }
#endif
        return false;
    }
}