using UnityEngine;
public class BotonPuerta : MonoBehaviour
{
    [Header("Referencia a la Puerta")]
    public PuertaPortal puertaObjetivo;

    private bool jugadorCerca = false;

    private void Update()
    {
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            if (puertaObjetivo != null)
            {
                puertaObjetivo.AbrirCerrarPuerta();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
        }
    }
}