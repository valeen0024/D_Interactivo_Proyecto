using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Traffic Light")]
    public TrafficLightManager trafficLightManager;

    [Header("UI")]
    [SerializeField] private GameObject uiInstructions;
    [SerializeField] private GameObject uiPlay;
    [SerializeField] private GameObject uiWin;

    [Header("CPU Turn Banner (Configurable en Inspector)")]
    [Tooltip("Activa o desactiva el aviso cuando pasa al turno de la CPU.")]
    [SerializeField] private bool enableCPUTurnBanner = true;

    [Tooltip("Texto TMP que anuncia el turno de la CPU.")]
    [SerializeField] private TMP_Text cpuTurnBannerText;

    [Tooltip("Contenedor o panel opcional del aviso de turno de la CPU.")]
    [SerializeField] private GameObject cpuTurnBannerContainer;

    [SerializeField] private string cpuTurnMessage = "Turno de la CPU";

    [Tooltip("Escribe el mensaje letra por letra si está activado.")]
    [SerializeField] private bool useTypewriterForCPUTurn = true;

    [Range(0.005f, 0.2f)]
    [SerializeField] private float cpuTurnTypewriterDelay = 0.035f;

    [Range(0.3f, 8f)]
    [SerializeField] private float cpuTurnBannerDuration = 2.5f;

    public enum GameState
    {
        Waiting,
        PlayerTurn,
        CPUTurn,
        Finished
    }

    private List<GameObject> fuelBarrels = new List<GameObject>();
    private Coroutine cpuBannerCoroutine;

    public GameState gameState;
    public bool isGameOver;
    public bool playerWon;
    public string winnerName;

    [HideInInspector]
    public float finalPlayerDistance;

    [HideInInspector]
    public float finalCPUDistance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetCPUTurnBannerVisible(false);
        FindFuelBarrels();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.TryGetComponent<KartLaneController>(out var laneCtrl))
        {
            laneCtrl.canMove = false;
        }

        GameObject cpu = GameObject.FindGameObjectWithTag("CPU");
        if (cpu != null && cpu.TryGetComponent<CPUController>(out var cpuCtrl))
        {
            cpuCtrl.canMove = false;
        }

        if (uiInstructions != null)
        {
            uiInstructions.SetActive(false);
        }

        if (uiPlay != null)
        {
            uiPlay.SetActive(true);
        }

        if (uiWin != null)
        {
            uiWin.SetActive(false);
        }

        StartGame();
    }

    private bool continueInstructions = false;
    private int currentInstruction = 0;

    [SerializeField] private TMPro.TextMeshProUGUI instructionText;
    [SerializeField] private TMPro.TextMeshProUGUI continueButtonText;
    [SerializeField] private TMPro.TextMeshProUGUI progressText;
    [SerializeField] private UnityEngine.UI.Button previousButton;

    private string[] tutorialMessages =
    {
        "Usa las flechas IZQUIERDA y DERECHA para cambiar de carril.",
        "Recoge los BIDONES para recuperar COMBUSTIBLE y seguir avanzando.",
        "Evita las MANCHAS DE ACEITE porque reducen la velocidad del vehículo.",
        "Presiona la flecha ARRIBA para activar el propulsor.",
        "¡Todo listo! Llega a la meta y consigue la mayor distancia posible."
    };

    private IEnumerator ShowInstructionsRoutine()
    {
        gameState = GameState.Waiting;

        if (uiPlay != null)
            uiPlay.SetActive(false);

        if (uiWin != null)
            uiWin.SetActive(false);

        if (uiInstructions != null)
            uiInstructions.SetActive(true);

        currentInstruction = 0;
        instructionText.text = tutorialMessages[currentInstruction];

        if (previousButton != null)
            previousButton.interactable = false;

        if (progressText != null)
            progressText.text = "1 de " + tutorialMessages.Length;

        if (continueButtonText != null)
            continueButtonText.text = "CONTINUAR";

        continueInstructions = false;
        yield return new WaitUntil(() => continueInstructions);

        if (uiInstructions != null)
            uiInstructions.SetActive(false);

        if (uiPlay != null)
            uiPlay.SetActive(true);

        StartGame();
    }

    public void ContinueInstructions()
    {
        if (gameState != GameState.Waiting)
            return;

        currentInstruction++;

        if (previousButton != null)
            previousButton.interactable = currentInstruction > 0;

        if (currentInstruction < tutorialMessages.Length)
        {
            instructionText.text = tutorialMessages[currentInstruction];

            if (progressText != null)
            {
                progressText.text =
                    (currentInstruction + 1) + " de " + tutorialMessages.Length;
            }

            if (continueButtonText != null)
            {
                continueButtonText.text =
                    currentInstruction == tutorialMessages.Length - 1
                    ? "COMENZAR"
                    : "CONTINUAR";
            }
        }
        else
        {
            continueInstructions = true;
        }
    }

    public void PreviousInstruction()
    {
        if (gameState != GameState.Waiting)
            return;

        if (currentInstruction <= 0)
            return;

        currentInstruction--;

        if (previousButton != null)
            previousButton.interactable = currentInstruction > 0;

        instructionText.text = tutorialMessages[currentInstruction];

        if (progressText != null)
        {
            progressText.text =
                (currentInstruction + 1) + " de " + tutorialMessages.Length;
        }

        if (continueButtonText != null)
        {
            continueButtonText.text = "CONTINUAR";
        }
    }

    public void SkipTutorial()
    {
        if (gameState != GameState.Waiting)
            return;

        continueInstructions = true;
    }

    private void FindFuelBarrels()
    {
        GameObject[] foundBarrels = GameObject.FindGameObjectsWithTag("Fuel");
        foreach (GameObject barrel in foundBarrels)
        {
            fuelBarrels.Add(barrel);
        }
    }

    public void StartGame()
    {
        gameState = GameState.Waiting;
        isGameOver = false;
        playerWon = false;
        winnerName = "";
        finalPlayerDistance = 0f;
        finalCPUDistance = 0f;

        if (cpuBannerCoroutine != null)
        {
            StopCoroutine(cpuBannerCoroutine);
            cpuBannerCoroutine = null;
        }
        SetCPUTurnBannerVisible(false);

        if (trafficLightManager != null)
        {
            trafficLightManager.StartPlayerTurnSequence();
        }
    }

    public void RespawnFuelBarrels()
    {
        foreach (GameObject barrel in fuelBarrels)
        {
            if (barrel != null)
            {
                barrel.SetActive(true);
            }
        }
    }

    public void EndPlayerTurn()
    {
        if (gameState != GameState.PlayerTurn) return;

        StartCoroutine(TransitionToCPUTurn());
    }

    private IEnumerator TransitionToCPUTurn()
    {
        gameState = GameState.Waiting;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            if (player.TryGetComponent<KartLaneController>(out var laneCtrl))
            {
                laneCtrl.canMove = false;
                laneCtrl.StopEngineSound();
            }

            if (player.TryGetComponent<KartMovement>(out var kartMov))
            {
                kartMov.enabled = false;
            }

            if (player.TryGetComponent<Rigidbody>(out var rb))
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                rb.isKinematic = true;
            }
        }

        Debug.Log("¡Turn over! The car has stopped. Waiting 3 seconds...");

        yield return new WaitForSeconds(3f);

        if (player != null)
        {
            player.SetActive(false);
            Debug.Log("Hidden player's kart.");
        }

        CameraFollow cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        GameObject cpu = GameObject.FindGameObjectWithTag("CPU");

        if (cam != null && cpu != null)
        {
            cam.SetTarget(cpu.transform);
            Debug.Log("Camera switching to the CPU kart.");
        }

        ShowCPUTurnBanner();

        RespawnFuelBarrels();

        if (trafficLightManager != null)
        {
            trafficLightManager.StartCPUTurnSequence();
        }
    }

    private void ShowCPUTurnBanner()
    {
        if (!enableCPUTurnBanner || cpuTurnBannerText == null) return;

        if (cpuBannerCoroutine != null)
        {
            StopCoroutine(cpuBannerCoroutine);
        }

        cpuBannerCoroutine = StartCoroutine(CPUTurnBannerRoutine());
    }

    private IEnumerator CPUTurnBannerRoutine()
    {
        SetCPUTurnBannerVisible(true);

        if (useTypewriterForCPUTurn)
        {
            cpuTurnBannerText.text = string.Empty;
            float delay = Mathf.Max(0.005f, cpuTurnTypewriterDelay);

            for (int i = 0; i < cpuTurnMessage.Length; i++)
            {
                cpuTurnBannerText.text += cpuTurnMessage[i];
                yield return new WaitForSeconds(delay);
            }
        }
        else
        {
            cpuTurnBannerText.text = cpuTurnMessage;
        }

        yield return new WaitForSeconds(Mathf.Max(0.3f, cpuTurnBannerDuration));

        SetCPUTurnBannerVisible(false);
        cpuBannerCoroutine = null;
    }

    private void SetCPUTurnBannerVisible(bool visible)
    {
        if (cpuTurnBannerContainer != null)
        {
            cpuTurnBannerContainer.SetActive(visible);
        }
        else if (cpuTurnBannerText != null)
        {
            cpuTurnBannerText.gameObject.SetActive(visible);
        }
    }

    public void EndCPUTurn()
    {
        if (gameState != GameState.CPUTurn) return;

        GameObject cpu = GameObject.FindGameObjectWithTag("CPU");
        if (cpu != null && cpu.TryGetComponent<CPUController>(out var cpuCtrl))
        {
            cpuCtrl.StopEngineSound();
        }

        Debug.Log("CPU turn finished.");
        FinishGame();
    }

    public void PlayerOutOfFuel()
    {
        EndPlayerTurn();
    }

    public void CPUOutOfFuel()
    {
        EndCPUTurn();
    }

    public void FinishGame()
    {
        if (isGameOver) return;
        isGameOver = true;
        gameState = GameState.Finished;
        CheckWinner();

        Debug.Log(
            "Game finished. Winner: " +
            winnerName
        );
    }

    public void CheckWinner()
    {
        Debug.Log(
            "Comparing distances - Player: " +
            finalPlayerDistance +
            " | CPU: " +
            finalCPUDistance
        );

        if (finalPlayerDistance > finalCPUDistance)
        {
            winnerName = "Player";
            playerWon = true;
        }
        else if (finalCPUDistance > finalPlayerDistance)
        {
            winnerName = "CPU";
            playerWon = false;
        }
        else
        {
            winnerName = "Draw";
            playerWon = false;
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}