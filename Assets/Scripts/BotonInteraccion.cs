using UnityEngine;
using UnityEngine.Audio;

public class BotonPuerta : ObjetoInteractuable
{
    [Header("Referencia al Objetivo")]
    [SerializeField] private MonoBehaviour objetoObjetivo;
    private IInteractuable objetivoInteractuable;

    private bool estaActivado = false;
    private MeshRenderer rendererBoton; 
    private Light luzBoton;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sfxBoton;
    [SerializeField, Range(0f, 1f)] private float volumenBoton = 0.35f;

    private void Awake()
    {
        luzBoton = GetComponentInChildren<Light>(true);
        
        rendererBoton = GetComponent<MeshRenderer>();

        if (objetoObjetivo != null)
        {
            objetivoInteractuable = objetoObjetivo as IInteractuable;
        }
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        AplicarColor();
    }

    protected override void Update()
    {
        base.Update();
    }

    public override void Interactuar()
    {
        if (objetivoInteractuable == null && objetoObjetivo != null)
        {
            objetivoInteractuable = objetoObjetivo as IInteractuable;
        }

        if (objetivoInteractuable != null)
        {
            objetivoInteractuable.Interactuar();
            
            estaActivado = !estaActivado;
            AplicarColor();
            SFXBoton();
        }
    }


    private void AplicarColor()
    {
        Color colorBoton = estaActivado ? Color.green : Color.red;

        if (rendererBoton != null)
        {
            rendererBoton.material.color = colorBoton;
        }
        rendererBoton.material.EnableKeyword("_EMISSION");
        rendererBoton.material.SetColor("_EmissionColor", colorBoton * 2f);

        if (luzBoton != null)
        {
            luzBoton.color = colorBoton;
            luzBoton.enabled = true;
            luzBoton.intensity = 1.5f;
            luzBoton.range = 2f;
        }
    }


    public bool ObtenerEstado() => estaActivado;

    public void EstablecerEstado(bool nuevoEstado)
    {
        estaActivado = nuevoEstado;
        AplicarColor();

        if (objetoObjetivo is PuertaPortal puerta)
        {
            if (estaActivado) puerta.AbrirPuertacheck();
            else puerta.CerrarPuertacheck();
        }
    }
    private void SFXBoton()
    {
        if (audioSource == null || sfxBoton == null) return;
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(sfxBoton, volumenBoton);
    }
}