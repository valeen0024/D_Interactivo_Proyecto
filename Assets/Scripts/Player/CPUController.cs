using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class CPUController : MonoBehaviour
{
    [Header("Lane Configuration")]
    [Tooltip("Horizontal distance between each lane center.")]
    [SerializeField] private float laneOffset = 2f;

    [Tooltip("Velocidad lateral al cambiar de carril (fluida y visible en cámara).")]
    [Range(4f, 16f)]
    [SerializeField] private float laneChangeSpeed = 9.5f;

    [Tooltip("Máximo de carriles que puede avanzar por decisión.")]
    [Range(1, 2)]
    [SerializeField] private int maxLaneStepPerDecision = 1;

    [Tooltip("Tiempo mínimo entre cambios de carril para evitar movimientos bruscos.")]
    [Min(0.1f)]
    [SerializeField] private float minTimeBetweenLaneChanges = 0.22f;

    [Tooltip("Base forward speed of the CPU kart.")]
    [SerializeField] private float cpuSpeed = 7.6f;

    [Tooltip("Variación aleatoria de ritmo por partida (ej. 0.12 = entre -12% y +12% de velocidad base).")]
    [Range(0f, 0.35f)]
    [SerializeField] private float speedVariancePerRun = 0.12f;

    [Header("Progressive Speed (Level 2+)")]
    [Tooltip("Activa el aumento gradual de velocidad de la CPU según el avance.")]
    [SerializeField] private bool enableProgressiveSpeed = false;

    [Tooltip("Velocidad máxima que alcanzará la CPU al final de la pista.")]
    [SerializeField] private float maxProgressiveCpuSpeed = 13.8f;

    [Header("Lane Limits (4 Lanes Setup)")]
    [Tooltip("Minimum lane index (leftmost lane).")]
    [SerializeField] private int minLane = -2;

    [Tooltip("Maximum lane index (rightmost lane).")]
    [SerializeField] private int maxLane = 1;

    [Header("AI Control & Camera Vision")]
    [Tooltip("Allows the CPU kart to move.")]
    public bool canMove = false;

    private bool cpuTurnStarted = false;

    [Tooltip("Qué tan seguido revisa lo que hay en pantalla frente a ella.")]
    [SerializeField] private float laneChangeInterval = 0.25f;

    [Tooltip("Distancia máxima hacia adelante dentro del campo de visión.")]
    [SerializeField] private float viewDistance = 24f;

    [Tooltip("Evalúa objetos que estén dentro del encuadre de la Main Camera.")]
    [SerializeField] private bool useMainCameraFOV = true;

    [Header("Fuel & Risk Taking")]
    [Tooltip("Umbral de combustible (0 a 1) a partir del cual prioriza aún más los bidones.")]
    [Range(0.2f, 0.9f)]
    [SerializeField] private float lowFuelThresholdRatio = 0.5f;

    [Tooltip("Si está activo, cuando necesita gasolina puede aceptar un pequeño roce/riesgo por alcanzar un bidón.")]
    [SerializeField] private bool riskForFuelWhenLow = true;

    [Header("Lane Weights")]
    [Tooltip("Weight for an empty lane.")]
    [SerializeField] private float emptyLaneWeight = 10f;

    [Tooltip("Weight base for a lane with a fuel barrel.")]
    [SerializeField] private float fuelCanWeight = 60f;

    [Tooltip("Weight for a lane with oil.")]
    [SerializeField] private float oilSpillWeight = 2f;

    [Tooltip("Weight for a lane with a solid/hazard obstacle or barrier.")]
    [SerializeField] private float obstacleLaneWeight = 0f;

    [Header("Oil Effect")]
    [SerializeField] private float oilSlowMultiplier = 0.55f;
    [SerializeField] private float oilSlowDuration = 1.6f;
    [SerializeField] private float oilFuelLoss = 10f;

    [Header("Obstacle & Barrier Effect")]
    [Tooltip("Optional extra tag used by obstacle objects in the scene.")]
    [SerializeField] private string obstacleTag = "Obstacle";

    [Tooltip("Speed multiplier when hitting a cone/obstacle.")]
    [Range(0f, 1f)]
    [SerializeField] private float obstacleSlowMultiplier = 0.45f;

    [Tooltip("Duration in seconds of the obstacle slowdown.")]
    [Min(0.1f)]
    [SerializeField] private float obstacleSlowDuration = 1.0f;

    [Tooltip("Fuel penalty when hitting a cone/obstacle.")]
    [Min(0f)]
    [SerializeField] private float obstacleFuelPenalty = 6f;

    [Tooltip("Bloqueo al chocar contra una valla STOP para que no la atraviese.")]
    [SerializeField] private float barrierBlockDuration = 0.85f;

    [Range(0f, 0.3f)]
    [SerializeField] private float barrierSpeedMultiplier = 0.05f;

    [Min(0f)]
    [SerializeField] private float barrierFuelPenalty = 12f;

    [Header("Start Lane")]
    [Tooltip("Initial lane of the CPU kart. Example: -1, 0, 1, 2.")]
    [SerializeField] private int startLane = 1;

    [Header("Turbo / Boost Settings")]
    [Tooltip("Opcional: arrastra el KartLaneController del jugador para copiar su Max Boost Uses. Si queda vacío, se busca automáticamente en la escena.")]
    [SerializeField] private KartLaneController playerKartController;

    [Tooltip("Copia automáticamente Max Boost Uses desde KartLaneController del jugador.")]
    [SerializeField] private bool syncMaxBoostUsesWithPlayer = true;

    [Tooltip("Speed multiplier during the boost.")]
    [SerializeField] private float boostSpeedMultiplier = 1.85f;

    [Tooltip("Duration of the boost in seconds.")]
    [SerializeField] private float boostDuration = 2.0f;

    [Tooltip("Maximum number of times the CPU can use boost in this level (usado si no se sincroniza con el jugador).")]
    [Min(0)]
    [SerializeField] private int maxBoostUses = 3;

    [Tooltip("Chance per second (0.0 to 1.0) for the CPU to activate boost when eligible.")]
    [SerializeField] private float boostChancePerSecond = 0.35f;

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

    private float boostWaitMin = 2.6f;
    private float boostWaitMax = 4.8f;
    private float boostWaitEndMin = 6.2f;
    private float boostWaitEndMax = 10.2f;

    private Coroutine activeBoostCoroutine;
    private Animator animator;

    private int currentLane;
    private int targetLane;
    private float laneChangeTimer;
    private float lastLaneChangeTime = -999f;
    private float startXPosition;

    private float runSpeedMultiplier = 1f;
    private float currentForwardSpeed;
    private KartFuelSystem fuelSystem;
    private DistanceTracker distanceTracker;
    private Camera mainCam;
    private bool isSliding = false;
    private bool isObstacleSlowed = false;
    private bool isBarrierBlocked = false;

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
        mainCam = Camera.main;

        startXPosition = transform.position.x - (Mathf.Clamp(startLane, minLane, maxLane) * laneOffset);

        RollHumanRunProfile();
        currentLane = Mathf.Clamp(startLane, minLane, maxLane);
        targetLane = currentLane;

        currentForwardSpeed = GetCurrentBaseCpuSpeed();
        SyncBoostUsesFromPlayer();
        laneChangeTimer = 0.05f;

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

    private void SyncBoostUsesFromPlayer()
    {
        if (syncMaxBoostUsesWithPlayer)
        {
            if (playerKartController == null)
            {
                playerKartController = FindAnyObjectByType<KartLaneController>();
            }

            if (playerKartController != null)
            {
                maxBoostUses = Mathf.Max(0, playerKartController.MaxBoostUses);
            }
        }

        usesRemaining = Mathf.Max(0, maxBoostUses);
    }

    private void RollHumanRunProfile()
    {
        float variance = Mathf.Clamp(speedVariancePerRun, 0f, 0.35f);
        runSpeedMultiplier = 1f + Random.Range(-variance, variance);
    }

    private float GetFuelRatio()
    {
        if (fuelSystem == null || fuelSystem.maxFuel <= 0f)
            return 0.6f;

        return Mathf.Clamp01(fuelSystem.currentFuel / fuelSystem.maxFuel);
    }

    private float GetFuelUrgency()
    {
        float ratio = GetFuelRatio();
        if (ratio >= lowFuelThresholdRatio)
            return 0.25f;

        return Mathf.Clamp01((lowFuelThresholdRatio - ratio) / Mathf.Max(0.05f, lowFuelThresholdRatio)) + 0.25f;
    }

    private float GetCurrentBaseCpuSpeed()
    {
        float baseSpeed = cpuSpeed * runSpeedMultiplier;

        if (!enableProgressiveSpeed || distanceTracker == null || distanceTracker.maxDistance <= 0f)
        {
            return baseSpeed;
        }

        float progress = Mathf.Clamp01(distanceTracker.currentDistance / distanceTracker.maxDistance);
        float targetMax = maxProgressiveCpuSpeed * runSpeedMultiplier;
        return Mathf.Lerp(baseSpeed, targetMax, progress);
    }

    public void ConfigureForLevel(float difficulty, int boostUsesForLevel)
    {
        maxBoostUses = Mathf.Max(0, boostUsesForLevel);
        SyncBoostUsesFromPlayer();
        boostChancePerSecond = Mathf.Clamp(0.35f * Mathf.Max(0.75f, difficulty), 0.14f, 0.58f);
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
            SyncBoostUsesFromPlayer();
            RollHumanRunProfile();
            nextBoostAllowedTime = Time.time + CalculateBoostWait();
            StartEngineSound();
            ChooseLane();
        }

        if (fuelSystem != null && fuelSystem.IsOutOfFuel)
            return;

        if (!isBoosting && !isSliding && !isObstacleSlowed && !isBarrierBlocked)
        {
            currentForwardSpeed = GetCurrentBaseCpuSpeed();
        }

        MoveCPU();

        bool isSettledInLane = currentLane == targetLane;
        bool cooldownReady = (Time.time - lastLaneChangeTime) >= minTimeBetweenLaneChanges;

        if (isSettledInLane && cooldownReady && HasImmediateHazard(currentLane, 7f))
        {
            ChooseLane();
            laneChangeTimer = laneChangeInterval;
        }

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
            laneChangeTimer = laneChangeInterval;
            if (isSettledInLane && cooldownReady)
            {
                ChooseLane();
            }
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

        if (Mathf.Abs(transform.position.x - targetX) < 0.08f)
        {
            currentLane = targetLane;
        }
    }

    private bool IsVisibleInCamera(Vector3 worldPoint)
    {
        if (!useMainCameraFOV)
            return true;

        if (mainCam == null)
        {
            mainCam = Camera.main;
        }

        if (mainCam == null)
            return true;

        Vector3 vp = mainCam.WorldToViewportPoint(worldPoint);
        return vp.z > 0f && vp.x >= -0.08f && vp.x <= 1.08f && vp.y >= -0.08f && vp.y <= 1.08f;
    }

    private string ResolveObstacleTag(Collider col)
    {
        if (col.CompareTag("Oil") || col.CompareTag("Cone") || col.CompareTag("Barrier"))
        {
            return col.tag;
        }

        if (!string.IsNullOrEmpty(obstacleTag) && col.CompareTag(obstacleTag))
        {
            return obstacleTag;
        }

        Transform root = col.transform.root;
        if (root != null)
        {
            if (root.CompareTag("Oil") || root.CompareTag("Cone") || root.CompareTag("Barrier"))
            {
                return root.tag;
            }
            if (!string.IsNullOrEmpty(obstacleTag) && root.CompareTag(obstacleTag))
            {
                return obstacleTag;
            }
        }

        return string.Empty;
    }

    private bool IsHazardTag(string tagName)
    {
        return tagName == "Oil" ||
               tagName == "Cone" ||
               tagName == "Barrier" ||
               (!string.IsNullOrEmpty(obstacleTag) && tagName == obstacleTag);
    }

    private bool IsFuelCollider(Collider col)
    {
        if (col.CompareTag("Fuel"))
            return true;

        Transform root = col.transform.root;
        return root != null && root.CompareTag("Fuel");
    }

    private void ChooseLane()
    {
        float urgency = GetFuelUrgency();
        int bestLane = currentLane;
        float bestWeight = EvaluateLaneWeight(currentLane, urgency);

        for (int lane = minLane; lane <= maxLane; lane++)
        {
            if (lane == currentLane)
                continue;

            float weight = EvaluateLaneWeight(lane, urgency);
            if (weight > bestWeight + 2f)
            {
                bestWeight = weight;
                bestLane = lane;
            }
        }

        if (bestLane != currentLane)
        {
            int dir = bestLane > currentLane ? 1 : -1;
            int stepCount = Mathf.Clamp(Mathf.Abs(bestLane - currentLane), 1, maxLaneStepPerDecision);
            int nextLane = Mathf.Clamp(currentLane + (dir * stepCount), minLane, maxLane);

            if (!HasImmediateHazard(nextLane, 5f))
            {
                targetLane = nextLane;
                lastLaneChangeTime = Time.time;
                return;
            }

            int singleStepLane = Mathf.Clamp(currentLane + dir, minLane, maxLane);
            if (singleStepLane != nextLane && !HasImmediateHazard(singleStepLane, 5f))
            {
                targetLane = singleStepLane;
                lastLaneChangeTime = Time.time;
                return;
            }

            int altLane = Mathf.Clamp(currentLane - dir, minLane, maxLane);
            if (altLane != currentLane && !HasImmediateHazard(altLane, 5f))
            {
                targetLane = altLane;
                lastLaneChangeTime = Time.time;
            }
        }
    }

    private bool HasImmediateHazard(int lane, float checkDistance)
    {
        float targetX = GetLanePosition(lane);
        Vector3 boxOrigin = new Vector3(targetX, transform.position.y + 0.5f, transform.position.z + 1.0f);
        Vector3 halfExtents = new Vector3(0.65f, 0.4f, 0.3f);

        RaycastHit[] hits = Physics.BoxCastAll(
            boxOrigin,
            halfExtents,
            Vector3.forward,
            Quaternion.identity,
            checkDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (RaycastHit hit in hits)
        {
            if (!IsVisibleInCamera(hit.point))
                continue;

            string tagName = ResolveObstacleTag(hit.collider);
            if (IsHazardTag(tagName) && hit.distance <= checkDistance)
            {
                return true;
            }
        }

        return false;
    }

    private float EvaluateLaneWeight(int lane, float urgency)
    {
        float targetX = GetLanePosition(lane);
        Vector3 boxOrigin = new Vector3(targetX, transform.position.y + 0.5f, transform.position.z + 1.0f);
        Vector3 halfExtents = new Vector3(0.65f, 0.4f, 0.3f);

        RaycastHit[] hits = Physics.BoxCastAll(
            boxOrigin,
            halfExtents,
            Vector3.forward,
            Quaternion.identity,
            viewDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        float nearestHazard = Mathf.Infinity;
        string nearestHazardTag = string.Empty;
        float nearestFuel = Mathf.Infinity;

        foreach (RaycastHit hit in hits)
        {
            if (!IsVisibleInCamera(hit.point))
                continue;

            string tagName = ResolveObstacleTag(hit.collider);
            if (IsHazardTag(tagName) && hit.distance < nearestHazard)
            {
                nearestHazard = hit.distance;
                nearestHazardTag = tagName;
            }
            else if (IsFuelCollider(hit.collider) && hit.distance < nearestFuel)
            {
                nearestFuel = hit.distance;
            }
        }

        bool seesFuelAhead = nearestFuel < Mathf.Infinity && nearestFuel <= viewDistance;

        if (seesFuelAhead)
        {
            float urgencyBoost = urgency * 35f;
            float proximityBoost = Mathf.Clamp((viewDistance - nearestFuel) * 0.75f, 0f, 18f);
            float fuelScore = fuelCanWeight + urgencyBoost + proximityBoost;

            if (nearestHazard < Mathf.Infinity)
            {
                bool isBarrier = nearestHazardTag == "Barrier";
                bool hazardRightInFront = nearestHazard < (isBarrier ? 7.5f : 4.5f);

                if (!hazardRightInFront && (riskForFuelWhenLow || nearestFuel < nearestHazard))
                {
                    return fuelScore;
                }

                if (nearestFuel >= nearestHazard)
                {
                    float hazardUrgency = 1f - (nearestHazard / Mathf.Max(0.1f, viewDistance));
                    return Mathf.Min(oilSpillWeight, obstacleLaneWeight) - (hazardUrgency * 12f);
                }
            }

            return fuelScore;
        }

        if (nearestHazard < Mathf.Infinity && nearestHazard <= viewDistance)
        {
            float hazardUrgency = 1f - (nearestHazard / Mathf.Max(0.1f, viewDistance));
            return Mathf.Min(oilSpillWeight, obstacleLaneWeight) - (hazardUrgency * 12f);
        }

        return emptyLaneWeight;
    }

    private float GetLanePosition(int lane)
    {
        return startXPosition + (lane * laneOffset);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleObstacleOrFuelContact(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider != null)
        {
            HandleObstacleOrFuelContact(collision.collider);
        }
    }

    private void HandleObstacleOrFuelContact(Collider other)
    {
        if (IsFuelCollider(other))
        {
            fuelBarrelsCollected++;
            if (fuelSystem != null)
            {
                fuelSystem.AddFuel();
            }
            other.gameObject.SetActive(false);
            return;
        }

        string resolvedTag = ResolveObstacleTag(other);
        if (string.IsNullOrEmpty(resolvedTag))
            return;

        if (resolvedTag == "Oil")
        {
            oilSpillsHit++;
            if (fuelSystem != null && oilFuelLoss > 0f)
            {
                fuelSystem.ConsumeFuel(oilFuelLoss);
            }
            ApplyOilSlow(oilSlowDuration);
            return;
        }

        if (resolvedTag == "Barrier")
        {
            obstaclesHit++;
            if (fuelSystem != null && barrierFuelPenalty > 0f)
            {
                fuelSystem.ConsumeFuel(barrierFuelPenalty);
            }
            StartCoroutine(BarrierBlockRoutine(barrierBlockDuration, barrierSpeedMultiplier));
            return;
        }

        obstaclesHit++;
        if (fuelSystem != null && obstacleFuelPenalty > 0f)
        {
            fuelSystem.ConsumeFuel(obstacleFuelPenalty);
        }
        ApplyObstacleSlow(obstacleSlowDuration, obstacleSlowMultiplier);
    }

    private IEnumerator BarrierBlockRoutine(float duration, float multiplier)
    {
        isBarrierBlocked = true;
        currentForwardSpeed = GetCurrentBaseCpuSpeed() * Mathf.Clamp(multiplier, 0f, 0.3f);

        yield return new WaitForSeconds(duration);

        isBarrierBlocked = false;
        if ((fuelSystem == null || !fuelSystem.IsOutOfFuel) && !isBoosting && !isSliding && !isObstacleSlowed)
        {
            currentForwardSpeed = GetCurrentBaseCpuSpeed();
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

        yield return new WaitForSeconds(duration);

        if ((fuelSystem == null || !fuelSystem.IsOutOfFuel) && !isBoosting && !isObstacleSlowed && !isBarrierBlocked)
        {
            currentForwardSpeed = GetCurrentBaseCpuSpeed();
        }

        isSliding = false;
    }

    private void ApplyObstacleSlow(float duration, float multiplier)
    {
        StartCoroutine(ObstacleSlowRoutine(duration, multiplier));
    }

    private IEnumerator ObstacleSlowRoutine(float duration, float multiplier)
    {
        isObstacleSlowed = true;
        currentForwardSpeed = GetCurrentBaseCpuSpeed() * Mathf.Clamp01(multiplier);

        yield return new WaitForSeconds(duration);

        isObstacleSlowed = false;

        if ((fuelSystem == null || !fuelSystem.IsOutOfFuel) && !isBoosting && !isSliding && !isBarrierBlocked)
        {
            currentForwardSpeed = GetCurrentBaseCpuSpeed();
        }
    }

    public void ResetForNewTurn()
    {
        StopAllCoroutines();

        canMove = false;
        transform.position = startPosition;
        transform.rotation = startRotation;

        SyncBoostUsesFromPlayer();
        RollHumanRunProfile();
        currentLane = Mathf.Clamp(startLane, minLane, maxLane);
        targetLane = currentLane;
        lastLaneChangeTime = -999f;

        currentForwardSpeed = GetCurrentBaseCpuSpeed();
        laneChangeTimer = 0.05f;

        isSliding = false;
        isObstacleSlowed = false;
        isBarrierBlocked = false;
        isBoosting = false;
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

        if ((fuelSystem == null || !fuelSystem.IsOutOfFuel) && !isSliding && !isObstacleSlowed && !isBarrierBlocked)
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