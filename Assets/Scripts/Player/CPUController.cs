using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class CPUController : MonoBehaviour
{
    [Header("Lane Configuration")]
    [Tooltip("Horizontal distance between each lane center.")]
    [SerializeField] private float laneOffset = 2f;

    [Tooltip("Speed at which the kart transitions horizontally between lanes.")]
    [SerializeField] private float laneChangeSpeed = 15f;

    [Tooltip("Forward speed of the CPU kart.")]
    [SerializeField] private float cpuSpeed = 6f;

    [Header("Progressive Speed (Level 2+)")]
    [Tooltip("Activa el aumento gradual de velocidad de la CPU según el avance.")]
    [SerializeField] private bool enableProgressiveSpeed = false;

    [Tooltip("Velocidad máxima que alcanzará la CPU al final de la pista.")]
    [SerializeField] private float maxProgressiveCpuSpeed = 12f;

    [Header("Lane Limits (4 Lanes Setup)")]
    [Tooltip("Minimum lane index (leftmost lane).")]
    [SerializeField] private int minLane = -2;

    [Tooltip("Maximum lane index (rightmost lane).")]
    [SerializeField] private int maxLane = 1;

    [Header("AI Control")]
    [Tooltip("Allows the CPU kart to move.")]
    public bool canMove = false;

    private bool cpuTurnStarted = false;


    [Tooltip("How often the CPU chooses a new lane.")]
    [SerializeField] private float laneChangeInterval = 2f;

    [Tooltip("How far ahead the CPU scans for objects.")]
    [SerializeField] private float scanDistance = 20f;

    [Header("Lane Weights")]
    [Tooltip("Weight for an empty lane.")]
    [SerializeField] private float emptyLaneWeight = 10f;

    [Tooltip("Weight for a lane with a fuel can.")]
    [SerializeField] private float fuelCanWeight = 50f;

    [Tooltip("Weight for a lane with oil.")]
    [SerializeField] private float oilSpillWeight = 2f;

    [Tooltip("Weight for a lane with a solid/hazard obstacle.")]
    [SerializeField] private float obstacleLaneWeight = 1f;

    [Header("Oil Effect")]
    [SerializeField] private float oilSlowMultiplier = 0.5f;
    [SerializeField] private float oilSlowDuration = 2f;

    [Header("Obstacle Effect (New Hazards / Barriers)")]
    [Tooltip("Tag used by obstacle objects in the scene.")]
    [SerializeField] private string obstacleTag = "Obstacle";

    [Tooltip("Speed multiplier when hitting an obstacle.")]
    [Range(0f, 1f)]
    [SerializeField] private float obstacleSlowMultiplier = 0.4f;

    [Tooltip("Duration in seconds of the obstacle slowdown.")]
    [Min(0.1f)]
    [SerializeField] private float obstacleSlowDuration = 1.2f;

    [Tooltip("Optional fuel penalty when hitting an obstacle (0 if only slows).")]
    [Min(0f)]
    [SerializeField] private float obstacleFuelPenalty = 0f;

    [Header("Start Lane")]
    [Tooltip("Initial lane of the CPU kart. Example: -1, 0, 1, 2.")]
    [SerializeField] private int startLane = 1;

    [Header("Turbo / Boost Settings")]
    [Tooltip("Speed multiplier during the boost.")]
    [SerializeField] private float boostSpeedMultiplier = 2f;

    [Tooltip("Duration of the boost in seconds.")]
    [SerializeField] private float boostDuration = 2f;

    [Tooltip("Maximum number of times the CPU can use boost in this level.")]
    [Min(0)]
    [SerializeField] private int maxBoostUses = 3;

    [Tooltip("Chance per second (0.0 to 1.0) for the CPU to activate boost when eligible.")]
    [SerializeField] private float boostChancePerSecond = 0.25f;

    [Header("Audio Settings")]
    [Tooltip("Grupo SFX de tu AudioMixer para que el slider de efectos controle motor y turbo.")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [SerializeField] private AudioClip engineClip;
    [SerializeField] private AudioClip turboClip;

    [SerializeField] private float normalEngineVolume = 0.5f;
    [SerializeField] private float normalEnginePitch = 1.0f;

    [SerializeField] private float turboEngineVolume = 1.0f;
    [SerializeField] private float turboEnginePitch = 1.5f;

    [Tooltip("Duración del Fade In/Out general del motor")]
    [SerializeField] private float engineFadeDuration = 1.0f;

    [Tooltip("Duración para que el motor vuelva a la normalidad tras el turbo")]
    [SerializeField] private float turboCooldownDuration = 0.5f;

    private bool isFadingOut = false;
    private AudioSource engineAudioSource;
    private AudioSource turboAudioSource;

    private int usesRemaining = 0;
    private bool isBoosting = false;
    private float nextBoostAllowedTime = 0f;

    private float boostWaitMin = 2.5f;
    private float boostWaitMax = 4.5f;
    private float boostWaitEndMin = 6f;
    private float boostWaitEndMax = 9.5f;

    private Coroutine activeBoostCoroutine;
    private Animator animator;

    private int currentLane;
    private int targetLane;
    private float laneChangeTimer;
    private float startXPosition;

    private float currentForwardSpeed;
    private KartFuelSystem fuelSystem;
    private DistanceTracker distanceTracker;
    private bool isSliding = false;
    private bool isObstacleSlowed = false;

    [Header("CPU Performance Statistics")]
    [SerializeField] private int fuelBarrelsCollected = 0;
    [SerializeField] private int oilSpillsHit = 0;
    [SerializeField] private int obstaclesHit = 0;
    [SerializeField] private int turbosUsed = 0;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        fuelSystem = GetComponent<KartFuelSystem>();
        distanceTracker = GetComponent<DistanceTracker>();
        animator = GetComponentInChildren<Animator>();

        startXPosition = transform.position.x - (Mathf.Clamp(startLane, minLane, maxLane) * laneOffset);

        currentLane = Mathf.Clamp(startLane, minLane, maxLane);
        targetLane = currentLane;

        currentForwardSpeed = cpuSpeed;
        usesRemaining = Mathf.Max(0, maxBoostUses);
        laneChangeTimer = laneChangeInterval;

        nextBoostAllowedTime = Time.time + CalculateBoostWait();

        if (engineClip != null)
        {
            engineAudioSource = gameObject.AddComponent<AudioSource>();
            engineAudioSource.clip = engineClip;
            engineAudioSource.loop = true;
            engineAudioSource.volume = normalEngineVolume;
            engineAudioSource.pitch = normalEnginePitch;
            engineAudioSource.playOnAwake = false;
            engineAudioSource.outputAudioMixerGroup = sfxMixerGroup;
        }

        if (turboClip != null)
        {
            turboAudioSource = gameObject.AddComponent<AudioSource>();
            turboAudioSource.clip = turboClip;
            turboAudioSource.loop = false;
            turboAudioSource.playOnAwake = false;
            turboAudioSource.outputAudioMixerGroup = sfxMixerGroup;
        }
    }

    private float GetCurrentBaseCpuSpeed()
    {
        if (!enableProgressiveSpeed || distanceTracker == null || distanceTracker.maxDistance <= 0f)
        {
            return cpuSpeed;
        }

        float progress = Mathf.Clamp01(distanceTracker.currentDistance / distanceTracker.maxDistance);
        return Mathf.Lerp(cpuSpeed, maxProgressiveCpuSpeed, progress);
    }

    public void ConfigureForLevel(float difficulty, int boostUsesForLevel)
    {
        maxBoostUses = Mathf.Max(0, boostUsesForLevel);
        usesRemaining = maxBoostUses;
        boostChancePerSecond = Mathf.Clamp(0.25f / Mathf.Max(0.5f, difficulty), 0.08f, 0.5f);
    }

    private void Update()
    {
        if (!canMove)
            return;

        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.gameState != GameManager.GameState.CPUTurn)
            return;

        if (!cpuTurnStarted)
        {
            cpuTurnStarted = true;
            nextBoostAllowedTime = Time.time + CalculateBoostWait();

            Debug.Log("[CPU TURBO] Turno iniciado. Espera inicial programada.");
        }

        if (fuelSystem != null && fuelSystem.IsOutOfFuel)
            return;

        if (!isBoosting && !isSliding && !isObstacleSlowed)
        {
            currentForwardSpeed = GetCurrentBaseCpuSpeed();
        }

        MoveCPU();


        if (!isBoosting &&
            usesRemaining > 0 &&
            Time.time >= nextBoostAllowedTime &&
            fuelBarrelsCollected > 0 &&
            fuelSystem != null &&
            fuelSystem.IsFuelAtMax)
        {
            if (Random.value < boostChancePerSecond * Time.deltaTime)
            {
                ActivateTurbo();
            }
        }

        laneChangeTimer -= Time.deltaTime;
        if (laneChangeTimer <= 0f)
        {
            ChooseLane();
            laneChangeTimer = laneChangeInterval;
        }
    }

    private void MoveCPU()
    {
        float targetX = GetLanePosition(targetLane);
        Vector3 currentPos = transform.position;

        float newX = Mathf.MoveTowards(
            currentPos.x,
            targetX,
            laneChangeSpeed * Time.deltaTime
        );

        float newZ = currentPos.z + (currentForwardSpeed * Time.deltaTime);

        float minX = GetLanePosition(minLane);
        float maxX = GetLanePosition(maxLane);
        newX = Mathf.Clamp(newX, minX, maxX);

        transform.position = new Vector3(newX, currentPos.y, newZ);

        if (Mathf.Abs(transform.position.x - targetX) < 0.05f)
        {
            currentLane = targetLane;
        }
    }

    private bool HasObstacleTag(Collider col)
    {
        return !string.IsNullOrEmpty(obstacleTag) && col.CompareTag(obstacleTag);
    }

    private void ChooseLane()
    {
        if (currentLane != targetLane)
            return;

        int bestLane = currentLane;
        float bestWeight = EvaluateLaneWeight(currentLane);

        bool currentLaneIsSafe = IsLaneSafe(currentLane);

        for (int lane = minLane; lane <= maxLane; lane++)
        {
            if (lane == currentLane)
                continue;

            float weight = EvaluateLaneWeight(lane);
            float minimumImprovement = 5f;

            if (IsLaneSafe(lane) &&
                ((!currentLaneIsSafe && bestLane == currentLane) ||
                 weight > bestWeight + minimumImprovement))
            {
                bestWeight = weight;
                bestLane = lane;
            }
        }


        if (bestLane != currentLane)
        {
            int direction = bestLane > currentLane ? 1 : -1;
            int nextLane = currentLane + direction;

            if (IsLaneSafe(nextLane))
            {
                targetLane = nextLane;

                Debug.Log(
                    "[CPU RUTA] Actual: " + currentLane +
                    " | Destino deseado: " + bestLane +
                    " | Siguiente carril: " + targetLane
                );

                return;
            }

            // Try the opposite direction when the planned route is blocked.
            int alternativeLane = currentLane - direction;

            if (alternativeLane >= minLane &&
                alternativeLane <= maxLane &&
                IsLaneSafe(alternativeLane))
            {
                targetLane = alternativeLane;

                Debug.Log(
                    "[CPU RUTA] Ruta alternativa segura: " + targetLane
                );
            }
            else
            {
                Debug.Log("[CPU RUTA] No hay una ruta alternativa segura.");
            }

            return;
        }


        if (Mathf.Approximately(bestWeight, emptyLaneWeight) && Random.value < 0.10f)
        {
            int[] directions = { -1, 1 };

            if (Random.value < 0.5f)
            {
                directions[0] = 1;
                directions[1] = -1;
            }

            foreach (int direction in directions)
            {
                int nextLane = currentLane + direction;

                if (nextLane < minLane || nextLane > maxLane)
                    continue;

                float nextWeight = EvaluateLaneWeight(nextLane);

                if (Mathf.Approximately(nextWeight, emptyLaneWeight) && IsLaneSafe(nextLane))
                {
                    targetLane = nextLane;
                    break;
                }
            }
        }

        Debug.Log(
            "[CPU CARRIL] Actual: " + currentLane +
            " | Objetivo: " + targetLane +
            " | X actual: " + transform.position.x.ToString("F2") +
            " | X objetivo: " + GetLanePosition(targetLane).ToString("F2")
        );
    }

    private bool IsLaneSafe(int lane)
    {
        float targetX = GetLanePosition(lane);

        Vector3 boxOrigin = new Vector3(
            targetX,
            transform.position.y + 0.5f,
            transform.position.z + 1.5f
        );

        Vector3 halfExtents = new Vector3(
            0.65f,
            0.35f,
            0.25f
        );

        RaycastHit[] hits = Physics.BoxCastAll(
            boxOrigin,
            halfExtents,
            Vector3.forward,
            Quaternion.identity,
            scanDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (RaycastHit hit in hits)
        {
            if ((hit.collider.CompareTag("Oil") || HasObstacleTag(hit.collider)) &&
                hit.distance <= 20f)
            {
                return false;
            }
        }

        return true;
    }

    private float EvaluateLaneWeight(int lane)
    {
        float targetX = GetLanePosition(lane);

        Vector3 boxOrigin = new Vector3(
            targetX,
            transform.position.y + 0.5f,
            transform.position.z + 1.5f
        );

        Vector3 halfExtents = new Vector3(
            0.65f,
            0.35f,
            0.25f
        );

        RaycastHit[] hits = Physics.BoxCastAll(
            boxOrigin,
            halfExtents,
            Vector3.forward,
            Quaternion.identity,
            scanDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        float nearestHazard = Mathf.Infinity;
        float nearestFuel = Mathf.Infinity;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("Oil") || HasObstacleTag(hit.collider))
            {
                nearestHazard = Mathf.Min(nearestHazard, hit.distance);
            }
            else if (hit.collider.CompareTag("Fuel"))
            {
                nearestFuel = Mathf.Min(nearestFuel, hit.distance);
            }
        }

        float dangerDistance = 20f;
        bool hazardIsClose = nearestHazard <= dangerDistance;

        if (hazardIsClose)
        {
            Debug.Log("[CPU IA] Peligro (aceite u obstáculo) cercano en carril " + lane);
            return Mathf.Min(oilSpillWeight, obstacleLaneWeight);
        }

        if (nearestFuel < Mathf.Infinity)
        {
            bool fuelIsSafe =
                nearestHazard == Mathf.Infinity ||
                nearestFuel + 5f < nearestHazard;

            if (fuelIsSafe)
            {
                float fuelBonus = Mathf.Clamp(
                    (scanDistance - nearestFuel) * 0.4f,
                    0f,
                    10f
                );

                Debug.Log("[CPU IA] Combustible seguro en carril " + lane);
                return fuelCanWeight + fuelBonus;
            }
        }

        if (nearestHazard < Mathf.Infinity && nearestFuel < Mathf.Infinity)
        {
            return Mathf.Min(oilSpillWeight, obstacleLaneWeight);
        }

        return emptyLaneWeight;
    }

    private float GetLanePosition(int lane)
    {
        return startXPosition + (lane * laneOffset);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(
            "The CPU came into contact with: "
            + other.gameObject.name
            + " | Tag: "
            + other.gameObject.tag
        );

        if (other.CompareTag("Fuel"))
        {
            fuelBarrelsCollected++;
            Debug.Log("[CPU ESTADISTICAS] Bidones recogidos: " + fuelBarrelsCollected);

            if (fuelSystem != null)
            {
                fuelSystem.AddFuel();
            }

            other.gameObject.SetActive(false);
            return;
        }

        if (other.CompareTag("Oil"))
        {
            oilSpillsHit++;
            Debug.Log("[CPU ESTADISTICAS] Manchas de aceite pisadas: " + oilSpillsHit);

            if (fuelSystem != null)
            {
                fuelSystem.ReduceFuelFromOil();
            }

            ApplyOilSlow(oilSlowDuration);
            return;
        }

        if (HasObstacleTag(other))
        {
            obstaclesHit++;
            Debug.Log("[CPU ESTADISTICAS] Obstáculos golpeados: " + obstaclesHit);

            if (obstacleFuelPenalty > 0f && fuelSystem != null)
            {
                fuelSystem.currentFuel = Mathf.Max(0f, fuelSystem.currentFuel - obstacleFuelPenalty);
            }

            ApplyObstacleSlow(obstacleSlowDuration, obstacleSlowMultiplier);
        }
    }

    private void ApplyOilSlow(float duration)
    {
        if (!isSliding)
        {
            StartCoroutine(OilSlowRoutine(duration));
        }
    }

    private IEnumerator OilSlowRoutine(float duration)
    {
        isSliding = true;
        currentForwardSpeed = GetCurrentBaseCpuSpeed() * oilSlowMultiplier;
        Debug.Log("CPU slipped on oil. Speed reduced.");

        yield return new WaitForSeconds(duration);

        if ((fuelSystem == null || !fuelSystem.IsOutOfFuel) && !isBoosting && !isObstacleSlowed)
        {
            currentForwardSpeed = GetCurrentBaseCpuSpeed();
        }

        isSliding = false;
        Debug.Log("CPU speed recovered.");
    }

    private void ApplyObstacleSlow(float duration, float multiplier)
    {
        StartCoroutine(ObstacleSlowRoutine(duration, multiplier));
    }

    private IEnumerator ObstacleSlowRoutine(float duration, float multiplier)
    {
        isObstacleSlowed = true;
        currentForwardSpeed = GetCurrentBaseCpuSpeed() * Mathf.Clamp01(multiplier);
        Debug.Log("CPU hit an obstacle! Speed reduced.");

        yield return new WaitForSeconds(duration);

        isObstacleSlowed = false;

        if ((fuelSystem == null || !fuelSystem.IsOutOfFuel) && !isBoosting && !isSliding)
        {
            currentForwardSpeed = GetCurrentBaseCpuSpeed();
        }

        Debug.Log("CPU recovered from obstacle impact.");
    }

    public void ResetForNewTurn()
    {
        StopAllCoroutines();

        canMove = false;
        transform.position = startPosition;
        transform.rotation = startRotation;

        currentLane = Mathf.Clamp(startLane, minLane, maxLane);
        targetLane = currentLane;

        currentForwardSpeed = cpuSpeed;
        laneChangeTimer = laneChangeInterval;

        isSliding = false;
        isObstacleSlowed = false;
        isBoosting = false;
        usesRemaining = Mathf.Max(0, maxBoostUses);
        nextBoostAllowedTime = Time.time + CalculateBoostWait();
        cpuTurnStarted = false;


        fuelBarrelsCollected = 0;
        oilSpillsHit = 0;
        obstaclesHit = 0;
        turbosUsed = 0;

        if (fuelSystem != null)
        {
            fuelSystem.ResetFuelState();
        }

        activeBoostCoroutine = null;

        if (engineAudioSource != null)
        {
            engineAudioSource.volume = normalEngineVolume;
            engineAudioSource.pitch = normalEnginePitch;
        }

    }

    private float CalculateBoostWait()
    {
        float progress = 0f;

        DistanceTracker tracker = GetComponent<DistanceTracker>();

        if (tracker != null && tracker.maxDistance > 0f)
        {
            progress = Mathf.Clamp01(
                tracker.currentDistance / tracker.maxDistance
            );
        }

        float minWait = Mathf.Lerp(boostWaitMin, boostWaitEndMin, progress);
        float maxWait = Mathf.Lerp(boostWaitMax, boostWaitEndMax, progress);

        return Random.Range(minWait, maxWait);
    }

    private void ActivateTurbo()
    {
        if (usesRemaining <= 0)
            return;

        usesRemaining--;
        turbosUsed++;

        nextBoostAllowedTime = Time.time + boostDuration + CalculateBoostWait();

        Debug.Log("[CPU ESTADISTICAS] Turbos utilizados: " + turbosUsed + " | Restantes: " + usesRemaining);

        if (animator != null)
        {
            animator.SetTrigger("Boost");
        }

        if (turboAudioSource != null)
        {
            turboAudioSource.outputAudioMixerGroup = sfxMixerGroup;
            turboAudioSource.Play();
        }

        if (activeBoostCoroutine != null)
        {
            StopCoroutine(activeBoostCoroutine);
        }

        activeBoostCoroutine = StartCoroutine(TurboRoutine());
    }

    private IEnumerator TurboRoutine()
    {
        isBoosting = true;
        currentForwardSpeed = GetCurrentBaseCpuSpeed() * boostSpeedMultiplier;

        if (fuelSystem != null)
        {
            fuelSystem.SetFuelConsumptionMultiplier(2.2f);
        }

        Debug.Log("CPU activated Boost!");

        if (engineAudioSource != null)
        {
            engineAudioSource.volume = turboEngineVolume;
            engineAudioSource.pitch = turboEnginePitch;
        }

        yield return new WaitForSeconds(boostDuration);

        if (fuelSystem != null)
        {
            fuelSystem.SetFuelConsumptionMultiplier(1f);
        }

        if ((fuelSystem == null || !fuelSystem.IsOutOfFuel) && !isSliding && !isObstacleSlowed)
        {
            currentForwardSpeed = GetCurrentBaseCpuSpeed();
        }

        if (engineAudioSource != null)
        {
            float startVolume = engineAudioSource.volume;
            float startPitch = engineAudioSource.pitch;
            float currentTime = 0f;

            while (currentTime < turboCooldownDuration)
            {
                currentTime += Time.deltaTime;
                engineAudioSource.volume = Mathf.Lerp(startVolume, normalEngineVolume, currentTime / turboCooldownDuration);
                engineAudioSource.pitch = Mathf.Lerp(startPitch, normalEnginePitch, currentTime / turboCooldownDuration);
                yield return null;
            }

            engineAudioSource.volume = normalEngineVolume;
            engineAudioSource.pitch = normalEnginePitch;
        }

        isBoosting = false;
        activeBoostCoroutine = null;
        Debug.Log("CPU Boost finished. Normal speed restored.");
    }

    public void StartEngineSound()
    {
        if (engineAudioSource != null && !engineAudioSource.isPlaying)
        {
            engineAudioSource.outputAudioMixerGroup = sfxMixerGroup;
            StartCoroutine(FadeInEngine());
        }
    }

    private IEnumerator FadeInEngine()
    {
        engineAudioSource.volume = 0f;
        engineAudioSource.Play();
        float currentTime = 0f;

        while (currentTime < engineFadeDuration)
        {
            currentTime += Time.deltaTime;
            engineAudioSource.volume = Mathf.Lerp(0f, normalEngineVolume, currentTime / engineFadeDuration);
            yield return null;
        }
        engineAudioSource.volume = normalEngineVolume;
    }

    public void StopEngineSound()
    {
        if (engineAudioSource != null && engineAudioSource.isPlaying && !isFadingOut)
        {
            StartCoroutine(FadeOutEngine());
        }
    }

    private IEnumerator FadeOutEngine()
    {
        isFadingOut = true;
        float startVolume = engineAudioSource.volume;
        float currentTime = 0f;

        while (currentTime < engineFadeDuration)
        {
            currentTime += Time.deltaTime;
            engineAudioSource.volume = Mathf.Lerp(startVolume, 0f, currentTime / engineFadeDuration);
            yield return null;
        }

        engineAudioSource.volume = 0f;
        engineAudioSource.Stop();
        isFadingOut = false;
    }
}