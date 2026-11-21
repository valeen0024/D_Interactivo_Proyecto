using UnityEngine;

public class DestroyAfterDistance : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Distance behind the kart at which the object will be destroyed.")]
    [SerializeField] private float destroyDistanceBehind = 15f;

    private Transform playerTransform;
    private Transform cpuTransform;

    private void Start()
    {
        // Busca una sola vez las referencias al Jugador y a la CPU por su Tag
        GameObject playerKart = GameObject.FindGameObjectWithTag("Player");
        if (playerKart != null)
        {
            playerTransform = playerKart.transform;
        }

        GameObject cpuKart = GameObject.FindGameObjectWithTag("CPU");
        if (cpuKart != null)
        {
            cpuTransform = cpuKart.transform;
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null)
            return;

        float referenceZ = float.MaxValue;
        bool hasActiveKart = false;

        // Evalúa la posición del Jugador si está activo en la escena
        if (playerTransform != null && playerTransform.gameObject.activeInHierarchy)
        {
            referenceZ = Mathf.Min(referenceZ, playerTransform.position.z);
            hasActiveKart = true;
        }

        // Evalúa la posición de la CPU si está activa en la escena
        if (cpuTransform != null && cpuTransform.gameObject.activeInHierarchy)
        {
            referenceZ = Mathf.Min(referenceZ, cpuTransform.position.z);
            hasActiveKart = true;
        }

        // Si el kart que va más atrás ya superó el objeto por la distancia indicada, se destruye
        if (hasActiveKart && (referenceZ - transform.position.z > destroyDistanceBehind))
        {
            Destroy(gameObject);
        }
    }
}