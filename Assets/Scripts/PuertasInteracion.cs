using UnityEngine;

public class PuertaPortal : MonoBehaviour
{
    [Header("Referencias de las Hojas")]
    public Transform hojaIzquierda;
    public Transform hojaDerecha;

    [Header("Configuración de Desplazamiento")]
    [Tooltip("Distancia que se desplaza cada hoja hacia su lado")]
    public float distanciaApertura = 2.0f;
    public float velocidadApertura = 5.0f;

    private bool jugadorCerca = false;
    private bool estaAbierta = false;

    // Posiciones locales iniciales (cerrada) y finales (abierta)
    private Vector3 posIzquierdaCerrada;
    private Vector3 posIzquierdaAbierta;
    private Vector3 posDerechaCerrada;
    private Vector3 posDerechaAbierta;

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
    }

    private void Update()
    {
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            estaAbierta = !estaAbierta;
        }

        Vector3 targetIzquierda = estaAbierta ? posIzquierdaAbierta : posIzquierdaCerrada;
        Vector3 targetDerecha = estaAbierta ? posDerechaAbierta : posDerechaCerrada;

        if (hojaIzquierda != null)
        {
            hojaIzquierda.localPosition = Vector3.Lerp(hojaIzquierda.localPosition, targetIzquierda, Time.deltaTime * velocidadApertura);
        }

        if (hojaDerecha != null)
        {
            hojaDerecha.localPosition = Vector3.Lerp(hojaDerecha.localPosition, targetDerecha, Time.deltaTime * velocidadApertura);
        }
    }
    public void AbrirCerrarPuerta()
    {
        estaAbierta = !estaAbierta;
    }
}