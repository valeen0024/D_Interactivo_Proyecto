using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject creditsPanel;

    [Header("Scenes")]
    // CAMBIO AQUÍ: Ahora por defecto carga la escena del nombre
    [SerializeField] private string nameInputScene = "NameInput";

    private void Start()
    {
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(false);
        }
    }

    public void StartGame()
    {
        // CAMBIO AQUÍ: Carga la escena guardada en la variable
        SceneManager.LoadScene(nameInputScene);
    }

    public void OpenCredits()
    {
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(true);
        }
    }

    public void CloseCredits()
    {
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(false);
        }
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
