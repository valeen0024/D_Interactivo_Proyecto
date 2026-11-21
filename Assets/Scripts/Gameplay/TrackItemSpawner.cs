using System.Collections.Generic;
using UnityEngine;

public class TrackItemSpawner : MonoBehaviour
{
    [Header("Prefabs (Arrastrar desde Assets/Prefabs)")]
    [SerializeField] private GameObject fuelDrumPrefab;
    [SerializeField] private GameObject oilSpillPrefab;

    [Header("Configuración de los 4 Carriles (Coordenadas X)")]
    [SerializeField] private float[] laneXPositions = new float[] { -104.9f, -102.9f, -100.9f, -98.9f };

    [Header("Alturas Y de aparición")]
    [SerializeField] private float fuelHeightY = 463.896f;
    [SerializeField] private float oilHeightY = 463.7f;

    [Header("Parámetros de Generación en Pista (800 Yardas)")]
    [Tooltip("Distancia por delante del kart donde van apareciendo los objetos.")]
    [SerializeField] private float spawnAheadDistance = 40f;

    [Tooltip("Separación base en Z entre cada oleada.")]
    [SerializeField] private float spawnIntervalZ = 10.5f;

    [Tooltip("Variación máxima en Z (+/-) para que los objetos salgan escalonados.")]
    [SerializeField] private float zStaggerRange = 3.8f;

    [Tooltip("Metros antes de la meta en los que se dejan de generar ítems.")]
    [SerializeField] private float safetyMarginBeforeFinish = 15f;

    [Tooltip("Si no encuentra el arco de meta, usará este límite Z por defecto.")]
    [SerializeField] private float fallbackMaxSpawnZ = 195f;

    [Header("Equilibrio de Bidones y Aceite")]
    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que aparezca un bidón en la oleada.")]
    [SerializeField] private float fuelSpawnChance = 0.68f;

    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que aparezca aceite en la oleada.")]
    [SerializeField] private float oilSpawnChance = 0.85f;

    [Header("Variedad por Tramo (Inicio vs. Final de Carrera)")]
    [Range(1, 3)]
    [Tooltip("Mínimo de manchas de aceite en la primera mitad de la pista.")]
    [SerializeField] private int earlyMinOilPerRow = 1;

    [Range(1, 3)]
    [Tooltip("Máximo de manchas de aceite en la primera mitad de la pista.")]
    [SerializeField] private int earlyMaxOilPerRow = 2;

    [Range(1, 3)]
    [Tooltip("Mínimo de manchas de aceite en la segunda mitad de la pista.")]
    [SerializeField] private int lateMinOilPerRow = 1;

    [Range(1, 3)]
    [Tooltip("Máximo de manchas de aceite en la segunda mitad de la pista.")]
    [SerializeField] private int lateMaxOilPerRow = 3;

    [Range(1, 2)]
    [Tooltip("Carriles garantizados libres de aceite por oleada.")]
    [SerializeField] private int guaranteedFreeLanes = 1;

    private Transform playerTransform;
    private Transform cpuTransform;
    private float startZ;
    private float nextSpawnZ;
    private float calculatedMaxSpawnZ;
    private bool isInitialized = false;
    private GameManager.GameState lastKnownState = GameManager.GameState.Waiting;

    private readonly List<GameObject> spawnedItems = new List<GameObject>();

    private void Start()
    {
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
        else
        {
            calculatedMaxSpawnZ = fallbackMaxSpawnZ;
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

        nextSpawnZ = startZ + 12f;
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

        if (currentState == GameManager.GameState.Waiting ||
            currentState == GameManager.GameState.Finished)
        {
            return;
        }

        float activeZ = GetActiveKartZ(currentState);

        while (activeZ + spawnAheadDistance >= nextSpawnZ && nextSpawnZ <= calculatedMaxSpawnZ)
        {
            SpawnWaveAt(nextSpawnZ);
            nextSpawnZ += spawnIntervalZ;
        }
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

    private float GetStaggeredZ(float baseZ)
    {
        float randomOffset = Random.Range(-zStaggerRange, zStaggerRange);
        return Mathf.Clamp(baseZ + randomOffset, startZ + 10f, calculatedMaxSpawnZ);
    }

    private void SpawnWaveAt(float baseZ)
    {
        if (laneXPositions == null || laneXPositions.Length == 0) return;

        List<int> availableLanes = new List<int>();
        for (int i = 0; i < laneXPositions.Length; i++)
        {
            availableLanes.Add(i);
        }

        int safeLanesCount = Mathf.Clamp(guaranteedFreeLanes, 1, laneXPositions.Length - 1);
        List<int> safeLanes = new List<int>();

        for (int i = 0; i < safeLanesCount; i++)
        {
            int idx = Random.Range(0, availableLanes.Count);
            safeLanes.Add(availableLanes[idx]);
            availableLanes.RemoveAt(idx);
        }

        // 1. Bidón de gasolina (priorizando un carril libre de aceite en esa oleada)
        if (fuelDrumPrefab != null && Random.value <= fuelSpawnChance && safeLanes.Count > 0)
        {
            int chosenSafeLane = safeLanes[Random.Range(0, safeLanes.Count)];
            float staggeredZ = GetStaggeredZ(baseZ);
            Vector3 fuelPos = new Vector3(laneXPositions[chosenSafeLane], fuelHeightY, staggeredZ);
            GameObject fuel = Instantiate(fuelDrumPrefab, fuelPos, fuelDrumPrefab.transform.rotation, transform);
            spawnedItems.Add(fuel);
        }

        // 2. Aceite con variedad progresiva según qué tan avanzado está el tramo
        if (oilSpillPrefab != null && Random.value <= oilSpawnChance && availableLanes.Count > 0)
        {
            float trackProgress = Mathf.InverseLerp(startZ, calculatedMaxSpawnZ, baseZ);

            int minOil = (trackProgress < 0.5f) ? earlyMinOilPerRow : lateMinOilPerRow;
            int maxOil = (trackProgress < 0.5f) ? earlyMaxOilPerRow : lateMaxOilPerRow;

            minOil = Mathf.Clamp(minOil, 1, maxOil);
            int oilCount = Random.Range(minOil, maxOil + 1);
            oilCount = Mathf.Min(oilCount, availableLanes.Count);

            float step = (oilCount > 1) ? (zStaggerRange * 2f) / (oilCount - 1) : 0f;

            for (int i = 0; i < oilCount; i++)
            {
                int randomIndex = Random.Range(0, availableLanes.Count);
                int chosenLane = availableLanes[randomIndex];
                availableLanes.RemoveAt(randomIndex);

                float structuredOffset = (oilCount > 1)
                    ? (-zStaggerRange + (step * i)) + Random.Range(-0.7f, 0.7f)
                    : Random.Range(-zStaggerRange, zStaggerRange);

                float oilZ = Mathf.Clamp(baseZ + structuredOffset, startZ + 10f, calculatedMaxSpawnZ);
                Vector3 oilPos = new Vector3(laneXPositions[chosenLane], oilHeightY, oilZ);
                GameObject oil = Instantiate(oilSpillPrefab, oilPos, oilSpillPrefab.transform.rotation, transform);
                spawnedItems.Add(oil);
            }
        }
    }
}