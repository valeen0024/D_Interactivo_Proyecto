using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleBehavior : MonoBehaviour
{
    public enum ObstacleKind
    {
        AutoFromTag,
        Unknown,
        Oil,
        Cone,
        Barrier
    }

    public enum AudioPlayMode
    {
        Random,
        Sequential
    }

    [Serializable]
    public class ObstacleAudioSection
    {
        [Tooltip("AudioSource específico para este obstáculo (opcional; si queda vacío usa el de respaldo o un reproductor 2D temporal).")]
        public AudioSource audioSource;

        [Tooltip("Lista de varios audios posibles al chocar o pisar este obstáculo.")]
        public List<AudioClip> clips = new List<AudioClip>();

        [Tooltip("Elige un clip al azar o en orden secuencial.")]
        public AudioPlayMode playMode = AudioPlayMode.Random;

        [Range(0f, 1f)]
        [Tooltip("Volumen de reproducción para este obstáculo (0 a 1).")]
        public float volume = 1f;

        [Tooltip("Si está activo, reproduce el sonido en 2D (sin atenuación por distancia ni 3D) para que se escuche claro y fuerte.")]
        public bool force2DSound = true;

        [HideInInspector]
        public int nextSequentialIndex;
    }

    [Serializable]
    public class OilSectionConfig
    {
        [Header("Comportamiento y Combustible (Máx. tanque: 100)")]
        [Tooltip("Duración en segundos del resbalón/desaceleración al pisar el aceite.")]
        public float slowDuration = 2.0f;

        [Min(0f)]
        [Tooltip("Cantidad de combustible que RESTA al pisar aceite. Si es 0, usa ReduceFuelFromOil() de KartFuelSystem.")]
        public float fuelLossOverride = 0f;

        [Tooltip("Oculta o desactiva el aceite tras pisarlo.")]
        public bool hideAfterHit = false;

        [Tooltip("Segundos de espera antes de ocultar/desactivar el aceite.")]
        public float hideDelay = 0.25f;

        [Header("Audios del Aceite (Varios)")]
        [Tooltip("Si está activo, también llama a AudioManager.Instance.PlayOilSlip.")]
        public bool useAudioManagerSlip = true;

        public ObstacleAudioSection audio = new ObstacleAudioSection();
    }

    [Serializable]
    public class ConeSectionConfig
    {
        [Header("Comportamiento y Combustible (Máx. tanque: 100)")]
        [Tooltip("Duración en segundos del freno temporal al tocar un cono.")]
        public float slowDuration = 1.6f;

        [Range(0f, 1f)]
        [Tooltip("Multiplicador de velocidad mientras dura el freno del cono.")]
        public float speedMultiplier = 0.45f;

        [Min(0f)]
        [Tooltip("Cantidad de combustible que RESTA al chocar contra conos (0 = no resta).")]
        public float fuelLoss = 0f;

        [Header("Rotura Natural del Cono")]
        [Tooltip("Si está activo, el cono se rompe y se destruye al impactar en vez de quedarse intacto.")]
        public bool breakAndDestroyOnHit = true;

        [Tooltip("Partículas o efecto visual opcional al romperse el cono.")]
        public GameObject breakEffectPrefab;

        [Tooltip("Objeto visual opcional de cono roto/aplastado dentro del prefab.")]
        public GameObject brokenVisualObject;

        [Tooltip("Segundos de espera antes de destruir el cono tras romperse (da margen al audio).")]
        public float destroyDelay = 0.6f;

        [Tooltip("Oculta o desactiva los conos tras chocar si Break And Destroy On Hit está desactivado.")]
        public bool hideAfterHit = false;

        [Tooltip("Segundos de espera antes de ocultar/desactivar los conos.")]
        public float hideDelay = 0.15f;

        [Header("Audios de los Conos (Varios)")]
        [Tooltip("Si está activo, también llama a AudioManager.Instance.PlayOilSlip.")]
        public bool useAudioManagerSlip = false;

        public ObstacleAudioSection audio = new ObstacleAudioSection { volume = 1f, force2DSound = true };
    }

    [Serializable]
    public class BarrierSectionConfig
    {
        [Header("Comportamiento y Combustible (Máx. tanque: 100)")]
        [Tooltip("Duración de referencia del bloqueo en el carril sin bloquear el cambio lateral de carril.")]
        public float blockDuration = 0.9f;

        [Range(0f, 0.5f)]
        [Tooltip("Multiplicador de referencia al chocar de frente contra la valla.")]
        public float blockSpeedMultiplier = 0.05f;

        [Min(0f)]
        [Tooltip("Cantidad de combustible que RESTA al chocar contra la valla (0 = no resta).")]
        public float fuelPenalty = 0f;

        [Header("Audios de la Valla STOP (Varios)")]
        public ObstacleAudioSection audio = new ObstacleAudioSection { volume = 1f, force2DSound = true };
    }

    [Header("Identificación General")]
    [Tooltip("AutoFromTag detecta 'Oil', 'Cone' o 'Barrier' desde el Tag del objeto o de su raíz.")]
    [SerializeField] private ObstacleKind obstacleKind = ObstacleKind.AutoFromTag;

    [Tooltip("Aplica efectos de velocidad y combustible. Desactívalo si solo quieres manejar audio/ocultado aquí.")]
    [SerializeField] private bool applyGameplayEffects = true;

    [Tooltip("Evita que el mismo obstáculo dispare más de una vez en el mismo choque.")]
    [SerializeField] private bool triggerOnlyOnce = true;

    [Tooltip("AudioSource de respaldo si un obstáculo no tiene uno asignado en su sección.")]
    [SerializeField] private AudioSource fallbackAudioSource;

    [Header("1. Aceite (Oil) - Configuración y Audios")]
    [SerializeField] private OilSectionConfig oilConfig = new OilSectionConfig();

    [Header("2. Conos (Cone) - Configuración y Audios")]
    [SerializeField] private ConeSectionConfig coneConfig = new ConeSectionConfig();

    [Header("3. Valla STOP (Barrier) - Configuración y Audios")]
    [SerializeField] private BarrierSectionConfig barrierConfig = new BarrierSectionConfig();

    private bool hasTriggered;

    private void OnEnable()
    {
        hasTriggered = false;
    }

    private ObstacleKind ResolveKind()
    {
        if (obstacleKind != ObstacleKind.AutoFromTag)
        {
            return obstacleKind;
        }

        string tagToCheck = gameObject.tag;

        if (tagToCheck == "Untagged" && transform.root != null)
        {
            tagToCheck = transform.root.gameObject.tag;
        }

        switch (tagToCheck)
        {
            case "Oil":
                return ObstacleKind.Oil;

            case "Cone":
                return ObstacleKind.Cone;

            case "Barrier":
                return ObstacleKind.Barrier;

            default:
                return ObstacleKind.Unknown;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ObstacleKind kind = ResolveKind();
        HandleContact(other.gameObject, kind);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ObstacleKind kind = ResolveKind();
        HandleContact(collision.gameObject, kind);
    }

    private void HandleContact(GameObject hitObject, ObstacleKind kind)
    {
        if (kind == ObstacleKind.Unknown ||
            (triggerOnlyOnce && hasTriggered))
        {
            return;
        }

        Transform hitRoot = hitObject.transform.root;

        KartLaneController kart =
            hitObject.GetComponentInParent<KartLaneController>();
        if (kart == null && hitRoot != null)
        {
            kart = hitRoot.GetComponentInChildren<KartLaneController>();
        }

        KartFuelSystem fuelSystem =
            hitObject.GetComponentInParent<KartFuelSystem>();
        if (fuelSystem == null && hitRoot != null)
        {
            fuelSystem = hitRoot.GetComponentInChildren<KartFuelSystem>();
        }

        bool isKart =
            hitObject.CompareTag("Player") ||
            hitObject.CompareTag("CPU") ||
            (hitRoot != null && (hitRoot.CompareTag("Player") || hitRoot.CompareTag("CPU")));

        if (!isKart && kart == null && fuelSystem == null)
        {
            return;
        }

        if (triggerOnlyOnce)
        {
            hasTriggered = true;
        }

        switch (kind)
        {
            case ObstacleKind.Oil:
                PlaySectionAudio(oilConfig.audio, willRemoveOrDisable: oilConfig.hideAfterHit);

                if (oilConfig.useAudioManagerSlip && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayOilSlip(oilConfig.audio.volume);
                }

                if (applyGameplayEffects)
                {
                    if (kart != null)
                    {
                        kart.ApplyOilSlow(oilConfig.slowDuration);
                    }

                    if (fuelSystem != null)
                    {
                        float overrideLoss = Mathf.Abs(oilConfig.fuelLossOverride);
                        if (overrideLoss > 0f)
                        {
                            fuelSystem.ConsumeFuel(overrideLoss);
                        }
                        else
                        {
                            fuelSystem.ReduceFuelFromOil();
                        }
                    }
                }

                if (oilConfig.hideAfterHit)
                {
                    StartCoroutine(HideOrDisableRoutine(oilConfig.hideDelay));
                }
                break;

            case ObstacleKind.Cone:
                bool coneWillVanish = coneConfig.breakAndDestroyOnHit || coneConfig.hideAfterHit;
                PlaySectionAudio(coneConfig.audio, willRemoveOrDisable: coneWillVanish);

                if (coneConfig.useAudioManagerSlip && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayOilSlip(coneConfig.audio.volume);
                }

                if (applyGameplayEffects)
                {
                    if (kart != null)
                    {
                        kart.ApplyObstacleSlow(coneConfig.slowDuration, coneConfig.speedMultiplier);
                    }

                    float coneLoss = Mathf.Abs(coneConfig.fuelLoss);
                    if (fuelSystem != null && coneLoss > 0f)
                    {
                        fuelSystem.ConsumeFuel(coneLoss);
                    }
                }

                if (coneConfig.breakAndDestroyOnHit)
                {
                    StartCoroutine(BreakAndDestroyConeRoutine(coneConfig));
                }
                else if (coneConfig.hideAfterHit)
                {
                    StartCoroutine(HideOrDisableRoutine(coneConfig.hideDelay));
                }
                break;

            case ObstacleKind.Barrier:
                PlaySectionAudio(barrierConfig.audio, willRemoveOrDisable: false);

                if (applyGameplayEffects)
                {
                    if (kart != null)
                    {
                        kart.ApplyBarrierBlock(barrierConfig.blockDuration, barrierConfig.blockSpeedMultiplier);
                    }

                    float barrierLoss = Mathf.Abs(barrierConfig.fuelPenalty);
                    if (fuelSystem != null && barrierLoss > 0f)
                    {
                        fuelSystem.ConsumeFuel(barrierLoss);
                    }
                }
                break;
        }
    }

    private void PlaySectionAudio(ObstacleAudioSection section, bool willRemoveOrDisable)
    {
        if (section == null || section.clips == null || section.clips.Count == 0)
        {
            return;
        }

        AudioClip chosenClip = null;

        if (section.playMode == AudioPlayMode.Sequential)
        {
            for (int attempt = 0; attempt < section.clips.Count; attempt++)
            {
                int idx = (section.nextSequentialIndex + attempt) % section.clips.Count;
                if (section.clips[idx] != null)
                {
                    chosenClip = section.clips[idx];
                    section.nextSequentialIndex = (idx + 1) % section.clips.Count;
                    break;
                }
            }
        }
        else
        {
            List<AudioClip> validClips = new List<AudioClip>();
            for (int i = 0; i < section.clips.Count; i++)
            {
                if (section.clips[i] != null)
                {
                    validClips.Add(section.clips[i]);
                }
            }

            if (validClips.Count > 0)
            {
                chosenClip = validClips[UnityEngine.Random.Range(0, validClips.Count)];
            }
        }

        if (chosenClip == null)
        {
            return;
        }

        float vol = Mathf.Clamp01(section.volume);
        AudioSource targetSource = section.audioSource != null ? section.audioSource : fallbackAudioSource;

        if (willRemoveOrDisable || targetSource == null || !gameObject.activeInHierarchy)
        {
            PlayClipOnTempSource(
                chosenClip,
                vol,
                section.force2DSound,
                targetSource != null ? targetSource.outputAudioMixerGroup : null
            );
            return;
        }

        if (section.force2DSound)
        {
            targetSource.spatialBlend = 0f;
        }

        targetSource.PlayOneShot(chosenClip, vol);
    }

    private void PlayClipOnTempSource(
        AudioClip clip,
        float volume,
        bool force2D,
        UnityEngine.Audio.AudioMixerGroup mixerGroup)
    {
        if (clip == null) return;

        GameObject tempObj = new GameObject("TempObstacleAudio");
        tempObj.transform.position = transform.position;

        AudioSource tempSource = tempObj.AddComponent<AudioSource>();
        tempSource.clip = clip;
        tempSource.volume = Mathf.Clamp01(volume);
        tempSource.spatialBlend = force2D ? 0f : 1f;
        tempSource.playOnAwake = false;

        if (mixerGroup != null)
        {
            tempSource.outputAudioMixerGroup = mixerGroup;
        }

        tempSource.Play();
        Destroy(tempObj, Mathf.Max(0.1f, clip.length + 0.15f));
    }

    private IEnumerator BreakAndDestroyConeRoutine(ConeSectionConfig config)
    {
        if (config.breakEffectPrefab != null)
        {
            Instantiate(config.breakEffectPrefab, transform.position, Quaternion.identity);
        }

        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        if (config.brokenVisualObject != null)
        {
            foreach (Renderer rend in GetComponentsInChildren<Renderer>())
            {
                rend.enabled = false;
            }

            config.brokenVisualObject.SetActive(true);
            foreach (Renderer brokenRend in config.brokenVisualObject.GetComponentsInChildren<Renderer>())
            {
                brokenRend.enabled = true;
            }
        }
        else
        {
            foreach (Renderer rend in GetComponentsInChildren<Renderer>())
            {
                rend.enabled = false;
            }
        }

        if (config.destroyDelay > 0f)
        {
            yield return new WaitForSeconds(config.destroyDelay);
        }

        Destroy(gameObject);
    }

    private IEnumerator HideOrDisableRoutine(float delay)
    {
        if (ResolveKind() == ObstacleKind.Barrier)
        {
            yield break;
        }

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            rend.enabled = false;
        }

        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        gameObject.SetActive(false);
    }
}