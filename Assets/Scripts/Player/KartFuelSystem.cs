using UnityEngine;
using UnityEngine.UI;

public class KartFuelSystem : MonoBehaviour
{
    [Header("Fuel Configuration")]
    public float currentFuel = 62f;
    public float maxFuel = 100f;
    [Tooltip("Initial fuel ratio (0.62 = 62%) when starting/resetting so players don't start overly full.")]
    [Range(0.2f, 1f)]
    public float startingFuelRatio = 0.62f;
    [Tooltip("Base fuel drained per second during active turn.")]
    public float fuelConsumptionRate = 9f;

    [Header("Rewards and Penalties")]
    public float fuelFromBarrel = 18f;
    public float fuelLostFromOil = 15f;

    [Header("Full Tank Threshold")]
    [Tooltip("Percentage of maxFuel (0.80 = 80%) considered full enough to unlock boost opportunity.")]
    [Range(0.5f, 1f)]
    public float fullFuelRatioThreshold = 0.80f;

    public bool HasCollectedFuel { get; private set; } = false;
    private bool isOutOfFuel = false;
    public bool IsFuelAtMax => currentFuel >= (maxFuel * fullFuelRatioThreshold);
    private float consumptionMultiplier = 1f;

    private Rigidbody rb;
    private KartLaneController kartController;
    private CPUController cpuController;

    [Header("UI")]
    [SerializeField] private Slider fuelBar;

    public bool IsOutOfFuel => isOutOfFuel;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        kartController = GetComponent<KartLaneController>();
        cpuController = GetComponent<CPUController>();

        currentFuel = Mathf.Clamp(maxFuel * startingFuelRatio, 0f, maxFuel);

        if (fuelBar != null)
        {
            fuelBar.minValue = 0f;
            fuelBar.maxValue = 1f;
            fuelBar.value = GetFuelNormalized();
        }
    }

    void Update()
    {
        if (isOutOfFuel)
            return;

        bool isMyTurn =
            (
                gameObject.CompareTag("Player") &&
                GameManager.Instance.gameState ==
                GameManager.GameState.PlayerTurn
            )
            ||
            (
                gameObject.CompareTag("CPU") &&
                GameManager.Instance.gameState ==
                GameManager.GameState.CPUTurn
            );

        if (!isMyTurn)
            return;

        if (currentFuel > 0f)
        {
            float activeMultiplier = Mathf.Max(0f, consumptionMultiplier);
            currentFuel -= fuelConsumptionRate * activeMultiplier * Time.deltaTime;
            currentFuel = Mathf.Clamp(currentFuel, 0f, maxFuel);

            if (fuelBar != null)
            {
                fuelBar.value = GetFuelNormalized();
            }

            if (currentFuel <= 0f)
            {
                SetFuelConsumptionMultiplier(1f);
                OnFuelEmpty();
            }
        }
    }

    public void ResetFuelState()
    {
        HasCollectedFuel = false;
        isOutOfFuel = false;
        consumptionMultiplier = 1f;
        currentFuel = Mathf.Clamp(maxFuel * startingFuelRatio, 0f, maxFuel);
        if (fuelBar != null)
        {
            fuelBar.value = GetFuelNormalized();
        }
    }

    public void SetFuelConsumptionMultiplier(float multiplier)
    {
        consumptionMultiplier = Mathf.Max(0f, multiplier);
    }

    public float GetFuelNormalized()
    {
        return currentFuel / maxFuel;
    }

    public void AddFuel(float amount = -1f)
    {
        float amountToAdd = (amount < 0f) ? fuelFromBarrel : amount;

        currentFuel = Mathf.Clamp(currentFuel + amountToAdd, 0f, maxFuel);
        HasCollectedFuel = true;

        if (fuelBar != null)
        {
            fuelBar.value = GetFuelNormalized();
        }

        if (kartController != null)
        {
            kartController.RegisterFuelCollected();
        }

        if (currentFuel > 0f && isOutOfFuel)
        {
            isOutOfFuel = false;

            if (kartController != null)
            {
                kartController.canMove = true;
            }

            Debug.Log("Fuel restored! Kart reactivated.");
        }
        else
        {
            Debug.Log($"Fuel refilled (+{amountToAdd})! Total: {currentFuel:F1}");
        }
    }

    public void ReduceFuelFromOil()
    {
        currentFuel = Mathf.Clamp(currentFuel - fuelLostFromOil, 0f, maxFuel);

        if (fuelBar != null)
        {
            fuelBar.value = GetFuelNormalized();
        }

        Debug.Log($"Fuel lost because of oil! Total: {currentFuel:F1}");

        if (currentFuel <= 0f && !isOutOfFuel)
        {
            SetFuelConsumptionMultiplier(1f);
            OnFuelEmpty();
        }
    }

    private void OnFuelEmpty()
    {
        isOutOfFuel = true;
        SetFuelConsumptionMultiplier(1f);

        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (kartController != null)
        {
            kartController.canMove = false;
        }

        if (cpuController != null)
        {
            cpuController.canMove = false;
        }

        Debug.Log("We've run out of gas! The go-kart comes to a stop.");

        if (GameManager.Instance != null)
        {
            if (CompareTag("Player"))
            {
                GameManager.Instance.PlayerOutOfFuel();

                if (kartController != null)
                {
                    kartController.StopEngineSound();
                }
            }
            else if (CompareTag("CPU"))
            {
                GameManager.Instance.CPUOutOfFuel();

                if (cpuController != null)
                {
                    cpuController.StopEngineSound();
                }
            }
        }
    }
}