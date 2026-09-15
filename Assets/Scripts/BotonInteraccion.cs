using UnityEngine;

public class BotonPuerta : ObjetoInteractuable
{
    [Header("Referencia al Objetivo")]
    [SerializeField] private MonoBehaviour objetoObjetivo;
    private IInteractuable objetivoInteractuable;

    private bool estaActivado = false;
    private MeshRenderer rendererBoton;

    private void Awake()
    {
        rendererBoton = GetComponent<MeshRenderer>();
        
        if (objetoObjetivo != null)
        {
            objetivoInteractuable = objetoObjetivo as IInteractuable;
        }
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
        }
    }   

    private void AplicarColor()
    {
        if (rendererBoton != null)
        {
            rendererBoton.material.color = estaActivado ? Color.green : Color.red;
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
}