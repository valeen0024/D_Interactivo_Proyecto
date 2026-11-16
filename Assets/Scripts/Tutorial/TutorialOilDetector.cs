
using UnityEngine;

public class TutorialOilDetector : MonoBehaviour
{
    private bool playerTouchedOil = false;

    public bool PlayerTouchedOil
    {
        get { return playerTouchedOil; }
    }

    public void ResetDetection()
    {
        playerTouchedOil = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        KartLaneController player =
            other.GetComponentInParent<KartLaneController>();

        if (player != null && player.CompareTag("Player"))
        {
            playerTouchedOil = true;

            Debug.Log("TUTORIAL: Player touched the oil!");
        }
    }
}
