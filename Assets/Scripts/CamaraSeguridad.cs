using UnityEngine;

// Cámara de seguridad: se queda fija en su posición y gira para seguir siempre al jugador.
[RequireComponent(typeof(Camera))]
public class CamaraSeguridad : MonoBehaviour
{
    [Tooltip("Si se deja vacío, busca dentro del objeto con tag \"Player\" el hijo de abajo.")]
    [SerializeField] private Transform objetivo;
    [SerializeField] private string nombreHijoObjetivo = "ColliderOxigeno";
    [Tooltip("Desplazamiento extra respecto al objetivo.")]
    [SerializeField] private Vector3 offsetObjetivo = Vector3.zero;
    [Header("Suavizado")]
    [Tooltip("Tiempo (s) que tarda el punto de mira en alcanzar al jugador. Más alto = más lento y suave.")]
    [SerializeField] private float tiempoSuavizado = 0.4f;
    [Tooltip("Velocidad de giro de la cámara hacia el punto de mira. 0 = giro instantáneo.")]
    [SerializeField] private float velocidadGiro = 4f;

    private Vector3 puntoMira;        // punto suavizado al que mira la cámara
    private Vector3 velocidadMira;    // estado interno de SmoothDamp
    private bool puntoInicializado;

    private void Start()
    {
        BuscarObjetivo();
    }

    private void LateUpdate()
    {
        if (objetivo == null)
        {
            BuscarObjetivo();
            if (objetivo == null) return;
        }

        Vector3 destino = objetivo.position + offsetObjetivo;

        // 1) El punto de mira persigue al jugador con SmoothDamp (absorbe tirones y saltos bruscos)
        if (!puntoInicializado)
        {
            puntoMira = destino;
            velocidadMira = Vector3.zero;
            puntoInicializado = true;
        }
        else
        {
            puntoMira = Vector3.SmoothDamp(puntoMira, destino, ref velocidadMira, tiempoSuavizado);
        }

        Vector3 direccion = puntoMira - transform.position;
        if (direccion.sqrMagnitude < 0.0001f) return;

        // 2) La cámara gira hacia ese punto con Slerp (independiente de los FPS)
        Quaternion rotacionDeseada = Quaternion.LookRotation(direccion, Vector3.up);
        transform.rotation = velocidadGiro <= 0f
            ? rotacionDeseada
            : Quaternion.Slerp(transform.rotation, rotacionDeseada, 1f - Mathf.Exp(-velocidadGiro * Time.deltaTime));
    }

    private void BuscarObjetivo()
    {
        // OJO: hay más de un objeto con tag "Player" (la raíz y PlayerInteractCollider, que está en los pies).
        // FindGameObjectWithTag devuelve cualquiera de ellos, así que se sube a la raíz y se busca desde ahí.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Transform raiz = player.transform.root;
        Transform hijo = BuscarHijo(raiz, nombreHijoObjetivo);
        objetivo = hijo != null ? hijo : raiz;

        if (hijo == null)
            Debug.LogWarning($"[CamaraSeguridad] No se encontró '{nombreHijoObjetivo}' en '{raiz.name}', se apunta a su pivote.", this);
    }

    private void OnDrawGizmosSelected()
    {
        if (objetivo == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, Application.isPlaying ? puntoMira : objetivo.position + offsetObjetivo);
    }

    private static Transform BuscarHijo(Transform padre, string nombre)
    {
        foreach (Transform hijo in padre)
        {
            if (hijo.name == nombre) return hijo;
            Transform encontrado = BuscarHijo(hijo, nombre);
            if (encontrado != null) return encontrado;
        }
        return null;
    }
}
