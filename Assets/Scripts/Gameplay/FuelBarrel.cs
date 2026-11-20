using UnityEngine;

public class FuelBarrel : MonoBehaviour
{
    [Header("Configuración del Barril")]
    [SerializeField] private float fuelAmount = 25f;

    [Header("Audio")]
    [Tooltip("Volumen del efecto de sonido al recoger el barril.")]
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("CPU"))
        {
            if (other.TryGetComponent<KartFuelSystem>(out KartFuelSystem fuelSystem))
            {
                fuelSystem.AddFuel(fuelAmount);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayFuelPickup(soundVolume);
            }

            gameObject.SetActive(false);
        }
    }
}