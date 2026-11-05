using UnityEngine;

// Rack de servidores interactuable: igual que BotonPuerta (trigger de rango + tecla E,
// via ObjetoInteractuable), pero al presionarlo elimina a todos los monstruos (Enemigo) de la escena.
// Los enemigos se desactivan (no se destruyen), asi que no reaparecen al hacer RespawnPlayer().
public class RackEliminarEnemigos : ObjetoInteractuable
{
    [Header("Comportamiento")]
    [Tooltip("Si esta activo, el rack solo se puede usar una vez.")]
    [SerializeField] private bool soloUnaVez = true;

    [Header("Indicador (opcional)")]
    [Tooltip("Luz que cambia de rojo (listo) a verde (usado). Si esta vacio se busca en los hijos.")]
    [SerializeField] private Light luzIndicador;

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sfxActivar;

    private bool usado = false;

    private void Awake()
    {
        if (luzIndicador == null) luzIndicador = GetComponentInChildren<Light>(true);
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        AplicarColor();
    }

    public override void Interactuar()
    {
        if (usado && soloUnaVez) return;

        Enemigo[] enemigos = FindObjectsByType<Enemigo>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Enemigo enemigo in enemigos)
        {
            enemigo.gameObject.SetActive(false);
        }

        usado = true;
        AplicarColor();
        SFXActivar();
        Debug.Log($"[RackEliminarEnemigos] {enemigos.Length} enemigos eliminados.", this);
    }

    private void AplicarColor()
    {
        if (luzIndicador == null) return;
        luzIndicador.color = usado ? Color.green : Color.red;
        luzIndicador.enabled = true;
    }

    private void SFXActivar()
    {
        if (audioSource == null || sfxActivar == null) return;
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(sfxActivar);
    }
}
