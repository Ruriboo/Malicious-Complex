using UnityEngine;

public class AutoRouteAudio : MonoBehaviour
{
    private void Start()
    {
        AudioSource[] todasLasFuentes = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        foreach (var source in todasLasFuentes)
        {
            if (source.outputAudioMixerGroup == null && AudioManager.Instance != null)
            {
                if (source.gameObject.name.ToLower().Contains("paso") || source.gameObject.name.ToLower().Contains("agua"))
                    source.outputAudioMixerGroup = AudioManager.Instance.SFX;
                else if (source.loop && source.clip != null && source.clip.length > 15f)
                    source.outputAudioMixerGroup = AudioManager.Instance.Music;
                else
                    source.outputAudioMixerGroup = AudioManager.Instance.SFX;
            }
        }
    }
}