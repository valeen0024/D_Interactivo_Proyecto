using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class NameInputManager : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TMP_InputField inputNombre;

    // Esta función la llamaremos desde el botón "Continuar"
    public void ConfirmName()
    {
        // Si el jugador escribió algo, lo guardamos
        if (inputNombre != null && !string.IsNullOrEmpty(inputNombre.text))
        {
            GameData.PlayerName = inputNombre.text;
        }
        else
        {
            // Si lo deja vacío, ponemos un nombre por defecto
            GameData.PlayerName = "Jugador 1";
        }

        Debug.Log("Nombre guardado: " + GameData.PlayerName);

        // Cargamos la escena de selección de personaje
        SceneManager.LoadScene("CharacterSelection");
    }

    // Esta función la llamaremos desde el botón "Salir"
    public void ExitToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}