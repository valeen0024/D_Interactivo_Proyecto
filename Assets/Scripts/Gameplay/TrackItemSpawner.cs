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

    [Header("Parámetros de Generación en Pista")]
    [Tooltip("Distancia por delante del jugador donde van apareciendo los objetos.")]
    [SerializeField] private float spawnAheadDistance = 30f;

    [Tooltip("Separación base en Z entre cada oleada.")]
    [SerializeField] private float spawnIntervalZ = 9f;

    [Tooltip("Variación máxima en Z (+/-) para que los objetos salgan escalonados y no en línea recta.")]
    [SerializeField] private float zStaggerRange = 3.2f;

    [Tooltip("Posición Z máxima hasta donde se pueden generar objetos (antes de la meta).")]
    [SerializeField] private float maxSpawnZ = 712f;

    [Header("Dificultad y Probabilidades")]
    [Range(0f, 1f)]
    [SerializeField] private float fuelSpawnChance = 0.55f;

    [Range(0f, 1f)]
    [SerializeField] private float oilSpawnChance = 0.90f;

    [Tooltip("Mínimo de manchas de aceite por oleada.")]
    [Range(1, 3)]
    [SerializeField] private int minOilPerRow = 1;

    [Tooltip("Máximo de manchas de aceite por oleada (nunca 4 para que siempre haya paso).")]
    [Range(1, 3)]
    [SerializeField] private int maxOilPerRow = 3;

    [Tooltip("Cantidad mínima de carriles libres garantizados en cada oleada.")]
    [Range(1, 2)]
    [SerializeField] private int guaranteedFreeLanes = 1;

    private Transform playerTransform;
    private Transform cpuTransform;
    private float startZ;
    private float nextSpawnZ;
    private bool isInitialized = false;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        GameObject cpu = GameObject.FindGameObjectWithTag("CPU");
        if (cpu != null) cpuTransform = cpu.transform;

        startZ = GetLeadingKartZ();
        nextSpawnZ = startZ + 10f;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized || GameManager.Instance == null) return;

        if (GameManager.Instance.gameState == GameManager.GameState.Waiting ||
            GameManager.Instance.gameState == GameManager.GameState.Finished)
        {
            return;
        }

        float leadingZ = GetLeadingKartZ();

        while (leadingZ + spawnAheadDistance >= nextSpawnZ && nextSpawnZ <= maxSpawnZ)
        {
            SpawnWaveAt(nextSpawnZ);
            nextSpawnZ += spawnIntervalZ;
        }
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
        // Desplaza cada ítem hacia adelante o atrás dentro de la oleada sin pasar la meta
        float randomOffset = Random.Range(-zStaggerRange, zStaggerRange);
        return Mathf.Clamp(baseZ + randomOffset, startZ + 8f, maxSpawnZ);
    }

    private void SpawnWaveAt(float baseZ)
    {
        if (laneXPositions == null || laneXPositions.Length == 0) return;

        List<int> availableLanes = new List<int>();
        for (int i = 0; i < laneXPositions.Length; i++)
        {
            availableLanes.Add(i);
        }

        // Reserva carriles libres de aceite para asegurar siempre un camino pasable
        int safeLanesCount = Mathf.Clamp(guaranteedFreeLanes, 1, laneXPositions.Length - 1);
        List<int> safeLanes = new List<int>();

        for (int i = 0; i < safeLanesCount; i++)
        {
            int idx = Random.Range(0, availableLanes.Count);
            safeLanes.Add(availableLanes[idx]);
            availableLanes.RemoveAt(idx);
        }

        // 1. Bidón de gasolina en un carril seguro, con su propia altura Z escalonada
        if (fuelDrumPrefab != null && Random.value <= fuelSpawnChance && safeLanes.Count > 0)
        {
            int chosenSafeLane = safeLanes[Random.Range(0, safeLanes.Count)];
            float staggeredZ = GetStaggeredZ(baseZ);
            Vector3 fuelPos = new Vector3(laneXPositions[chosenSafeLane], fuelHeightY, staggeredZ);
            Instantiate(fuelDrumPrefab, fuelPos, fuelDrumPrefab.transform.rotation, transform);
        }

        // 2. Manchas de aceite en los carriles restantes, cada una en una posición Z distinta (escalonadas)
        if (oilSpillPrefab != null && Random.value <= oilSpawnChance && availableLanes.Count > 0)
        {
            float trackProgress = Mathf.InverseLerp(startZ, maxSpawnZ, baseZ);
            int dynamicMin = (trackProgress > 0.5f) ? Mathf.Min(2, maxOilPerRow) : minOilPerRow;
            int oilCount = Random.Range(dynamicMin, maxOilPerRow + 1);
            oilCount = Mathf.Min(oilCount, availableLanes.Count);

            // Reparte desplazamientos distintos para que las manchas de la misma oleada no queden en línea
            float step = (oilCount > 1) ? (zStaggerRange * 2f) / (oilCount - 1) : 0f;

            for (int i = 0; i < oilCount; i++)
            {
                int randomIndex = Random.Range(0, availableLanes.Count);
                int chosenLane = availableLanes[randomIndex];
                availableLanes.RemoveAt(randomIndex);

                float structuredOffset = (oilCount > 1)
                    ? (-zStaggerRange + (step * i)) + Random.Range(-0.6f, 0.6f)
                    : Random.Range(-zStaggerRange, zStaggerRange);

                float oilZ = Mathf.Clamp(baseZ + structuredOffset, startZ + 8f, maxSpawnZ);
                Vector3 oilPos = new Vector3(laneXPositions[chosenLane], oilHeightY, oilZ);
                Instantiate(oilSpillPrefab, oilPos, oilSpillPrefab.transform.rotation, transform);
            }
        }
    }
}