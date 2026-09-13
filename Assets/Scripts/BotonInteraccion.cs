
using StarterAssets;
using UnityEngine;
public class BotonPuerta : MonoBehaviour
{
    [Header("Referencia a la Puerta")]
    public PuertaPortal puertaObjetivo;

    private bool jugadorCerca = false;
    private bool estaActivado = false;
    private MeshRenderer rendererBoton;
    private StarterAssetsInputs inputJugador;

    private void Start()
    {
        rendererBoton = GetComponent<MeshRenderer>();
        AplicarColor();
    }

    private void Update()
    {
        if (jugadorCerca && inputJugador != null && inputJugador.interact)
        {
            inputJugador.interact = false;
            if (puertaObjetivo != null)
            {
                puertaObjetivo.AbrirCerrarPuerta();
                estaActivado = !estaActivado;
                AplicarColor();
            }
        }
    }

    private void AplicarColor()
    {
        if (rendererBoton == null) return;
        rendererBoton.material.color = estaActivado ? Color.green : Color.red;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
            inputJugador = other.GetComponentInParent<StarterAssetsInputs>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
            inputJugador = null;
        }
    }
}