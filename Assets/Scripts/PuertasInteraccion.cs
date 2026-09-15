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
    }

    protected override void Update()
    {
        if (abrirSinBoton)
        {
            base.Update();
        }

        Vector3 targetIzquierda = estaAbierta ? posIzquierdaAbierta : posIzquierdaCerrada;
        Vector3 targetDerecha = estaAbierta ? posDerechaAbierta : posDerechaCerrada;

        if (hojaIzquierda != null)
            hojaIzquierda.localPosition = Vector3.Lerp(hojaIzquierda.localPosition, targetIzquierda, Time.deltaTime * velocidadApertura);

        if (hojaDerecha != null)
            hojaDerecha.localPosition = Vector3.Lerp(hojaDerecha.localPosition, targetDerecha, Time.deltaTime * velocidadApertura);
    }

    public override void Interactuar() => estaAbierta = !estaAbierta;
    public void AbrirPuertacheck() => estaAbierta = true;
    public void CerrarPuertacheck() => estaAbierta = false;
}