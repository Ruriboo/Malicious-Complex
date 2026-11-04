using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer Único")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("Grupos del Mixer (Asignar en Inspector)")]
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup ambienteGroup;
    [SerializeField] private AudioMixerGroup vozGroup;

    [Header("Parámetros Expuestos")]
    [SerializeField] private string musicVolumeParam = "MusicVolume";
    [SerializeField] private string musicLowpassParam = "MusicLowpassCutoff";
    [SerializeField] private string sfxLowpassParam = "SFXLowpassCutoff";

    [Header("Configuración de Ducking (Voz sobre Música)")]
    [SerializeField] private float musicVolumeNormal = -12f; 
    [SerializeField] private float musicVolumeDuck = -24f;   
    [SerializeField] private float duckFadeTime = 0.4f;

    private int vocesSonando;
    private Coroutine duckRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public AudioMixerGroup Music => musicGroup;
    public AudioMixerGroup SFX => sfxGroup;
    public AudioMixerGroup Ambiente => ambienteGroup;
    public AudioMixerGroup Voz => vozGroup;

    // --- DUCKING (Bajar música cuando hay voz) ---
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

    // --- EFECTO BAJO EL AGUA (Lowpass) ---
    public void SetBajoElAgua(bool estado)
    {
        float objetivo = estado ? 500f : 22000f;
        
        StopCoroutine(nameof(FadeCutoff));

        StartCoroutine(FadeCutoff(musicLowpassParam, objetivo));
        StartCoroutine(FadeCutoff(sfxLowpassParam, objetivo));
    }

    private IEnumerator FadeCutoff(string parametro, float objetivo)
    {
        mainMixer.GetFloat(parametro, out float actual);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.3f;
            mainMixer.SetFloat(parametro, Mathf.Lerp(actual, objetivo, t));
            yield return null;
        }
        mainMixer.SetFloat(parametro, objetivo);
    }
    
    // --- FADE GENÉRICO PARA CUALQUIER AUDIO ---
    public void FadeSource(AudioSource source, float targetVolume, float duration)
    {
        StartCoroutine(FadeSourceRoutine(source, targetVolume, duration));
    }

    private IEnumerator FadeSourceRoutine(AudioSource source, float target, float dur)
    {
        float start = source.volume;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            source.volume = Mathf.Lerp(start, target, t);
            yield return null;
        }
        source.volume = target;
    }
}