using UnityEngine;

public class DistanceTracker : MonoBehaviour
{
    private Transform startLine;
    public bool isPlayer;
    public float currentDistance;
    public float maxDistance = 800f;
    public float distanceMultiplier = 3.4f;
    private bool hasFinished = false;

    private void Start()
    {
        hasFinished = false;

        GameObject startObject = GameObject.FindGameObjectWithTag("Start");
        if (startObject != null)
        {
            startLine = startObject.transform;
        }
        else
        {
            Debug.LogError("No object with the tag 'Start' was found.");
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        bool isMyTurn =
            (isPlayer && GameManager.Instance.gameState == GameManager.GameState.PlayerTurn)
            ||
            (!isPlayer && GameManager.Instance.gameState == GameManager.GameState.CPUTurn);

        if (isMyTurn && !hasFinished)
        {
            UpdateDistance();
            CheckFinish();
        }
    }

    public void UpdateDistance()
    {
        if (startLine == null) return;

        currentDistance = Mathf.Abs(transform.position.z - startLine.position.z) * distanceMultiplier;
        currentDistance = Mathf.Min(currentDistance, maxDistance);

        if (isPlayer)
        {
            GameManager.Instance.finalPlayerDistance = currentDistance;
        }
        else
        {
            GameManager.Instance.finalCPUDistance = currentDistance;
        }
    }

    public void CheckFinish()
    {
        // Diagnóstico en consola para ver el valor real de maxDistance y qué dispara el freno
        Debug.Log($"[DistanceTracker] {gameObject.name} -> posZ: {transform.position.z:F2} | currentDistance: {currentDistance:F1} | maxDistance: {maxDistance} | state: {GameManager.Instance.gameState}");

        if (currentDistance >= maxDistance)
        {
            hasFinished = true;
            currentDistance = maxDistance;

            Debug.LogWarning($"[DistanceTracker] {gameObject.name} alcanzó maxDistance ({maxDistance}) y detendrá el turno.");

            if (isPlayer)
            {
                GameManager.Instance.finalPlayerDistance = currentDistance;

                if (TryGetComponent<KartLaneController>(out var laneController))
                {
                    laneController.canMove = false;
                }

                if (TryGetComponent<KartMovement>(out var kartMovement))
                {
                    kartMovement.enabled = false;

                    if (laneController != null)
                    {
                        laneController.StopEngineSound();
                    }
                }

                StopRigidbody();
                GameManager.Instance.EndPlayerTurn();
            }
            else
            {
                GameManager.Instance.finalCPUDistance = currentDistance;

                if (TryGetComponent<CPUController>(out var cpuController))
                {
                    cpuController.canMove = false;
                    cpuController.StopEngineSound();
                }

                StopRigidbody();
                GameManager.Instance.EndCPUTurn();
            }
        }
    }

    private void StopRigidbody()
    {
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
        }
    }
}