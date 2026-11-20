using UnityEngine;

public class OilSlick : MonoBehaviour
{
    [Header("Configuración del Aceite")]
    [Tooltip("Duración en segundos del efecto de desaceleración")]
    public float slowDuration = 2f;

    [Header("Audio")]
    [Tooltip("Volumen del efecto de sonido al pisar el aceite.")]
    [Range(0f, 1f)]
    [SerializeField] private float slipVolume = 1f;

    private void OnTriggerEnter(Collider other)
    {
        // Imprime en consola para confirmar de inmediato que la colisión física ocurrió
        Debug.Log("¡Colisión detectada con el aceite por: " + other.gameObject.name);

        // Busca el controlador de carril en el objeto detectado o en cualquiera de sus padres
        KartLaneController kart = other.GetComponentInParent<KartLaneController>();

        if (kart != null)
        {
            kart.ApplyOilSlow(slowDuration);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayOilSlip(slipVolume);
            }

            Debug.Log("¡Efecto de aceite aplicado correctamente!");
        }
        else
        {
            Debug.LogWarning("Se detectó colisión, pero no se encontró 'KartLaneController' en " + other.gameObject.name + " ni en sus padres.");
        }
    }
}