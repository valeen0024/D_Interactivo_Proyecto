using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class TutorialManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private KartLaneController playerController;

    [Header("Tutorial UI")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Exercise Settings")]
    [SerializeField] private float requiredLaneDistance = 1.8f;
    [SerializeField] private float messageDelay = 1.5f;

    [Header("Fuel Exercise")]
    [SerializeField] private GameObject tutorialFuelBarrel;
    [SerializeField] private KartFuelSystem playerFuelSystem;
    [SerializeField] private float fuelSpawnDistance = 15f;
    [SerializeField] private int minRandomLane = -1;
    [SerializeField] private int maxRandomLane = 2;

    private Vector3 originalFuelPosition;
    private int selectedFuelLane;

    [Header("Tutorial Movement")]
    [SerializeField] private float tutorialForwardSpeed = 4f;

    [Header("Oil Exercise")]
    [SerializeField] private GameObject tutorialOilSpill;
    [SerializeField] private TutorialOilDetector oilDetector;
    [SerializeField] private float oilSpawnDistance = 18f;

    private int selectedOilLane;

    [Header("Completion UI")]
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private GameObject instructionsPanel;

    [Header("HUD Introduction")]
    [SerializeField] private GameObject hudTutorialPanel;

    private float initialX;
    private int currentExercise = 0;
    private bool correctKeyPressed = false;
    private bool transitioning = false;
    private bool fuelRetryInProgress = false;
    private float fuelBeforeCollection;

    private Vector3 fuelExerciseStartPosition;
    private Quaternion fuelExerciseStartRotation;
    private int fuelExerciseStartLane;
    private Vector3 oilExerciseStartPosition;
    private int oilExerciseStartLane;
    private Quaternion oilExerciseStartRotation;

    private bool turboExerciseCompleted = false;

    private void PositionFuelBarrelRandomly()
    {
        if (tutorialFuelBarrel == null || playerController == null)
            return;

        selectedFuelLane = Random.Range(minRandomLane, maxRandomLane + 1);

        float randomX = playerController.GetLaneWorldX(selectedFuelLane);

        Vector3 newPosition = tutorialFuelBarrel.transform.position;

        newPosition.x = randomX;
        newPosition.z = playerController.transform.position.z + fuelSpawnDistance;

        tutorialFuelBarrel.transform.position = newPosition;

        Debug.Log("TUTORIAL: Fuel barrel placed in random lane: " + selectedFuelLane);
    }

    private bool HasMissedFuelBarrel()
    {
        if (tutorialFuelBarrel == null ||
            playerController == null ||
            !tutorialFuelBarrel.activeInHierarchy)
        {
            return false;
        }

        float playerZ = playerController.transform.position.z;
        float barrelZ = tutorialFuelBarrel.transform.position.z;

        return playerZ > barrelZ + 3f;
    }

    private void PositionOilSpillRandomly()
    {
        if (tutorialOilSpill == null || playerController == null)
            return;

        selectedOilLane = Random.Range(minRandomLane, maxRandomLane + 1);

        float randomX = playerController.GetLaneWorldX(selectedOilLane);

        Vector3 newPosition = tutorialOilSpill.transform.position;

        newPosition.x = randomX;
        newPosition.z = playerController.transform.position.z + oilSpawnDistance;

        tutorialOilSpill.transform.position = newPosition;

        Debug.Log("TUTORIAL: Oil spill placed in random lane: " + selectedOilLane);
    }

    private IEnumerator RetryFuelExercise()
    {
        fuelRetryInProgress = true;
        transitioning = true;

        playerController.canMove = false;
        playerController.SetForwardSpeed(0f);

        UpdateUI(
            "¡No recogiste el combustible! Inténtalo de nuevo.",
            "PASO 3 DE 5"
        );

        yield return new WaitForSeconds(messageDelay);

        playerController.RestoreTutorialLane(fuelExerciseStartLane);
        playerController.transform.SetPositionAndRotation(
            fuelExerciseStartPosition,
            fuelExerciseStartRotation
        );

        if (tutorialFuelBarrel != null)
        {
            tutorialFuelBarrel.SetActive(false);
            PositionFuelBarrelRandomly();
            tutorialFuelBarrel.SetActive(true);
        }

        if (playerFuelSystem != null)
        {
            fuelBeforeCollection = playerFuelSystem.GetFuelNormalized();
        }

        initialX = playerController.transform.position.x;
        transitioning = false;
        fuelRetryInProgress = false;

        UpdateUI(
            "¡Recoge el bidón de combustible!",
            "PASO 3 DE 5"
        );

        playerController.SetForwardSpeed(tutorialForwardSpeed);
        playerController.canMove = true;
    }

    private void Start()
    {
        if (playerController == null)
        {
            Debug.LogError("Tutorial: Player controller is missing.");
            enabled = false;
            return;
        }

        playerController.canMove = false;
        playerController.canUseTurbo = false;
        playerController.SetForwardSpeed(0f);

        if (hudTutorialPanel != null)
        {
            hudTutorialPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Tutorial: HUD panel is not assigned.");
            BeginTutorialExercises();
        }
    }

    public void BeginTutorialExercises()
    {
        if (hudTutorialPanel != null)
        {
            hudTutorialPanel.SetActive(false);
        }

        playerController.canMove = true;
        playerController.canUseTurbo = false;
        playerController.SetForwardSpeed(0f);

        StartExercise(0);
    }

    private void StartExercise(int exercise)
    {
        currentExercise = exercise;
        initialX = playerController.transform.position.x;
        correctKeyPressed = false;
        transitioning = false;

        if (exercise == 0)
        {
            UpdateUI(
                "Presiona la flecha DERECHA →",
                "PASO 1 DE 5"
            );
        }
        else if (exercise == 1)
        {
            UpdateUI(
                "Presiona la flecha IZQUIERDA ←",
                "PASO 2 DE 5"
            );
        }
        else if (exercise == 2)
        {
            fuelExerciseStartPosition = playerController.transform.position;
            fuelExerciseStartRotation = playerController.transform.rotation;
            fuelExerciseStartLane = playerController.CurrentLane;

            UpdateUI(
                "¡Recoge el bidón de combustible!",
                "PASO 3 DE 5"
            );

            if (playerFuelSystem != null)
            {
                fuelBeforeCollection = playerFuelSystem.GetFuelNormalized();
            }

            if (tutorialFuelBarrel != null)
            {
                PositionFuelBarrelRandomly();
                tutorialFuelBarrel.SetActive(true);
            }

            playerController.canMove = true;
            playerController.SetForwardSpeed(tutorialForwardSpeed);
        }
        else if (exercise == 3)
        {
            oilExerciseStartPosition = playerController.transform.position;
            oilExerciseStartRotation = playerController.transform.rotation;
            oilExerciseStartLane = playerController.CurrentLane;

            UpdateUI(
                "¡Esquiva la mancha de aceite cambiando de carril!",
                "PASO 4 DE 5"
            );

            if (oilDetector != null)
            {
                oilDetector.ResetDetection();
            }

            if (tutorialOilSpill != null)
            {
                PositionOilSpillRandomly();
                tutorialOilSpill.SetActive(true);
            }

            playerController.canMove = true;
            playerController.SetForwardSpeed(tutorialForwardSpeed);
        }
        else if (exercise == 4)
        {
            UpdateUI(
                "¡Presiona la flecha ARRIBA para activar el PROPULSOR!",
                "PASO 5 DE 5"
            );

            turboExerciseCompleted = false;

            playerController.canUseTurbo = true;
            playerController.canMove = true;
            playerController.SetForwardSpeed(tutorialForwardSpeed);
        }
    }

    private void Update()
    {
        if (transitioning || currentExercise >= 5)
            return;

        if (currentExercise == 0)
        {
            if (Keyboard.current != null && Keyboard.current.rightArrowKey.wasPressedThisFrame)
                correctKeyPressed = true;

            float distance =
                playerController.transform.position.x - initialX;

            if (correctKeyPressed &&
                distance >= requiredLaneDistance)
            {
                StartCoroutine(CompleteExercise());
            }
        }
        else if (currentExercise == 1)
        {
            if (Keyboard.current != null && Keyboard.current.leftArrowKey.wasPressedThisFrame)
                correctKeyPressed = true;

            float distance =
                initialX - playerController.transform.position.x;

            if (correctKeyPressed &&
                distance >= requiredLaneDistance)
            {
                StartCoroutine(CompleteExercise());
            }
        }
        else if (currentExercise == 2)
        {
            if (playerFuelSystem != null)
            {
                float currentFuel =
                    playerFuelSystem.GetFuelNormalized();

                if (currentFuel > fuelBeforeCollection + 0.001f)
                {
                    StartCoroutine(CompleteFuelExercise());
                }
            }

            if (!transitioning &&
                !fuelRetryInProgress &&
                HasMissedFuelBarrel())
            {
                StartCoroutine(RetryFuelExercise());
            }
        }
        else if (currentExercise == 3)
        {
            if (tutorialOilSpill == null || oilDetector == null)
                return;

            if (oilDetector.PlayerTouchedOil)
            {
                StartCoroutine(CompleteOilExercise(false));
                return;
            }

            float playerZ = playerController.transform.position.z;
            float oilZ = tutorialOilSpill.transform.position.z;

            if (playerZ >= oilZ + 3f)
            {
                StartCoroutine(CompleteOilExercise(true));
            }
        }
        else if (currentExercise == 4)
        {
            if (!turboExerciseCompleted && playerController.HasUsedTurbo)
            {
                turboExerciseCompleted = true;

                Debug.Log("TUTORIAL: Turbo activated successfully!");

                StartCoroutine(CompleteTurboExercise());
            }
        }
    }

    private IEnumerator CompleteExercise()
    {
        transitioning = true;

        if (instructionText != null)
        {
            instructionText.text =
                "¡Excelente! Cambio de carril completado.";
        }

        Debug.Log("Tutorial exercise completed!");

        yield return new WaitForSeconds(messageDelay);

        if (currentExercise == 0)
        {
            StartExercise(1);
        }
        else if (currentExercise == 1)
        {
            StartExercise(2);
        }
    }

    private IEnumerator CompleteFuelExercise()
    {
        transitioning = true;

        playerController.SetForwardSpeed(0f);

        UpdateUI(
            "¡Excelente! Recuperaste combustible.",
            "PASO 3 DE 5"
        );

        Debug.Log("TUTORIAL: Fuel exercise completed!");

        yield return new WaitForSeconds(messageDelay);

        playerController.canMove = false;
        StartExercise(3);
    }

    private IEnumerator CompleteOilExercise(bool avoidedOil)
    {
        transitioning = true;

        playerController.SetForwardSpeed(0f);
        playerController.canMove = false;

        if (avoidedOil)
        {
            UpdateUI(
                "¡Excelente! Esquivaste la mancha de aceite.",
                "PASO 4 DE 5"
            );

            Debug.Log("TUTORIAL: Oil avoided successfully!");
        }
        else
        {
            UpdateUI(
                "¡Cuidado! Tocaste el aceite. Inténtalo de nuevo.",
                "PASO 4 DE 5"
            );

            Debug.Log("TUTORIAL: Player touched oil. Retry required.");
        }

        yield return new WaitForSeconds(messageDelay);

        if (avoidedOil)
        {
            if (tutorialOilSpill != null)
            {
                tutorialOilSpill.SetActive(false);
            }

            Debug.Log("TUTORIAL: Starting turbo exercise.");

            StartExercise(4);
        }
        else
        {
            Debug.Log("TUTORIAL: Restarting oil exercise.");

            if (tutorialOilSpill != null)
            {
                tutorialOilSpill.SetActive(false);
            }

            playerController.RestoreTutorialLane(oilExerciseStartLane);
            playerController.transform.SetPositionAndRotation(
                oilExerciseStartPosition,
                oilExerciseStartRotation
            );

            if (oilDetector != null)
            {
                oilDetector.ResetDetection();
            }

            StartExercise(3);
        }
    }

    private IEnumerator CompleteTurboExercise()
    {
        transitioning = true;

        UpdateUI(
            "¡Excelente! Activaste correctamente el propulsor.",
            "PASO 5 DE 5"
        );

        Debug.Log("TUTORIAL: Turbo exercise completed!");

        yield return new WaitForSeconds(messageDelay);

        playerController.SetForwardSpeed(0f);
        playerController.canMove = false;

        currentExercise = 5;

        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(false);
        }

        if (completionPanel != null)
        {
            completionPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("TUTORIAL: Completion Panel is not assigned!");
        }

        Debug.Log("TUTORIAL: Training completed.");
    }

    private void UpdateUI(string message, string progress)
    {
        if (instructionText != null)
            instructionText.text = message;

        if (progressText != null)
            progressText.text = progress;
    }

    public void StartMainRace()
    {
        Debug.Log("TUTORIAL: Starting main race.");

        SceneManager.LoadScene("Gameplay");
    }
}