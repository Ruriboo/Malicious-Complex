using UnityEngine;

// Escombro que, cuando el agua de inundacion lo alcanza, flota en su superficie meciendose y derivando.
// Mientras el agua esta por debajo, se queda apoyado donde fue colocado.
public class FlotanteEnAgua : MonoBehaviour
{
    [SerializeField] private float calado = 0.05f;          // cuanto se hunde respecto de la superficie
    [SerializeField] private float amplitudVaiven = 0.06f;
    [SerializeField] private float inclinacionMax = 8f;
    [SerializeField] private float deriva = 0.15f;          // m/s
    [SerializeField] private float radioDeriva = 1.5f;

    private Vector3 posInicial;
    private Quaternion rotInicial;
    private float semilla;
    private FloodWater agua;
    private float proximaBusqueda;
    private Vector3 desplazamiento;

    private void Start()
    {
        posInicial = transform.position;
        rotInicial = transform.rotation;
        semilla = Random.value * 50f;
        CheckpointManager.OnJugadorRespawn += Reiniciar;
    }

    private void OnDestroy() => CheckpointManager.OnJugadorRespawn -= Reiniciar;

    private void Reiniciar()
    {
        desplazamiento = Vector3.zero;
        transform.SetPositionAndRotation(posInicial, rotInicial);
    }

    private void Update()
    {
        if (Time.time > proximaBusqueda)
        {
            proximaBusqueda = Time.time + 0.5f;
            agua = FloodWater.BuscarDebajo(posInicial);
        }
        if (agua == null) return;

        float superficie = agua.AlturaSuperficie - calado;
        if (superficie <= posInicial.y) return; // el agua todavia no llego

        float t = Time.time;
        desplazamiento += new Vector3(Mathf.PerlinNoise(semilla, t * 0.1f) - 0.5f, 0f, Mathf.PerlinNoise(t * 0.1f, semilla) - 0.5f) * (deriva * Time.deltaTime * 2f);
        desplazamiento = Vector3.ClampMagnitude(desplazamiento, radioDeriva);

        Vector3 p = posInicial + desplazamiento;
        p.y = superficie + Mathf.Sin(t * 1.3f + semilla) * amplitudVaiven;
        transform.position = Vector3.Lerp(transform.position, p, Time.deltaTime * 3f);

        Quaternion vaiven = Quaternion.Euler(
            Mathf.Sin(t * 0.9f + semilla) * inclinacionMax,
            t * 4f + semilla * 10f,
            Mathf.Cos(t * 0.7f + semilla) * inclinacionMax);
        transform.rotation = Quaternion.Slerp(transform.rotation, vaiven * rotInicial, Time.deltaTime * 2f);
    }
}
