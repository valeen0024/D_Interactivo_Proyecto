using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class NameInputManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField inputNombre;
    [SerializeField] private TextMeshProUGUI textAyuda; // Referencia al texto de ayuda

    [Header("Settings")]
    [SerializeField] private int minCharacters = 3;

    private void Update()
    {
        if (Keyboard.current == null) return;

        // 1. PRESIONAR ENTER PARA CONTINUAR
        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            ConfirmName();
        }

        // 2. PRESIONAR ESC PARA SALIR
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitToMenu();
        }
    }

    public void ConfirmName()
    {
        // Obtenemos el texto y le quitamos los espacios en blanco al principio y al final
        string nombreIngresado = inputNombre.text.Trim();

        // Validamos el mínimo de caracteres
        if (nombreIngresado.Length < minCharacters)
        {
            // Mostramos el error en rojo
            if (textAyuda != null)
            {
                textAyuda.text = "El nombre debe tener al menos " + minCharacters + " caracteres.";
                textAyuda.color = Color.white;
            }
            return; // Detenemos la función para que no avance de escena
        }

        // Si pasa la validación, guardamos el nombre
        GameData.PlayerName = nombreIngresado;

        Debug.Log("Nombre guardado: " + GameData.PlayerName);

        // Cargamos la escena de selección de personaje
        SceneManager.LoadScene("CharacterSelection");
    }

    public void ExitToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}