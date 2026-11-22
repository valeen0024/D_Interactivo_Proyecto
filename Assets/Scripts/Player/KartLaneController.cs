using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KartLaneController : MonoBehaviour
{
    [Header("Lane Configuration")]
    [Tooltip("Horizontal distance between each lane center.")]
    [SerializeField] private float laneOffset = 2f;

    [Tooltip("Speed at which the kart transitions horizontally between lanes.")]
    [SerializeField] private float laneChangeSpeed = 15f;

    [Header("Speed & Inertia Settings")]
    [Tooltip("Target max forward speed during normal driving.")]
    [SerializeField] private float baseForwardSpeed = 15f;

    [Tooltip("How fast the vehicle accelerates up to its target speed.")]
    [SerializeField] private float acceleration = 12f;

    [Tooltip("How fast the vehicle loses speed when releasing gas or idling.")]
    [SerializeField] private float friction = 2f;

    [Header("Progressive Speed (Level 2+)")]
    [Tooltip("Activa el aumento gradual de velocidad según el avance en la pista.")]
    [SerializeField] private bool enableProgressiveSpeed = false;

    [Tooltip("Velocidad máxima que alcanzará el kart al llegar al final de la pista.")]
    [SerializeField] private float maxProgressiveForwardSpeed = 22f;

    [Tooltip("Curva opcional para controlar cómo sube la velocidad (lineal por defecto).")]
    [SerializeField] private AnimationCurve speedProgressionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Lane Limits (4 Lanes Setup)")]
    [Tooltip("Minimum lane index (most left lane).")]
    [SerializeField] private int minLane = -1;

    [Tooltip("Maximum lane index (most right lane).")]
    [SerializeField] private int maxLane = 2;

    [Header("Start Control")]
    [Tooltip("Toggles whether the kart is allowed to respond to input and move.")]
    public bool canMove = false;

    [Header("Current State")]
    [Tooltip("Current lane index (-1, 0, 1, 2).")]
    [SerializeField] private int currentLane = 0;
    public int CurrentLane => currentLane;

    [Header("Boost / Turbo (Many Opportunities, Limited Uses per Level)")]
    [Tooltip("Speed multiplier during the boost.")]
    [SerializeField] private float boostSpeedMultiplier = 1.8f;

    [Tooltip("Extra fuel consumption multiplier while boost is active.")]
    [Min(1f)]
    [SerializeField] private float boostFuelConsumptionMultiplier = 2.2f;

    [Tooltip("Duration of the boost in seconds.")]
    [SerializeField] private float boostDuration = 2f;

    [Tooltip("Max boost uses allowed in this level (e.g., 3 in Level 1, 2 in Level 2).")]
    [Min(0)]
    [SerializeField] private int maxBoostUses = 3;

    [Tooltip("Seconds the activation window stays open once ready.")]
    [Min(1f)]
    [SerializeField] private float boostActivationWindow = 5.5f;

    [Tooltip("If true, requires collecting at least one fuel drum before opportunities start opening.")]
    [SerializeField] private bool requireCollectionBeforeBoost = true;

    [Tooltip("If true, requires fuel to reach the threshold in KartFuelSystem to open a window.")]
    [SerializeField] private bool requireFullFuelForBoost = true;

    [Tooltip("Difficulty factor of the current level (1.0 in Level 1, higher in later levels).")]
    [Range(0.5f, 2.5f)]
    [SerializeField] private float levelDifficulty = 1.0f;

    [Header("Progressive Race Wait (Scales with Race Progress & Level Difficulty)")]
    [Tooltip("Minimum wait time (seconds) near the start of the race.")]
    [Min(0f)]
    [SerializeField] private float startMinWaitSeconds = 1.5f;

    [Tooltip("Maximum wait time (seconds) near the start of the race.")]
    [Min(0f)]
    [SerializeField] private float startMaxWaitSeconds = 3.0f;

    [Tooltip("Minimum wait time (seconds) near the end of the race.")]
    [Min(0f)]
    [SerializeField] private float endMinWaitSeconds = 4.0f;

    [Tooltip("Maximum wait time (seconds) near the end of the race.")]
    [Min(0f)]
    [SerializeField] private float endMaxWaitSeconds = 7.0f;

    [SerializeField] private int usesRemaining = 0;
    [SerializeField] private float waitTimerRemaining = 0f;
    [SerializeField] private float currentActiveWaitDuration = 0f;
    [SerializeField] private float activationWindowRemaining = 0f;

    public int MaxBoostUses => maxBoostUses;
    public int UsesRemaining => usesRemaining;
    public bool HasRemainingUses => usesRemaining > 0;
    public bool IsWaitingBeforeRecharge => waitTimerRemaining > 0f;
    public bool IsBoostReadyToTrigger => activationWindowRemaining > 0f && !isBoosting && waitTimerRemaining <= 0f && HasRemainingUses;
    public bool ShouldShowBoostOverlay => IsBoostReadyToTrigger || isBoosting;

    [Header("Obstacle Settings")]
    [Tooltip("Legacy fallback tag used by obstacle objects (other than Oil, Cone, Barrier).")]
    [SerializeField] private string obstacleTag = "Obstacle";

    [Tooltip("Speed multiplier when hitting a cone or generic obstacle.")]
    [Range(0f, 1f)]
    [SerializeField] private float obstacleSpeedMultiplier = 0.45f;

    [Tooltip("Duration in seconds of the cone / obstacle slowdown.")]
    [Min(0.1f)]
    [SerializeField] private float obstacleSlowDuration = 1.6f;

    [Tooltip("Optional fuel penalty when hitting a generic obstacle.")]
    [Min(0f)]
    [SerializeField] private float obstacleFuelPenalty = 0f;

    [Tooltip("Margen en Z para evitar que el kart penetre la valla al chocar.")]
    [Min(0f)]
    [SerializeField] private float barrierStopMarginZ = 0.15f;

    [Header("Boost UI Overlay (Borde + Barril con rayos)")]
    [Tooltip("Grupo/objeto padre que contiene el borde y el barril con rayos sobrepuestos.")]
    [SerializeField] private GameObject boostOverlayGroup;

    [Tooltip("Opcional: objeto base si también quieres alternar su visibilidad.")]
    [SerializeField] private GameObject barrelIndicatorObject;

    [Tooltip("Si está activo, oculta el indicador base cuando se muestra el overlay de propulsor.")]
    [SerializeField] private bool hideBaseIndicatorWhenOverlayActive = false;

    [Tooltip("Opcional: si el indicador tiene componente UI Image, cambia su color/sprite.")]
    [SerializeField] private Image barrelIconImage;

    [SerializeField] private Color normalBarrelColor = Color.white;
    [SerializeField] private Color readyBarrelColor = new Color(0.25f, 1f, 0.85f, 1f);

    [Tooltip("Sprite opcional cuando el propulsor está listo (si se deja vacío, solo cambia el color).")]
    [SerializeField] private Sprite normalBarrelSprite;
    [SerializeField] private Sprite readyBarrelSprite;

    [Header("Boost Typewriter Notification (Configurable en Inspector)")]
    [Tooltip("Activa o desactiva el aviso en pantalla al usar un propulsor.")]
    [SerializeField] private bool enableBoostNotification = true;

    [Tooltip("Texto TMP donde se escribe temporalmente cuántos propulsores quedan.")]
    [SerializeField] private TMP_Text boostNotificationText;

    [Tooltip("Contenedor/panel opcional del aviso (si se deja vacío, oculta solo el texto).")]
    [SerializeField] private GameObject boostNotificationContainer;

    [Tooltip("Segundos entre cada letra mientras se escribe el mensaje.")]
    [Range(0.005f, 0.2f)]
    [SerializeField] private float typewriterCharDelay = 0.035f;

    [Tooltip("Segundos que el mensaje queda visible completo antes de ocultarse.")]
    [Range(0.2f, 10f)]
    [SerializeField] private float holdAfterTypingSeconds = 3.0f;

    [Tooltip("Segundos extra que permanece visible cuando se agotan los propulsores (0 para usar el mismo tiempo).")]
    [Range(0f, 8f)]
    [SerializeField] private float extraHoldWhenEmptySeconds = 1.0f;

    [Header("Audio Settings")]
    [Tooltip("Grupo SFX de tu AudioMixer para que el slider de efectos controle motor y turbo.")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Tooltip("Engine sound.")]
    [SerializeField] private AudioClip engineClip;

    [Tooltip("Turbo sound.")]
    [SerializeField] private AudioClip turboClip;

    [Tooltip("Normal engine volume and tone.")]
    [SerializeField] private float normalEngineVolume = 0.5f;
    [SerializeField] private float normalEnginePitch = 1.0f;

    [Tooltip("Engine volume and tone when the turbo is active.")]
    [SerializeField] private float turboEngineVolume = 1.0f;
    [SerializeField] private float turboEnginePitch = 1.5f;

    [Tooltip("Duration of the Fade In / Fade Out effect in seconds.")]
    [SerializeField] private float engineFadeDuration = 1.0f;

    [Tooltip("Duration in seconds for the engine sound to return to normal after the turbo.")]
    [SerializeField] private float turboCooldownDuration = 0.5f;

    private bool isFadingOut = false;

    private AudioSource engineAudioSource;
    private AudioSource turboAudioSource;

    private Animator animator;

    public float currentForwardSpeed;
    private float targetForwardSpeed;
    private Vector3 targetPosition;
    private KartFuelSystem fuelSystem;
    private DistanceTracker distanceTracker;
    private Coroutine activeBoostCoroutine;
    private Coroutine notificationCoroutine;
    private Coroutine oilSlideCoroutine;
    private Coroutine obstacleSlowCoroutine;
    private bool wasWindowOpenLastFrame = false;
    private bool isSliding = false;
    private bool isObstacleSlowed = false;
    private float activeObstacleMultiplier = 0.45f;
    private bool isBoosting = false;

    private bool isBarrierBlocked = false;
    private int blockedLane = int.MinValue;
    private float barrierFrontZ = float.MaxValue;

    public bool HasUsedTurbo => usesRemaining < maxBoostUses;
    public bool canUseTurbo = true;

    private float startXPosition;

    private void Start()
    {
        fuelSystem = GetComponent<KartFuelSystem>();
        distanceTracker = GetComponent<DistanceTracker>();
        animator = GetComponentInChildren<Animator>();

        if (barrelIconImage == null && barrelIndicatorObject != null)
        {
            barrelIconImage = barrelIndicatorObject.GetComponentInChildren<Image>();
        }

        if (barrelIconImage != null && normalBarrelSprite == null)
        {
            normalBarrelSprite = barrelIconImage.sprite;
        }

        ResetBoostSystem();
        UpdateBarrelVisual();

        targetForwardSpeed = 0f;
        currentForwardSpeed = 0f;
        targetPosition = transform.position;
        startXPosition = transform.position.x;

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

    public void ConfigureForLevel(
        float difficulty,
        int boostUsesForLevel,
        bool mustCollectFirst = true,
        bool mustHaveFullFuel = true)
    {
        levelDifficulty = Mathf.Clamp(difficulty, 0.5f, 2.5f);
        maxBoostUses = Mathf.Max(0, boostUsesForLevel);
        requireCollectionBeforeBoost = mustCollectFirst;
        requireFullFuelForBoost = mustHaveFullFuel;
        ResetBoostSystem();
    }

    public void ResetBoostSystem()
    {
        StopActiveBoost(restoreNormalConsumption: true);
        usesRemaining = Mathf.Max(0, maxBoostUses);
        waitTimerRemaining = 0f;
        currentActiveWaitDuration = 0f;
        activationWindowRemaining = 0f;
        wasWindowOpenLastFrame = false;
        ClearBarrierBlock();

        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
            notificationCoroutine = null;
        }
        SetBoostNotificationVisible(false);

        UpdateBarrelVisual();
    }

    public void RegisterFuelCollected() { }

    public void SetForwardSpeed(float speed)
    {
        float clampedSpeed = Mathf.Max(0f, speed);
        targetForwardSpeed = clampedSpeed;
        currentForwardSpeed = clampedSpeed;
    }

    private void ClearBarrierBlock()
    {
        isBarrierBlocked = false;
        blockedLane = int.MinValue;
        barrierFrontZ = float.MaxValue;
    }

    private void ShowBoostRemainingNotification()
    {
        if (!enableBoostNotification || boostNotificationText == null) return;

        string message;
        float extraHold = 0f;

        if (usesRemaining == 1)
        {
            message = "Te queda 1 propulsor";
        }
        else if (usesRemaining > 1)
        {
            message = $"Te quedan {usesRemaining} propulsores";
        }
        else
        {
            message = "No te quedan propulsores";
            extraHold = extraHoldWhenEmptySeconds;
        }

        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }

        notificationCoroutine = StartCoroutine(TypewriterNotificationRoutine(message, extraHold));
    }

    private IEnumerator TypewriterNotificationRoutine(string fullMessage, float extraHold)
    {
        SetBoostNotificationVisible(true);
        boostNotificationText.text = string.Empty;

        float charDelay = Mathf.Max(0.005f, typewriterCharDelay);
        for (int i = 0; i < fullMessage.Length; i++)
        {
            boostNotificationText.text += fullMessage[i];
            yield return new WaitForSeconds(charDelay);
        }

        float totalHold = Mathf.Max(0.2f, holdAfterTypingSeconds + extraHold);
        yield return new WaitForSeconds(totalHold);

        SetBoostNotificationVisible(false);
        notificationCoroutine = null;
    }

    private void SetBoostNotificationVisible(bool visible)
    {
        if (boostNotificationContainer != null)
        {
            boostNotificationContainer.SetActive(visible);
        }
        else if (boostNotificationText != null)
        {
            boostNotificationText.gameObject.SetActive(visible);
        }
    }

    private float GetRaceProgressNormalized()
    {
        if (distanceTracker != null && distanceTracker.maxDistance > 0f)
        {
            return Mathf.Clamp01(distanceTracker.currentDistance / distanceTracker.maxDistance);
        }
        return 0f;
    }

    private float RollNextWaitDuration()
    {
        float progress = GetRaceProgressNormalized();
        float difficultyFactor = Mathf.Max(0.5f, levelDifficulty);

        float minBase = Mathf.Lerp(startMinWaitSeconds, endMinWaitSeconds, progress);
        float maxBase = Mathf.Lerp(startMaxWaitSeconds, endMaxWaitSeconds, progress);
        if (maxBase < minBase) maxBase = minBase;

        float rolled = UnityEngine.Random.Range(minBase, maxBase);
        float scaledWait = rolled * Mathf.Lerp(1f, difficultyFactor, 0.5f);
        return Mathf.Max(minBase, scaledWait);
    }

    private bool CanOpenBoostOpportunity()
    {
        if (!canUseTurbo || !HasRemainingUses || isBoosting || waitTimerRemaining > 0f)
            return false;

        if (fuelSystem != null)
        {
            if (fuelSystem.IsOutOfFuel)
                return false;

            if (requireCollectionBeforeBoost && !fuelSystem.HasCollectedFuel)
                return false;

            if (requireFullFuelForBoost && !fuelSystem.IsFuelAtMax)
                return false;
        }

        return true;
    }

    private void UpdateBoostOpportunityAndVisual()
    {
        if (waitTimerRemaining > 0f)
        {
            waitTimerRemaining = Mathf.Max(0f, waitTimerRemaining - Time.deltaTime);
            activationWindowRemaining = 0f;
            wasWindowOpenLastFrame = false;
        }
        else if (!isBoosting)
        {
            if (activationWindowRemaining > 0f)
            {
                activationWindowRemaining = Mathf.Max(0f, activationWindowRemaining - Time.deltaTime);

                if (activationWindowRemaining <= 0f && wasWindowOpenLastFrame)
                {
                    currentActiveWaitDuration = RollNextWaitDuration();
                    waitTimerRemaining = currentActiveWaitDuration;
                    wasWindowOpenLastFrame = false;
                }
            }
            else if (CanOpenBoostOpportunity())
            {
                activationWindowRemaining = boostActivationWindow;
                wasWindowOpenLastFrame = true;
            }
            else
            {
                wasWindowOpenLastFrame = false;
            }
        }
        else
        {
            activationWindowRemaining = 0f;
            wasWindowOpenLastFrame = false;
        }

        UpdateBarrelVisual();
    }

    private void UpdateBarrelVisual()
    {
        bool overlayVisible = ShouldShowBoostOverlay;
        bool ready = IsBoostReadyToTrigger;

        if (boostOverlayGroup != null && boostOverlayGroup.activeSelf != overlayVisible)
        {
            boostOverlayGroup.SetActive(overlayVisible);
        }

        if (barrelIndicatorObject != null && hideBaseIndicatorWhenOverlayActive)
        {
            bool showBase = !overlayVisible;
            if (barrelIndicatorObject.activeSelf != showBase)
            {
                barrelIndicatorObject.SetActive(showBase);
            }
        }

        if (barrelIconImage != null)
        {
            barrelIconImage.color = ready ? readyBarrelColor : normalBarrelColor;

            if (ready && readyBarrelSprite != null)
            {
                barrelIconImage.sprite = readyBarrelSprite;
            }
            else if (!ready && normalBarrelSprite != null)
            {
                barrelIconImage.sprite = normalBarrelSprite;
            }
        }
    }

    private bool CanTriggerBoostNow()
    {
        if (!canUseTurbo || !HasRemainingUses || isBoosting || waitTimerRemaining > 0f || activationWindowRemaining <= 0f)
            return false;

        if (fuelSystem != null && fuelSystem.IsOutOfFuel)
            return false;

        return true;
    }

    private void Update()
    {
        bool pressedBoostKey = Keyboard.current != null &&
            (Keyboard.current.upArrowKey.wasPressedThisFrame ||
             Keyboard.current.wKey.wasPressedThisFrame);

        if (!canMove)
        {
            UpdateBarrelVisual();
            return;
        }

        bool hasFuel = (fuelSystem == null || !fuelSystem.IsOutOfFuel);

        if (!hasFuel && isBoosting)
        {
            StopActiveBoost(restoreNormalConsumption: true);
        }

        UpdateBoostOpportunityAndVisual();

        if (pressedBoostKey && CanTriggerBoostNow())
        {
            ActivateTurbo();
        }

        HandleLaneInput();
        CalculateTargetSpeed();
        ApplyInertiaAndSpeed();
        MoveKart();
    }

    private void HandleLaneInput()
    {
        if (fuelSystem != null && fuelSystem.IsOutOfFuel) return;
        if (Keyboard.current == null) return;

        int previousLane = currentLane;

        if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            if (currentLane > minLane) currentLane--;
        }
        else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            if (currentLane < maxLane) currentLane++;
        }

        if (currentLane != previousLane && isBarrierBlocked && currentLane != blockedLane)
        {
            ClearBarrierBlock();
        }
    }

    private float GetCurrentBaseForwardSpeed()
    {
        if (!enableProgressiveSpeed)
        {
            return baseForwardSpeed;
        }

        float progress = GetRaceProgressNormalized();
        float curvedProgress = speedProgressionCurve != null ? speedProgressionCurve.Evaluate(progress) : progress;
        return Mathf.Lerp(baseForwardSpeed, maxProgressiveForwardSpeed, curvedProgress);
    }

    private void CalculateTargetSpeed()
    {
        if (fuelSystem != null && fuelSystem.IsOutOfFuel)
        {
            targetForwardSpeed = 0f;
            return;
        }

        if (isBarrierBlocked && currentLane == blockedLane)
        {
            targetForwardSpeed = 0f;
            return;
        }

        float activeBaseSpeed = GetCurrentBaseForwardSpeed();

        if (isBoosting)
        {
            targetForwardSpeed = activeBaseSpeed * boostSpeedMultiplier;
            return;
        }

        if (isSliding)
        {
            targetForwardSpeed = activeBaseSpeed * 0.5f;
            return;
        }

        if (isObstacleSlowed)
        {
            targetForwardSpeed = activeBaseSpeed * activeObstacleMultiplier;
            return;
        }

        targetForwardSpeed = activeBaseSpeed;
    }

    private void ApplyInertiaAndSpeed()
    {
        if (isBarrierBlocked && currentLane == blockedLane)
        {
            currentForwardSpeed = 0f;
            targetForwardSpeed = 0f;
            return;
        }

        if (currentForwardSpeed < targetForwardSpeed)
        {
            currentForwardSpeed += acceleration * Time.deltaTime;
            currentForwardSpeed = Mathf.Min(currentForwardSpeed, targetForwardSpeed);
        }
        else if (currentForwardSpeed > targetForwardSpeed)
        {
            currentForwardSpeed -= friction * 5f * Time.deltaTime;
            currentForwardSpeed = Mathf.Max(currentForwardSpeed, targetForwardSpeed);
        }
    }

    public void RestoreTutorialLane(int lane)
    {
        currentLane = Mathf.Clamp(lane, minLane, maxLane);
        ClearBarrierBlock();
    }

    public float GetLaneWorldX(int lane)
    {
        int safeLane = Mathf.Clamp(lane, minLane, maxLane);
        return startXPosition + (safeLane * laneOffset);
    }

    private void MoveKart()
    {
        float targetX = startXPosition + (currentLane * laneOffset);
        Vector3 currentPos = transform.position;
        float newX = Mathf.MoveTowards(currentPos.x, targetX, laneChangeSpeed * Time.deltaTime);

        float newZ = currentPos.z;
        if (!(isBarrierBlocked && currentLane == blockedLane))
        {
            newZ += currentForwardSpeed * Time.deltaTime;
            if (isBarrierBlocked && currentLane == blockedLane && newZ > barrierFrontZ)
            {
                newZ = barrierFrontZ;
            }
        }
        else if (currentPos.z > barrierFrontZ)
        {
            newZ = barrierFrontZ;
        }

        transform.position = new Vector3(newX, currentPos.y, newZ);
    }

    public void ApplyOilSlow(float duration)
    {
        float safeDuration = Mathf.Max(0.1f, duration);
        if (oilSlideCoroutine != null)
        {
            StopCoroutine(oilSlideCoroutine);
        }
        oilSlideCoroutine = StartCoroutine(OilSlideRoutine(safeDuration));
    }

    private IEnumerator OilSlideRoutine(float duration)
    {
        isSliding = true;
        yield return new WaitForSeconds(duration);
        isSliding = false;
        oilSlideCoroutine = null;
    }

    public void ApplyObstacleSlow(float duration, float speedMultiplier)
    {
        float safeDuration = Mathf.Max(0.1f, duration);
        activeObstacleMultiplier = Mathf.Clamp(speedMultiplier, 0f, 1f);

        if (obstacleSlowCoroutine != null)
        {
            StopCoroutine(obstacleSlowCoroutine);
        }
        obstacleSlowCoroutine = StartCoroutine(ObstacleSlowRoutine(safeDuration));
    }

    public void ApplyBarrierBlock(float duration = 0f, float blockMultiplier = 0f)
    {
        BlockAgainstBarrier(null);
    }

    public void BlockAgainstBarrier(Collider barrierCollider)
    {
        isBarrierBlocked = true;
        blockedLane = currentLane;
        currentForwardSpeed = 0f;
        targetForwardSpeed = 0f;

        if (barrierCollider != null)
        {
            float frontEdgeZ = barrierCollider.bounds.min.z - barrierStopMarginZ;
            if (transform.position.z > frontEdgeZ)
            {
                barrierFrontZ = Mathf.Min(barrierFrontZ, frontEdgeZ);
                transform.position = new Vector3(transform.position.x, transform.position.y, barrierFrontZ);
            }
            else
            {
                barrierFrontZ = Mathf.Min(barrierFrontZ, Mathf.Max(transform.position.z, frontEdgeZ));
            }
        }
        else
        {
            barrierFrontZ = Mathf.Min(barrierFrontZ, transform.position.z);
        }
    }

    private IEnumerator ObstacleSlowRoutine(float duration)
    {
        isObstacleSlowed = true;
        yield return new WaitForSeconds(duration);
        isObstacleSlowed = false;
        obstacleSlowCoroutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Oil"))
        {
            if (fuelSystem != null)
            {
                fuelSystem.ReduceFuelFromOil();
            }

            float duration = 2f;
            if (other.TryGetComponent<OilSlick>(out OilSlick oilScript))
            {
                duration = oilScript.slowDuration;
            }

            ApplyOilSlow(duration);
        }
        else if (other.CompareTag("Cone"))
        {
            ApplyObstacleSlow(obstacleSlowDuration, obstacleSpeedMultiplier);
        }
        else if (!string.IsNullOrEmpty(obstacleTag) && other.CompareTag(obstacleTag))
        {
            if (obstacleFuelPenalty > 0f && fuelSystem != null)
            {
                fuelSystem.ConsumeFuel(obstacleFuelPenalty);
            }

            ApplyObstacleSlow(obstacleSlowDuration, obstacleSpeedMultiplier);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Barrier"))
        {
            BlockAgainstBarrier(collision.collider);
        }
        else if (collision.gameObject.CompareTag("Cone"))
        {
            ApplyObstacleSlow(obstacleSlowDuration, obstacleSpeedMultiplier);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Barrier"))
        {
            BlockAgainstBarrier(collision.collider);
        }
    }

    private void ActivateTurbo()
    {
        usesRemaining = Mathf.Max(0, usesRemaining - 1);
        ShowBoostRemainingNotification();

        activationWindowRemaining = 0f;
        wasWindowOpenLastFrame = false;
        currentActiveWaitDuration = RollNextWaitDuration();
        waitTimerRemaining = currentActiveWaitDuration;
        UpdateBarrelVisual();

        if (animator != null)
        {
            animator.SetTrigger("Boost");
        }

        if (turboAudioSource != null)
        {
            turboAudioSource.outputAudioMixerGroup = sfxMixerGroup;
            turboAudioSource.Play();
        }

        StopActiveBoost(restoreNormalConsumption: false);
        activeBoostCoroutine = StartCoroutine(TurboRoutine());
    }

    private void StopActiveBoost(bool restoreNormalConsumption)
    {
        if (activeBoostCoroutine != null)
        {
            StopCoroutine(activeBoostCoroutine);
            activeBoostCoroutine = null;
        }

        isBoosting = false;

        if (restoreNormalConsumption && fuelSystem != null)
        {
            fuelSystem.SetFuelConsumptionMultiplier(1f);
        }
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

    private IEnumerator TurboRoutine()
    {
        isBoosting = true;
        UpdateBarrelVisual();

        if (fuelSystem != null)
        {
            fuelSystem.SetFuelConsumptionMultiplier(boostFuelConsumptionMultiplier);
        }

        if (engineAudioSource != null)
        {
            engineAudioSource.volume = turboEngineVolume;
            engineAudioSource.pitch = turboEnginePitch;
        }

        float elapsed = 0f;
        while (elapsed < boostDuration)
        {
            if (fuelSystem != null && fuelSystem.IsOutOfFuel)
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (fuelSystem != null)
        {
            fuelSystem.SetFuelConsumptionMultiplier(1f);
        }

        if (fuelSystem == null || !fuelSystem.IsOutOfFuel)
        {
            currentForwardSpeed = GetCurrentBaseForwardSpeed();
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
        UpdateBarrelVisual();
    }
}