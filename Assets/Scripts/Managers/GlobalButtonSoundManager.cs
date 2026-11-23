using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GlobalButtonSoundManager : MonoBehaviour
{
    public static GlobalButtonSoundManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [Tooltip("Arrastra aquí tu grupo SFX del AudioMixer para respetar el volumen de efectos.")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Button Sound Clips")]
    [Tooltip("Lista de sonidos distintos que sonarán aleatoriamente al presionar cualquier botón.")]
    [SerializeField] private List<AudioClip> buttonClickClips = new List<AudioClip>();

    [Header("Playback Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.85f;

    [Tooltip("Pequeña variación de tono para que suene aún más orgánico (0 = sin variación).")]
    [Range(0f, 0.25f)]
    [SerializeField] private float randomPitchRange = 0.06f;

    [Tooltip("Si está activo, evita repetir exactamente el mismo clip dos veces seguidas.")]
    [SerializeField] private bool avoidImmediateRepeat = true;

    private AudioSource audioSource;
    private int lastPlayedIndex = -1;
    private readonly HashSet<int> registeredButtonIds = new HashSet<int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.outputAudioMixerGroup = sfxMixerGroup;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        RegisterAllButtonsInScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        registeredButtonIds.Clear();
        RegisterAllButtonsInScene();
    }

    /// <summary>
    /// Busca todos los botones de la escena actual (incluso los que están en paneles ocultos)
    /// y les asigna el sonido al hacer clic.
    /// </summary>
    public void RegisterAllButtonsInScene()
    {
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Button btn in allButtons)
        {
            RegisterButton(btn);
        }
    }

    public void RegisterButton(Button btn)
    {
        if (btn == null)
            return;

        int id = btn.GetInstanceID();
        if (registeredButtonIds.Contains(id))
            return;

        registeredButtonIds.Add(id);
        btn.onClick.AddListener(PlayRandomButtonSound);
    }

    public void PlayRandomButtonSound()
    {
        if (audioSource == null || buttonClickClips == null || buttonClickClips.Count == 0)
            return;

        int index = GetNextClipIndex();
        if (index < 0 || index >= buttonClickClips.Count)
            return;

        AudioClip clip = buttonClickClips[index];
        if (clip == null)
            return;

        lastPlayedIndex = index;
        audioSource.outputAudioMixerGroup = sfxMixerGroup;
        audioSource.pitch = 1f + Random.Range(-randomPitchRange, randomPitchRange);
        audioSource.PlayOneShot(clip, volume);
    }

    private int GetNextClipIndex()
    {
        int count = buttonClickClips.Count;
        if (count == 1)
            return 0;

        int index = Random.Range(0, count);
        if (avoidImmediateRepeat && index == lastPlayedIndex)
        {
            index = (index + 1 + Random.Range(0, count - 1)) % count;
        }

        return index;
    }
}