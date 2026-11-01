using UnityEngine;

public class PuertaPortal : ObjetoInteractuable
{
    [Header("Referencias de las Hojas")]
    [SerializeField] private Transform hojaIzquierda;
    [SerializeField] private Transform hojaDerecha;

    [Header("Configuración de Desplazamiento")]
    [SerializeField] private float distanciaApertura = 2.0f;
    [SerializeField] private float velocidadApertura = 5.0f;

    [Header("Interacción Directa")]
    [SerializeField] private bool abrirSinBoton = true;

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sfxAbrir;
    [SerializeField] private AudioClip sfxCerrar; 
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.05f;

    private bool estaAbierta = false;
    private Vector3 posIzquierdaCerrada, posIzquierdaAbierta;
    private Vector3 posDerechaCerrada, posDerechaAbierta;

    private void Start()
    {
        if (hojaIzquierda != null)
        {
            posIzquierdaCerrada = hojaIzquierda.localPosition;
            posIzquierdaAbierta = posIzquierdaCerrada + new Vector3(0, 0, distanciaApertura);
        }
        if (hojaDerecha != null)
        {
            posDerechaCerrada = hojaDerecha.localPosition;
            posDerechaAbierta = posDerechaCerrada - new Vector3(0, 0, distanciaApertura);
        }
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    protected override void Update()
    {
        if (abrirSinBoton) base.Update();

        Vector3 targetIzquierda = estaAbierta ? posIzquierdaAbierta : posIzquierdaCerrada;
        Vector3 targetDerecha = estaAbierta ? posDerechaAbierta : posDerechaCerrada;

        if (hojaIzquierda != null)
            hojaIzquierda.localPosition = Vector3.Lerp(hojaIzquierda.localPosition, targetIzquierda, Time.deltaTime * velocidadApertura);
        if (hojaDerecha != null)
            hojaDerecha.localPosition = Vector3.Lerp(hojaDerecha.localPosition, targetDerecha, Time.deltaTime * velocidadApertura);
    }

    public override void Interactuar()
    {
        estaAbierta = !estaAbierta;

        if (estaAbierta)
            SFXAbrir();
        else
            SFXCerrar();
    }

    public void AbrirPuertacheck()
    {
        if (!estaAbierta)
            SFXAbrir();

        estaAbierta = true;
    }

    public void CerrarPuertacheck()
    {
        if (estaAbierta)
            SFXCerrar();

        estaAbierta = false;
    }

    private void SFXAbrir()
    {
        if (audioSource == null || sfxAbrir == null) return;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(sfxAbrir);
    }

    private void SFXCerrar()
    {
        if (audioSource == null || sfxCerrar == null) return;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(sfxCerrar);
    }
}