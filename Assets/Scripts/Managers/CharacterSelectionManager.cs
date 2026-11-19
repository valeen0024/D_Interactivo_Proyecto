using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Selection Borders")]
    [SerializeField] private RectTransform playerBorder;
    [SerializeField] private RectTransform cpuBorder;

    [Header("Color Points")]
    [SerializeField] private Transform[] colorPoints;

    [Header("Locked Overlays")]
    [SerializeField] private GameObject[] lockOverlays;

    [Header("CPU Settings")]
    [SerializeField] private float cpuMoveSpeed = 0.08f;
    [SerializeField] private float cpuThinkingTime = 1.2f;

    [Header("Vista Previa del Carro")]
    [SerializeField] private Image imagenCarroUI; 
    [SerializeField] private Sprite[] spritesCarro; 

    [Header("UI Jugador")]
    [SerializeField] private TextMeshProUGUI playerNameText;       // El texto que está afuera (opcional)
    [SerializeField] private TextMeshProUGUI playerBorderNameText; // NUEVO: El texto que va DENTRO del borde

    private int currentSelection = 0;

    private bool playerConfirmed = false;
    private bool inputEnabled = true;

    private int playerSelection = -1;
    private int cpuSelection = -1;
    private int cpuCurrentIndex = 0;


    private void Start()
    {
        // Obtener el nombre a mostrar (o usar "Jugador 1" por defecto)
        string nombreFinal = string.IsNullOrEmpty(GameData.PlayerName) ? "Jugador 1" : GameData.PlayerName;

        // Asignar el nombre al texto de afuera (si está asignado)
        if (playerNameText != null)
        {
            playerNameText.text = nombreFinal;
        }

        // Asignar el nombre al texto que está DENTRO del borde
        if (playerBorderNameText != null)
        {
            playerBorderNameText.text = nombreFinal;
        }

        cpuBorder.gameObject.SetActive(false);

        for (int i = 0; i < lockOverlays.Length; i++)
        {
            lockOverlays[i].SetActive(false);
        }

        UpdatePlayerBorder();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitToMainMenu();
            return;
        }

        if (!inputEnabled)
            return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmSelection();
            return; 
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            RandomSelection();
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow)) MoveRight();
        if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveLeft();
        if (Input.GetKeyDown(KeyCode.UpArrow)) MoveUp();
        if (Input.GetKeyDown(KeyCode.DownArrow)) MoveDown();
    }

    private void MoveRight()
    {
        if (currentSelection % 4 < 3)
        {
            currentSelection++;
            UpdatePlayerBorder();
        }
    }

    private void MoveLeft()
    {
        if (currentSelection % 4 > 0)
        {
            currentSelection--;
            UpdatePlayerBorder();
        }
    }

    private void MoveUp()
    {
        if (currentSelection >= 4)
        {
            currentSelection -= 4;
            UpdatePlayerBorder();
        }
    }

    private void MoveDown()
    {
        if (currentSelection < 4)
        {
            currentSelection += 4;
            UpdatePlayerBorder();
        }
    }

    private void UpdatePlayerBorder()
    {
        // Mueve el borde de selección
        playerBorder.position = colorPoints[currentSelection].position;

        // Cambia el sprite del carro
        if (imagenCarroUI != null && spritesCarro != null && spritesCarro.Length > currentSelection)
        {
            imagenCarroUI.sprite = spritesCarro[currentSelection];
        }
    }

    public void ConfirmSelection()
    {
        if (playerConfirmed)
            return;

        playerConfirmed = true;
        inputEnabled = false;

        playerSelection = currentSelection;

        lockOverlays[playerSelection].SetActive(true);

        cpuBorder.gameObject.SetActive(true);

        StartCoroutine(CPUSelectionRoutine());
    }

    public void RandomSelection()
    {
        if (playerConfirmed || !inputEnabled)
            return;

        inputEnabled = false;
        StartCoroutine(RandomSelectionRoutine());
    }

    private IEnumerator RandomSelectionRoutine()
    {
        float timer = 0f;
        float duration = 1.0f; 
        float speed = 0.05f;   

        int finalSelection = Random.Range(0, colorPoints.Length);

        while (timer < duration)
        {
            currentSelection++;
            if (currentSelection >= colorPoints.Length)
                currentSelection = 0;

            UpdatePlayerBorder(); 

            timer += speed;
            yield return new WaitForSeconds(speed);
        }

        currentSelection = finalSelection;
        UpdatePlayerBorder();

        yield return new WaitForSeconds(0.2f); 

        inputEnabled = true; 
        ConfirmSelection();
    }

    private IEnumerator CPUSelectionRoutine()
    {
        do
        {
            cpuSelection = Random.Range(0, colorPoints.Length);

        } while (cpuSelection == playerSelection);

        cpuCurrentIndex = 0;

        float timer = 0f;

        while (timer < cpuThinkingTime)
        {
            cpuBorder.position = colorPoints[cpuCurrentIndex].position;

            cpuCurrentIndex++;

            if (cpuCurrentIndex >= colorPoints.Length)
                cpuCurrentIndex = 0;

            timer += cpuMoveSpeed;

            yield return new WaitForSeconds(cpuMoveSpeed);
        }

        cpuBorder.position = colorPoints[cpuSelection].position;

        lockOverlays[cpuSelection].SetActive(true);

        GameData.PlayerSelection = playerSelection;
        GameData.CpuSelection = cpuSelection;

        Debug.Log("Llegué al final de la Coroutine");

        yield return new WaitForSeconds(0.8f);

        Debug.Log("Voy a cargar Gameplay_Tutorial");

        SceneManager.LoadScene("Gameplay_Tutorial");
    }

    public void ExitToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void SeleccionarPorMouse(int index)
    {
        if (playerConfirmed || !inputEnabled)
            return;

        if (index >= 0 && index < colorPoints.Length)
        {
            currentSelection = index;
            UpdatePlayerBorder();
            Debug.Log("Selección por mouse: " + index);
        }
    }
}