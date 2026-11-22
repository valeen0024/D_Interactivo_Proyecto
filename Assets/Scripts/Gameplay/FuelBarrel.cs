using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class FuelBarrel : MonoBehaviour
{
    public enum AudioPlayMode
    {
        Random,
        Sequential
    }

    [Header("Configuración del Barril")]
    [SerializeField] private float fuelAmount = 25f;

    [Header("Audio")]
    [Tooltip("Volumen del efecto de sonido al recoger el barril (0 a 1).")]
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    [Tooltip("Si está activo, reproduce el audio del barril en 2D para que no se escuche suave ni cortado al desactivarse.")]
    [SerializeField] private bool force2DSound = true;

    [Tooltip("Si está activo, también llama a AudioManager.Instance.PlayFuelPickup.")]
    [SerializeField] private bool useAudioManagerPickup = true;

    [Tooltip("AudioSource opcional para reproducir los clips del barril.")]
    [SerializeField] private AudioSource barrelAudioSource;

    [Tooltip("Grupo de mezcla opcional (por ejemplo SFX).")]
    [SerializeField] private AudioMixerGroup mixerGroup;

    [Tooltip("Lista de varios sonidos propios opcionales al recoger el barril.")]
    [SerializeField] private List<AudioClip> pickupClips = new List<AudioClip>();

    [Tooltip("Elige un clip al azar o en orden secuencial.")]
    [SerializeField] private AudioPlayMode audioPlayMode = AudioPlayMode.Random;

    private int nextSequentialIndex;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("CPU"))
        {
            KartFuelSystem fuelSystem = other.GetComponent<KartFuelSystem>();
            if (fuelSystem == null)
            {
                fuelSystem = other.GetComponentInParent<KartFuelSystem>();
            }

            if (fuelSystem != null)
            {
                fuelSystem.AddFuel(fuelAmount);
            }

            PlayBarrelAudio();

            if (useAudioManagerPickup && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayFuelPickup(soundVolume);
            }

            gameObject.SetActive(false);
        }
    }

    private void PlayBarrelAudio()
    {
        AudioClip chosenClip = PickClipFromList();
        if (chosenClip == null) return;

        float vol = Mathf.Clamp01(soundVolume);
        AudioMixerGroup targetMixer = mixerGroup != null
            ? mixerGroup
            : (barrelAudioSource != null ? barrelAudioSource.outputAudioMixerGroup : null);

        // Al desactivar el barril con gameObject.SetActive(false), un AudioSource temporal en 2D evita cortes y atenuación por distancia.
        GameObject tempObj = new GameObject("TempFuelBarrelAudio");
        tempObj.transform.position = transform.position;

        AudioSource tempSource = tempObj.AddComponent<AudioSource>();
        tempSource.clip = chosenClip;
        tempSource.volume = vol;
        tempSource.spatialBlend = force2DSound ? 0f : 1f;
        tempSource.playOnAwake = false;

        if (targetMixer != null)
        {
            tempSource.outputAudioMixerGroup = targetMixer;
        }

        tempSource.Play();
        Destroy(tempObj, Mathf.Max(0.1f, chosenClip.length + 0.15f));
    }

    private AudioClip PickClipFromList()
    {
        if (pickupClips == null || pickupClips.Count == 0) return null;

        if (audioPlayMode == AudioPlayMode.Sequential)
        {
            for (int i = 0; i < pickupClips.Count; i++)
            {
                int idx = (nextSequentialIndex + i) % pickupClips.Count;
                if (pickupClips[idx] != null)
                {
                    AudioClip selected = pickupClips[idx];
                    nextSequentialIndex = (idx + 1) % pickupClips.Count;
                    return selected;
                }
            }
            return null;
        }

        List<AudioClip> validClips = new List<AudioClip>();
        for (int i = 0; i < pickupClips.Count; i++)
        {
            if (pickupClips[i] != null)
            {
                validClips.Add(pickupClips[i]);
            }
        }

        if (validClips.Count == 0) return null;
        return validClips[Random.Range(0, validClips.Count)];
    }
}