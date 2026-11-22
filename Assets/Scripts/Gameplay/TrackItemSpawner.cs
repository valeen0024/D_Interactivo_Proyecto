using System;
using System.Collections.Generic;
using UnityEngine;

public class TrackItemSpawner : MonoBehaviour
{
    public enum DifficultyPreset
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Custom = 3
    }

    [Serializable]
    public class ObstacleDefinition
    {
        [Tooltip("Nombre descriptivo para identificarlo fácil en el Inspector (ej. Aceite, Conos, Valla).")]
        public string label = "Aceite";

        [Tooltip("Prefab del obstáculo a instanciar.")]
        public GameObject prefab;

        [Tooltip("Altura Y en la pista para este obstáculo.")]
        public float heightY = 463.7f;

        [Tooltip("Ajuste extra de rotación (en grados) aplicado sobre la rotación del prefab al instanciar.")]
        public Vector3 rotationOffsetEuler = Vector3.zero;

        [Min(0.01f)]
        [Tooltip("Peso relativo de aparición frente a otros obstáculos.")]
        public float weight = 1f;

        [Range(0f, 1f)]
        [Tooltip("Progreso mínimo de la carrera (0 = inicio, 1 = meta) desde donde puede aparecer.")]
        public float minTrackProgress = 0f;

        [Range(0f, 1f)]
        [Tooltip("Progreso máximo de la carrera (0 = inicio, 1 = meta) hasta donde puede aparecer.")]
        public float maxTrackProgress = 1f;

        [Tooltip("Marcar si este obstáculo bloquea/castiga fuerte (ej. Tag 'Barrier') para limitar cuántos salen por oleada.")]
        public bool isBlockingBarrier = false;
    }

    [Header("Combustible (Bidón)")]
    [SerializeField] private GameObject fuelDrumPrefab;
    [SerializeField] private float fuelHeightY = 463.896f;

    [Header("Obstáculos (Escalable para nuevos niveles)")]
    [Tooltip("Agrega aquí Aceite (Oil), Conos (Cone), Valla (Barrier) o cualquier obstáculo nuevo.")]
    [SerializeField] private List<ObstacleDefinition> obstacles = new List<ObstacleDefinition>();

    [Header("Nivel, Dificultad y Distancia")]
    [Min(50f)]
    [SerializeField] private float targetDistanceYards = 800f;

    [SerializeField] private float metersPerYardEstimate = 1.168f;
    [SerializeField] private DifficultyPreset difficultyPreset = DifficultyPreset.Normal;

    [Range(0.25f, 2.5f)]
    [SerializeField] private float difficultyMultiplier = 1.0f;

    [Range(0.25f, 2.5f)]
    [SerializeField] private float fuelGenerosityMultiplier = 1.15f;

    [Header("Carriles y Límites de Pista")]
    [SerializeField] private float[] laneXPositions = new float[] { -104.9f, -102.9f, -100.9f, -98.9f };
    [SerializeField] private float laneMatchTolerance = 0.35f;
    [SerializeField] private float spawnAheadDistance = 42f;
    [SerializeField] private float minSpawnIntervalZ = 7.5f;
    [SerializeField] private float maxSpawnIntervalZ = 14.5f;
    [SerializeField] private float zStaggerRange = 4.2f;
    [SerializeField] private float safetyMarginBeforeFinish = 18f;
    [SerializeField] private float fallbackMaxSpawnZ = 896.78f;

    [Header("Ritmo y Progresividad")]
    [Range(0f, 1f)]
    [SerializeField] private float baseFuelSpawnChance = 0.65f;

    [Range(0f, 1f)]
    [SerializeField] private float baseObstacleSpawnChance = 0.85f;

    [Min(0f)]
    [Tooltip("Distancia mínima en Z en el mismo carril para evitar que un bidón quede encima o pegado a un obstáculo.")]
    [SerializeField] private float minSameLaneGapZ = 8.5f;

    [Min(0f)]
    [Tooltip("Distancia mínima en Z en el mismo carril entre obstáculos consecutivos para evitar encimamientos.")]
    [SerializeField] private float minObstacleGapZ = 7.0f;

    [Min(0)]
    [Tooltip("Máximo de vallas/obstáculos bloqueantes por oleada (1 evita muros imposibles de esquivar).")]
    [SerializeField] private int maxBlockingBarriersPerWave = 1;

    [Range(0f, 1f)]
    [Tooltip("Probabilidad muy baja (ej. 0.05) de permitir un bidón cerca/en un carril con obstáculo; 0 = nunca encima.")]
    [SerializeField] private float rareFuelNearObstacleChance = 0f;

    [Min(0)]
    [Tooltip("Oleadas iniciales sin obstáculos (0 para verlos desde el arranque en pruebas).")]
    [SerializeField] private int warmupWavesWithoutObstacles = 1;

    [Range(1, 3)]
    [SerializeField] private int startMinObstacles = 1;

    [Range(1, 3)]
    [SerializeField] private int startMaxObstacles = 1;

    [Range(1, 3)]
    [SerializeField] private int endMinObstacles = 1;

    [Range(1, 3)]
    [SerializeField] private int endMaxObstacles = 3;

    [Range(1, 2)]
    [SerializeField] private int guaranteedFreeLanes = 1;

    [Header("Depuración")]
    [SerializeField] private bool logSpawnDebug = false;

    private Transform playerTransform;
    private Transform cpuTransform;
    private float startZ;
    private float nextSpawnZ;
    private float calculatedMaxSpawnZ;
    private int waveIndex;
    private int lastFuelLane = -1;
    private int wavesSinceLastFuel;
    private bool isInitialized;
    private GameManager.GameState lastKnownState = GameManager.GameState.Waiting;

    private readonly List<GameObject> spawnedItems = new List<GameObject>();
    private readonly List<int> lanePool = new List<int>();

    private void OnValidate()
    {
        if (maxSpawnIntervalZ < minSpawnIntervalZ)
        {
            maxSpawnIntervalZ = minSpawnIntervalZ;
        }

        if (laneMatchTolerance < 0.05f)
        {
            laneMatchTolerance = 0.05f;
        }

        if (startMaxObstacles < startMinObstacles) startMaxObstacles = startMinObstacles;
        if (endMaxObstacles < endMinObstacles) endMaxObstacles = endMinObstacles;

        ApplyPresetIfNeeded();
    }

    private void ApplyPresetIfNeeded()
    {
        if (difficultyPreset == DifficultyPreset.Custom) return;

        switch (difficultyPreset)
        {
            case DifficultyPreset.Easy:
                difficultyMultiplier = 0.85f;
                fuelGenerosityMultiplier = 1.35f;
                break;
            case DifficultyPreset.Normal:
                difficultyMultiplier = 1.0f;
                fuelGenerosityMultiplier = 1.15f;
                break;
            case DifficultyPreset.Hard:
                difficultyMultiplier = 1.45f;
                fuelGenerosityMultiplier = 0.9f;
                break;
        }
    }

    private void Start()
    {
        ApplyPresetIfNeeded();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        GameObject cpu = GameObject.FindGameObjectWithTag("CPU");
        if (cpu != null) cpuTransform = cpu.transform;

        CalculateTrackLimits();
        ResetSpawnerForTurn();
        isInitialized = true;
    }

    private void CalculateTrackLimits()
    {
        GameObject startObj = GameObject.FindGameObjectWithTag("Start");
        startZ = (startObj != null) ? startObj.transform.position.z : GetLeadingKartZ();

        Transform finishTransform = null;
        FinishLine finishComponent = FindFirstObjectByType<FinishLine>();
        if (finishComponent != null)
        {
            finishTransform = finishComponent.transform;
        }
        else
        {
            GameObject gantry = GameObject.Find("MD_Finish_Line_Gantry");
            if (gantry != null) finishTransform = gantry.transform;
        }

        if (finishTransform != null)
        {
            calculatedMaxSpawnZ = finishTransform.position.z - safetyMarginBeforeFinish;
        }
        else if (targetDistanceYards > 0f && metersPerYardEstimate > 0f)
        {
            calculatedMaxSpawnZ = (startZ + (targetDistanceYards * metersPerYardEstimate)) - safetyMarginBeforeFinish;
        }
        else
        {
            calculatedMaxSpawnZ = fallbackMaxSpawnZ;
        }

        if (calculatedMaxSpawnZ <= startZ + 15f)
        {
            calculatedMaxSpawnZ = Mathf.Max(fallbackMaxSpawnZ, startZ + 100f);
        }
    }

    public void ResetSpawnerForTurn()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
            {
                Destroy(spawnedItems[i]);
            }
        }
        spawnedItems.Clear();

        waveIndex = 0;
        lastFuelLane = -1;
        wavesSinceLastFuel = 0;
        nextSpawnZ = startZ + UnityEngine.Random.Range(10f, 16f);
    }

    private void Update()
    {
        if (!isInitialized || GameManager.Instance == null) return;

        GameManager.GameState currentState = GameManager.Instance.gameState;

        if (currentState != lastKnownState)
        {
            if (currentState == GameManager.GameState.PlayerTurn || currentState == GameManager.GameState.CPUTurn)
            {
                ResetSpawnerForTurn();
            }
            lastKnownState = currentState;
        }

        if (currentState == GameManager.GameState.Waiting || currentState == GameManager.GameState.Finished)
        {
            return;
        }

        float activeZ = GetActiveKartZ(currentState);

        while (activeZ + spawnAheadDistance >= nextSpawnZ && nextSpawnZ <= calculatedMaxSpawnZ)
        {
            SpawnWaveAt(nextSpawnZ, waveIndex);
            waveIndex++;

            float progress = GetTrackProgress(nextSpawnZ);
            float speedFactor = Mathf.Lerp(1.15f, 0.85f, progress * Mathf.Clamp(difficultyMultiplier, 0.5f, 1.8f));
            float randomStep = UnityEngine.Random.Range(minSpawnIntervalZ, maxSpawnIntervalZ) * speedFactor;
            nextSpawnZ += Mathf.Max(4.5f, randomStep);
        }
    }

    private float GetTrackProgress(float zPos)
    {
        float span = Mathf.Max(1f, calculatedMaxSpawnZ - startZ);
        return Mathf.Clamp01((zPos - startZ) / span);
    }

    private float GetActiveKartZ(GameManager.GameState state)
    {
        if (state == GameManager.GameState.PlayerTurn && playerTransform != null)
            return playerTransform.position.z;

        if (state == GameManager.GameState.CPUTurn && cpuTransform != null)
            return cpuTransform.position.z;

        return GetLeadingKartZ();
    }

    private float GetLeadingKartZ()
    {
        float maxZ = float.MinValue;

        if (playerTransform != null && playerTransform.gameObject.activeInHierarchy)
            maxZ = Mathf.Max(maxZ, playerTransform.position.z);

        if (cpuTransform != null && cpuTransform.gameObject.activeInHierarchy)
            maxZ = Mathf.Max(maxZ, cpuTransform.position.z);

        return (maxZ == float.MinValue) ? 0f : maxZ;
    }

    private float GetStaggeredZ(float baseZ, float extraOffset = 0f)
    {
        float randomOffset = UnityEngine.Random.Range(-zStaggerRange, zStaggerRange) + extraOffset;
        return Mathf.Clamp(baseZ + randomOffset, startZ + 10f, calculatedMaxSpawnZ);
    }

    private bool HasNearbyItemInLane(float laneX, float candidateZ, float minGapZ, bool ignoreFuel)
    {
        if (minGapZ <= 0f) return false;

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            GameObject item = spawnedItems[i];
            if (item == null || !item.activeInHierarchy) continue;
            if (ignoreFuel && item.CompareTag("Fuel")) continue;

            bool sameLane = Mathf.Abs(item.transform.position.x - laneX) < laneMatchTolerance;
            bool closeInZ = Mathf.Abs(item.transform.position.z - candidateZ) < minGapZ;

            if (sameLane && closeInZ)
            {
                return true;
            }
        }

        return false;
    }

    private void SpawnWaveAt(float baseZ, int currentWave)
    {
        if (laneXPositions == null || laneXPositions.Length == 0) return;

        float progress = GetTrackProgress(baseZ);
        float diff = Mathf.Clamp(difficultyMultiplier, 0.25f, 2.5f);
        float fuelMult = Mathf.Clamp(fuelGenerosityMultiplier, 0.25f, 2.5f);

        BuildShuffledLanes();

        int safeCount = Mathf.Clamp(guaranteedFreeLanes, 1, laneXPositions.Length - 1);
        List<int> safeLanes = new List<int>();
        for (int i = 0; i < safeCount && lanePool.Count > 0; i++)
        {
            safeLanes.Add(lanePool[0]);
            lanePool.RemoveAt(0);
        }

        // 1. Bidones con compensación si llevamos varias oleadas sin gasolina
        float pityBoost = Mathf.Clamp01(wavesSinceLastFuel * 0.14f);
        float fuelChance = Mathf.Clamp01((baseFuelSpawnChance * fuelMult) + pityBoost);

        bool spawnedFuel = false;
        if (fuelDrumPrefab != null && safeLanes.Count > 0 && UnityEngine.Random.value <= fuelChance)
        {
            int chosenSafeLane = safeLanes[0];
            if (safeLanes.Count > 1 && chosenSafeLane == lastFuelLane)
            {
                chosenSafeLane = safeLanes[1];
            }

            float fuelZ = GetStaggeredZ(baseZ);
            float fuelX = laneXPositions[chosenSafeLane];
            bool allowRareOverlap = rareFuelNearObstacleChance > 0f && UnityEngine.Random.value <= rareFuelNearObstacleChance;

            if (!allowRareOverlap && HasNearbyItemInLane(fuelX, fuelZ, minSameLaneGapZ, ignoreFuel: true))
            {
                for (int i = 0; i < safeLanes.Count; i++)
                {
                    int candidateLane = safeLanes[i];
                    float candidateX = laneXPositions[candidateLane];
                    if (!HasNearbyItemInLane(candidateX, fuelZ, minSameLaneGapZ, ignoreFuel: true))
                    {
                        chosenSafeLane = candidateLane;
                        fuelX = candidateX;
                        break;
                    }
                }
            }

            if (allowRareOverlap || !HasNearbyItemInLane(fuelX, fuelZ, minSameLaneGapZ, ignoreFuel: true))
            {
                Vector3 fuelPos = new Vector3(fuelX, fuelHeightY, fuelZ);
                GameObject fuel = Instantiate(fuelDrumPrefab, fuelPos, fuelDrumPrefab.transform.rotation, transform);
                spawnedItems.Add(fuel);

                lastFuelLane = chosenSafeLane;
                wavesSinceLastFuel = 0;
                spawnedFuel = true;
            }
        }

        if (!spawnedFuel)
        {
            wavesSinceLastFuel++;
        }

        // 2. Obstáculos dinámicos
        if (currentWave < warmupWavesWithoutObstacles) return;

        if (obstacles == null || obstacles.Count == 0)
        {
            if (logSpawnDebug) Debug.LogWarning("[TrackItemSpawner] La lista 'Obstacles' está vacía.");
            return;
        }

        float obstacleChance = Mathf.Clamp01(
            Mathf.Lerp(baseObstacleSpawnChance * 0.8f, 1f, progress) * diff
        );

        if (lanePool.Count == 0 || UnityEngine.Random.value > obstacleChance) return;

        int minCount = Mathf.RoundToInt(Mathf.Lerp(startMinObstacles, endMinObstacles, progress));
        int maxCount = Mathf.RoundToInt(Mathf.Lerp(startMaxObstacles, endMaxObstacles, progress));

        if (diff >= 1.35f && progress > 0.45f)
        {
            maxCount = Mathf.Min(3, maxCount + 1);
        }

        maxCount = Mathf.Clamp(maxCount, 1, Mathf.Min(3, lanePool.Count));
        minCount = Mathf.Clamp(minCount, 1, maxCount);

        int countToSpawn = UnityEngine.Random.Range(minCount, maxCount + 1);
        float step = (countToSpawn > 1) ? (zStaggerRange * 2f) / (countToSpawn - 1) : 0f;
        int spawnedObstaclesThisWave = 0;
        int barriersInThisWave = 0;

        for (int i = 0; i < countToSpawn; i++)
        {
            bool allowBlocking = maxBlockingBarriersPerWave <= 0 || barriersInThisWave < maxBlockingBarriersPerWave;
            ObstacleDefinition chosenObstacle = PickWeightedObstacle(progress, allowBlocking);
            if (chosenObstacle == null || chosenObstacle.prefab == null) continue;

            int lane = lanePool[i];
            float laneX = laneXPositions[lane];
            float structuredOffset = (countToSpawn > 1)
                ? (-zStaggerRange + (step * i)) + UnityEngine.Random.Range(-1.1f, 1.1f)
                : UnityEngine.Random.Range(-zStaggerRange, zStaggerRange);

            float itemZ = Mathf.Clamp(baseZ + structuredOffset, startZ + 10f, calculatedMaxSpawnZ);

            if (HasNearbyItemInLane(laneX, itemZ, minObstacleGapZ, ignoreFuel: true))
            {
                if (logSpawnDebug)
                {
                    Debug.Log($"[TrackItemSpawner] Omitido '{chosenObstacle.label}' en carril {lane} (Z={itemZ:F1}) por cercanía con otro obstáculo.");
                }
                continue;
            }

            Vector3 itemPos = new Vector3(laneX, chosenObstacle.heightY, itemZ);
            Quaternion finalRot = chosenObstacle.prefab.transform.rotation * Quaternion.Euler(chosenObstacle.rotationOffsetEuler);
            GameObject obj = Instantiate(chosenObstacle.prefab, itemPos, finalRot, transform);
            spawnedItems.Add(obj);
            spawnedObstaclesThisWave++;

            if (chosenObstacle.isBlockingBarrier)
            {
                barriersInThisWave++;
            }
        }

        if (logSpawnDebug && spawnedObstaclesThisWave == 0)
        {
            Debug.LogWarning($"[TrackItemSpawner] Oleada {currentWave} (progress={progress:F2}) no instanció obstáculos: revisa que 'Obstacles' tenga Prefab asignado y MaxTrackProgress >= {progress:F2}.");
        }
    }

    private ObstacleDefinition PickWeightedObstacle(float progress, bool allowBlocking)
    {
        float totalWeight = 0f;

        for (int i = 0; i < obstacles.Count; i++)
        {
            ObstacleDefinition item = obstacles[i];
            if (item == null || item.prefab == null || item.weight <= 0f) continue;
            if (!allowBlocking && item.isBlockingBarrier) continue;

            float minP = Mathf.Min(item.minTrackProgress, item.maxTrackProgress);
            float maxP = Mathf.Max(item.minTrackProgress, item.maxTrackProgress);

            if (progress >= minP && progress <= maxP)
            {
                totalWeight += item.weight;
            }
        }

        if (totalWeight <= 0f) return null;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < obstacles.Count; i++)
        {
            ObstacleDefinition item = obstacles[i];
            if (item == null || item.prefab == null || item.weight <= 0f) continue;
            if (!allowBlocking && item.isBlockingBarrier) continue;

            float minP = Mathf.Min(item.minTrackProgress, item.maxTrackProgress);
            float maxP = Mathf.Max(item.minTrackProgress, item.maxTrackProgress);

            if (progress >= minP && progress <= maxP)
            {
                cumulative += item.weight;
                if (roll <= cumulative)
                {
                    return item;
                }
            }
        }

        return null;
    }

    private void BuildShuffledLanes()
    {
        lanePool.Clear();
        for (int i = 0; i < laneXPositions.Length; i++)
        {
            lanePool.Add(i);
        }

        for (int i = lanePool.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            int temp = lanePool[i];
            lanePool[i] = lanePool[j];
            lanePool[j] = temp;
        }
    }
}

