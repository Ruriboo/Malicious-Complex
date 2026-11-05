using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer Único")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("Grupos del Mixer")]
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup ambienteGroup;
    [SerializeField] private AudioMixerGroup vozGroup;

    [Header("Snapshots (Bloque B)")]
    [SerializeField] private AudioMixerSnapshot snapshotNormal;
    [SerializeField] private AudioMixerSnapshot snapshotBajoElAgua;
    [SerializeField] private AudioMixerSnapshot snapshotVictoria;
    [SerializeField] private AudioMixerSnapshot snapshotDerrota;

    [Header("Parámetros para Ducking (Bloque F)")]
    [SerializeField] private string musicVolumeParam = "MusicVolume";
    [SerializeField] private float musicVolumeNormal = -12f;
    [SerializeField] private float musicVolumeDuck = -24f;
    [SerializeField] private float duckFadeTime = 0.4f;

    [Header("Fuentes de Música")]
    [SerializeField] private AudioSource menuMusicSource;
    [SerializeField] private AudioSource gameMusicSource;

    private int vocesSonando;
    private Coroutine duckRoutine;

    public AudioMixerGroup Music => musicGroup;
    public AudioMixerGroup SFX => sfxGroup;
    public AudioMixerGroup Ambiente => ambienteGroup;
    public AudioMixerGroup Voz => vozGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        PlayMenuMusic();
    }

    // --- CONTROL DE MÚSICA ---
    public void PlayMenuMusic()
    {
        if (gameMusicSource != null) gameMusicSource.Stop();
        if (menuMusicSource != null && !menuMusicSource.isPlaying) menuMusicSource.Play();
    }

    public void PlayGameMusic()
    {
        if (menuMusicSource != null) menuMusicSource.Stop();
        if (gameMusicSource != null && !gameMusicSource.isPlaying) gameMusicSource.Play();
    }

    // ---Transiciones de Música (Snapshots) ---
    public void SetBajoElAgua(bool estado)
    {
        if (estado) snapshotBajoElAgua.TransitionTo(0.5f);
        else snapshotNormal.TransitionTo(0.5f);
    }

    public void TransitionToVictoria()
    {
        if (snapshotVictoria != null) snapshotVictoria.TransitionTo(2.0f);
        else snapshotNormal.TransitionTo(2.0f); 
    }

    public void TransitionToDerrota()
    {
        if (snapshotDerrota != null) snapshotDerrota.TransitionTo(1.0f);
        else snapshotNormal.TransitionTo(1.0f);
    }

    // --- Ducking ---
    public void EmpezarVoz()
    {
        vocesSonando++;
        if (duckRoutine != null) StopCoroutine(duckRoutine);
        duckRoutine = StartCoroutine(FadeMusic(musicVolumeDuck));
    }

    public void TerminarVoz()
    {
        vocesSonando = Mathf.Max(0, vocesSonando - 1);
        if (vocesSonando == 0)
        {
            if (duckRoutine != null) StopCoroutine(duckRoutine);
            duckRoutine = StartCoroutine(FadeMusic(musicVolumeNormal));
        }
    }

    private IEnumerator FadeMusic(float targetDb)
    {
        mainMixer.GetFloat(musicVolumeParam, out float current);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duckFadeTime;
            mainMixer.SetFloat(musicVolumeParam, Mathf.Lerp(current, targetDb, t));
            yield return null;
        }
        mainMixer.SetFloat(musicVolumeParam, targetDb);
    }
}