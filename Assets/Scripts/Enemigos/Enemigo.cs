using System;
using UnityEngine;
using UnityEngine.AI;

// Enemigo basado en NavMeshAgent (paquete AI Navigation).
// Patrulla (puntos o deambulando), persigue al jugador al verlo/oirlo,
// lo busca en su ultima posicion conocida y lo mata al alcanzarlo.
[RequireComponent(typeof(NavMeshAgent))]
public class Enemigo : MonoBehaviour
{
    public enum Estado { Patrullando, Persiguiendo, Buscando, Atacando }

    [Header("Patrulla")]
    [Tooltip("Si esta vacio, el enemigo deambula alrededor de su posicion inicial.")]
    [SerializeField] private Transform[] puntosPatrulla;
    [SerializeField] private float esperaEnPunto = 1.5f;
    [SerializeField] private float radioDeambular = 6f;
    [SerializeField] private float velocidadPatrulla = 1.7f;

    [Header("Deteccion")]
    [SerializeField] private float radioDeteccion = 12f;
    [SerializeField, Range(0f, 360f)] private float anguloVision = 120f;
    [Tooltip("A esta distancia detecta al jugador aunque este a su espalda.")]
    [SerializeField] private float radioOido = 3f;
    [SerializeField] private float alturaOjos = 1.2f;
    [Tooltip("Cuanto adelantar los ojos desde el centro del cuerpo hacia la cabeza (m).")]
    [SerializeField] private float adelantoOjos = 0f;
    [Tooltip("Capas que bloquean la vision (paredes, suelo...).")]
    [SerializeField] private LayerMask obstaculosVision = ~0;
    [Tooltip("Segundos sin ver al jugador antes de volver a patrullar.")]
    [SerializeField] private float tiempoOlvido = 4f;
    [Tooltip("Segundos sin verlo antes de dejar de perseguirlo y pasar a buscarlo (evita perderlo por un frame).")]
    [SerializeField] private float tiempoGraciaVision = 0.75f;

    [Header("Persecucion")]
    [SerializeField] private float velocidadPersecucion = 4.5f;
    [SerializeField] private float intervaloRecalculo = 0.2f;
    [Tooltip("Distancia maxima para proyectar al jugador sobre la NavMesh (saltos, paredes...).")]
    [SerializeField] private float distanciaProyeccion = 2.5f;
    [Tooltip("Segundos que se queda gritando al detectar al jugador antes de perseguirlo (0 = nada).")]
    [SerializeField] private float duracionGrito = 0f;

    [Header("Ataque")]
    [Tooltip("Trigger grande alrededor del cuerpo. Al entrar el jugador empieza el ataque.")]
    [SerializeField] private ZonaAtaqueEnemigo zonaAtaque;
    [Tooltip("Fraccion del oxigeno MAXIMO que quita cada golpe (0.3 = 30%).")]
    [SerializeField, Range(0f, 1f)] private float porcentajeDanio = 0.3f;
    [Tooltip("Duracion de la animacion de ataque (s).")]
    [SerializeField] private float duracionAtaque = 1.5f;
    [Tooltip("Momento del golpe como fraccion de la animacion (0.5 = a la mitad). Si el jugador esta en la zona en ese momento, recibe el dano.")]
    [SerializeField, Range(0f, 1f)] private float momentoGolpe = 0.5f;
    [Tooltip("Segundos entre el final de un ataque y el siguiente.")]
    [SerializeField] private float enfriamientoAtaque = 1.5f;

    [Header("Animacion (opcional)")]
    [SerializeField] private Animator animator;

    [Header("Audio (opcional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sfxDeteccion;
    [SerializeField] private AudioClip sfxAtaque;

    private static readonly int ParamVelocidad = Animator.StringToHash("Velocidad");
    private static readonly int ParamAtacar = Animator.StringToHash("Atacar");
    private static readonly int ParamGritar = Animator.StringToHash("Gritar");

    // Avisos para la animacion procedural / efectos.
    public event Action OnAtaqueIniciado;
    public event Action<Estado> OnEstadoCambiado;

    public Estado EstadoActual => estado;
    public NavMeshAgent Agente => agent;

    private NavMeshAgent agent;
    private Transform jugador;
    private Oxigeno oxigenoJugador;

    private Estado estado = Estado.Patrullando;
    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;
    private Vector3[] puntos = Array.Empty<Vector3>();
    private int indicePunto;
    private float temporizadorEspera;
    private float temporizadorRecalculo;
    private float ultimoAvistamiento;
    private Vector3 ultimaPosicionConocida;
    private float inicioAtaque;
    private float proximoAtaque;
    private float finGrito;
    private bool golpeResuelto;
    private bool golpeMortalPendiente;
    private Oxigeno victimaPendiente;
    private readonly RaycastHit[] impactosVision = new RaycastHit[16];

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (zonaAtaque == null) zonaAtaque = GetComponentInChildren<ZonaAtaqueEnemigo>();
    }

    // Registro de enemigos activos (lo usa LuzFluorescente: los tubos parpadean cerca de un monstruo).
    private static readonly System.Collections.Generic.List<Enemigo> activos = new();
    public static System.Collections.Generic.IReadOnlyList<Enemigo> Activos => activos;

    private void OnEnable()
    {
        CheckpointManager.OnJugadorRespawn += Reiniciar;
        if (!activos.Contains(this)) activos.Add(this);
    }

    private void OnDisable()
    {
        CheckpointManager.OnJugadorRespawn -= Reiniciar;
        activos.Remove(this);
    }

    private void Start()
    {
        posicionInicial = transform.position;
        rotacionInicial = transform.rotation;

        // Se cachean las posiciones: los puntos pueden ser hijos del enemigo sin moverse con el.
        if (puntosPatrulla != null)
        {
            puntos = Array.ConvertAll(Array.FindAll(puntosPatrulla, p => p != null), p => p.position);
        }

        BuscarJugador();

        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, agent.areaMask))
            {
                agent.Warp(hit.position);
            }
            else
            {
                Debug.LogWarning($"[Enemigo] {name} no esta sobre la NavMesh. Hornea el NavMeshSurface (Tools/Enemigos).", this);
                enabled = false;
                return;
            }
        }

        CambiarEstado(Estado.Patrullando);
        SiguienteDestinoPatrulla();
    }

    private void BuscarJugador()
    {
        // Varios objetos tienen el tag "Player": la raiz vacia del prefab "Player" (que se queda
        // quieta en el spawn), "PlayerArmature" (el que se mueve) y su collider de interaccion.
        // El objetivo real es el que tiene el CharacterController.
        GameObject[] candidatos = GameObject.FindGameObjectsWithTag("Player");
        if (candidatos.Length == 0)
        {
            Debug.LogWarning("[Enemigo] No se encontro ningun objeto con el tag 'Player'.", this);
            return;
        }

        CharacterController cc = null;
        foreach (GameObject go in candidatos)
        {
            cc = go.GetComponentInParent<CharacterController>();
            if (cc == null) cc = go.GetComponentInChildren<CharacterController>();
            if (cc != null) break;
        }

        jugador = cc != null ? cc.transform : candidatos[0].transform;
        if (cc == null) Debug.LogWarning("[Enemigo] El jugador no tiene CharacterController; se usa " + jugador.name, this);

        oxigenoJugador = jugador.GetComponentInChildren<Oxigeno>();
        if (oxigenoJugador == null) oxigenoJugador = jugador.GetComponentInParent<Oxigeno>();
        if (oxigenoJugador == null) oxigenoJugador = jugador.root.GetComponentInChildren<Oxigeno>();
    }

    private void Update()
    {
        if (!agent.isOnNavMesh) return;

        bool jugadorVivo = jugador != null && (oxigenoJugador == null || !oxigenoJugador.EstaMuerto);
        // Si lo esta tocando (zona de ataque) cuenta como visto aunque el rayo falle.
        bool loVe = jugadorVivo && ((zonaAtaque != null && zonaAtaque.JugadorDentro) || PuedeVerJugador());
        if (loVe)
        {
            ultimoAvistamiento = Time.time;
            ultimaPosicionConocida = jugador.position;
        }

        if (estado != Estado.Atacando && PuedeAtacar(jugadorVivo))
        {
            EmpezarAtaque();
        }

        switch (estado)
        {
            case Estado.Patrullando:
                if (loVe) { EmpezarPersecucion(); break; }
                ActualizarPatrulla();
                break;

            case Estado.Persiguiendo:
                if (!jugadorVivo) { VolverAPatrullar(); break; }
                // Sigue persiguiendo la posicion actual un momento aunque no lo vea; solo si lo
                // pierde mas de tiempoGraciaVision pasa a buscarlo en su ultima posicion conocida.
                if (!loVe && Time.time - ultimoAvistamiento > tiempoGraciaVision)
                {
                    CambiarEstado(Estado.Buscando);
                    IrA(ultimaPosicionConocida);
                    break;
                }
                ActualizarPersecucion();
                break;

            case Estado.Buscando:
                // Va a la ultima posicion conocida y espera ahi hasta olvidarlo.
                if (loVe) { CambiarEstado(Estado.Persiguiendo); break; }
                if (Time.time - ultimoAvistamiento > tiempoOlvido) VolverAPatrullar();
                break;

            case Estado.Atacando:
                ActualizarAtaque(jugadorVivo);
                break;
        }

        if (animator != null)
        {
            animator.SetFloat(ParamVelocidad, agent.velocity.magnitude);
        }
    }

    // ---------- Patrulla ----------

    private void ActualizarPatrulla()
    {
        if (!LlegoAlDestino()) return;

        temporizadorEspera -= Time.deltaTime;
        if (temporizadorEspera <= 0f) SiguienteDestinoPatrulla();
    }

    private void SiguienteDestinoPatrulla()
    {
        temporizadorEspera = esperaEnPunto;
        agent.speed = velocidadPatrulla;

        if (puntos.Length > 0)
        {
            IrA(puntos[indicePunto]);
            indicePunto = (indicePunto + 1) % puntos.Length;
            return;
        }

        // Deambular: punto aleatorio de la NavMesh alrededor de la posicion inicial.
        for (int i = 0; i < 5; i++)
        {
            Vector2 r = UnityEngine.Random.insideUnitCircle * radioDeambular;
            Vector3 candidato = posicionInicial + new Vector3(r.x, 0f, r.y);
            if (NavMesh.SamplePosition(candidato, out NavMeshHit hit, 2f, agent.areaMask))
            {
                IrA(hit.position);
                return;
            }
        }
    }

    private void VolverAPatrullar()
    {
        CambiarEstado(Estado.Patrullando);
        SiguienteDestinoPatrulla();
    }

    // ---------- Persecucion ----------

    private void EmpezarPersecucion()
    {
        CambiarEstado(Estado.Persiguiendo);
        Reproducir(sfxDeteccion);
        temporizadorRecalculo = 0f;

        if (duracionGrito > 0f)
        {
            finGrito = Time.time + duracionGrito;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            if (animator != null) animator.SetTrigger(ParamGritar);
        }
    }

    private void ActualizarPersecucion()
    {
        // Grito de deteccion: se queda quieto mirando al jugador.
        if (Time.time < finGrito)
        {
            MirarHacia(jugador.position);
            return;
        }

        agent.speed = velocidadPersecucion;
        temporizadorRecalculo -= Time.deltaTime;
        if (temporizadorRecalculo > 0f) return;
        temporizadorRecalculo = intervaloRecalculo;

        // Si el jugador esta en el aire / en una pared / en la tirolesa, se proyecta a la NavMesh.
        if (NavMesh.SamplePosition(jugador.position, out NavMeshHit hit, distanciaProyeccion, agent.areaMask))
        {
            IrA(hit.position);
        }
    }

    // ---------- Ataque ----------
    // Como el agua (FloodWater): un trigger grande ("ZonaAtaque") detecta al jugador.
    // Al entrar arranca la animacion de ataque; al TERMINAR la animacion, si el jugador
    // sigue dentro del trigger recibe el dano, si salio a tiempo no le pasa nada.

    private bool PuedeAtacar(bool jugadorVivo)
    {
        return jugadorVivo && zonaAtaque != null && zonaAtaque.JugadorDentro && Time.time >= proximoAtaque;
    }

    private void EmpezarAtaque()
    {
        CambiarEstado(Estado.Atacando);
        inicioAtaque = Time.time;
        golpeResuelto = false;
        golpeMortalPendiente = false;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        if (animator != null) animator.SetTrigger(ParamAtacar);
        Reproducir(sfxAtaque);
        OnAtaqueIniciado?.Invoke();
    }

    private void ActualizarAtaque(bool jugadorVivo)
    {
        if (jugadorVivo) MirarHacia(jugador.position);

        float t = Time.time - inicioAtaque;

        // A mitad de la animacion: si el jugador sigue en la zona de ataque, recibe el golpe.
        if (!golpeResuelto && t >= duracionAtaque * momentoGolpe)
        {
            golpeResuelto = true;
            Oxigeno victima = zonaAtaque != null && zonaAtaque.OxigenoJugador != null ? zonaAtaque.OxigenoJugador : oxigenoJugador;
            if (jugadorVivo && victima != null && zonaAtaque != null && zonaAtaque.JugadorDentro)
            {
                // Golpe mortal: la muerte pausa el juego (timeScale = 0), asi que se aplica al
                // terminar la animacion para no cortarla. Si no es mortal, el dano es inmediato.
                if (victima.GolpeSeriaLetal(porcentajeDanio))
                {
                    golpeMortalPendiente = true;
                    victimaPendiente = victima;
                }
                else
                {
                    victima.RecibirGolpe(porcentajeDanio);
                }
            }
        }

        if (t < duracionAtaque) return;

        if (golpeMortalPendiente)
        {
            golpeMortalPendiente = false;
            if (victimaPendiente != null) victimaPendiente.RecibirGolpe(porcentajeDanio);
            victimaPendiente = null;
        }

        proximoAtaque = Time.time + enfriamientoAtaque;
        // Retoma la persecucion en el acto, con destino recalculado.
        ultimoAvistamiento = Time.time;
        temporizadorRecalculo = 0f;
        agent.isStopped = false;

        bool sigueVivo = oxigenoJugador == null || !oxigenoJugador.EstaMuerto;
        if (sigueVivo) CambiarEstado(Estado.Persiguiendo);
        else VolverAPatrullar();
    }

    // ---------- Deteccion ----------

    private bool PuedeVerJugador()
    {
        Vector3 ojos = Ojos();
        Vector3 objetivo = jugador.position + Vector3.up * 1f;
        Vector3 dir = objetivo - ojos;
        float dist = dir.magnitude;

        if (dist > radioDeteccion) return false;
        if (dist < 0.01f) return true;

        if (dist > radioOido)
        {
            Vector3 plano = new Vector3(dir.x, 0f, dir.z);
            if (Vector3.Angle(transform.forward, plano) > anguloVision * 0.5f) return false;
        }

        // Linea de vista: el primer impacto que NO sea el propio enemigo debe ser el jugador.
        // (Antes el rayo chocaba con el cuerpo del monstruo cuando el jugador estaba cerca.)
        int n = Physics.RaycastNonAlloc(ojos, dir / dist, impactosVision, dist, obstaculosVision, QueryTriggerInteraction.Ignore);
        float masCerca = float.MaxValue;
        Transform primero = null;
        for (int i = 0; i < n; i++)
        {
            Transform t = impactosVision[i].transform;
            if (t == transform || t.IsChildOf(transform)) continue;
            if (impactosVision[i].distance < masCerca)
            {
                masCerca = impactosVision[i].distance;
                primero = t;
            }
        }
        return primero == null || primero == jugador || primero.IsChildOf(jugador);
    }

    private Vector3 Ojos() => transform.position + Vector3.up * alturaOjos + transform.forward * adelantoOjos;

    // ---------- Utilidades ----------

    private void IrA(Vector3 destino)
    {
        agent.isStopped = false;
        agent.SetDestination(destino);
    }

    private bool LlegoAlDestino()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f;
    }

    private void MirarHacia(Vector3 punto)
    {
        Vector3 d = punto - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), agent.angularSpeed * Time.deltaTime);
    }

    private void CambiarEstado(Estado nuevo)
    {
        if (estado == nuevo) return;
        estado = nuevo;
        OnEstadoCambiado?.Invoke(nuevo);
    }

    private void Reproducir(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    // Llamado al reaparecer el jugador: el enemigo vuelve a su posicion inicial.
    public void Reiniciar()
    {
        if (agent == null || !agent.enabled) return;

        agent.Warp(posicionInicial);
        transform.rotation = rotacionInicial;
        agent.isStopped = false;
        indicePunto = 0;
        proximoAtaque = 0f;
        golpeMortalPendiente = false;
        victimaPendiente = null;
        if (zonaAtaque != null) zonaAtaque.Vaciar();
        finGrito = 0f;
        if (animator != null)
        {
            animator.ResetTrigger(ParamAtacar);
            animator.ResetTrigger(ParamGritar);
            animator.Rebind();
        }
        VolverAPatrullar();
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 ojos = Ojos();
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radioDeteccion);
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radioOido);

        Vector3 izq = Quaternion.Euler(0f, -anguloVision * 0.5f, 0f) * transform.forward;
        Vector3 der = Quaternion.Euler(0f, anguloVision * 0.5f, 0f) * transform.forward;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(ojos, ojos + izq * radioDeteccion);
        Gizmos.DrawLine(ojos, ojos + der * radioDeteccion);

        if (puntosPatrulla == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < puntosPatrulla.Length; i++)
        {
            if (puntosPatrulla[i] == null) continue;
            Gizmos.DrawSphere(puntosPatrulla[i].position, 0.25f);
            Transform sig = puntosPatrulla[(i + 1) % puntosPatrulla.Length];
            if (sig != null) Gizmos.DrawLine(puntosPatrulla[i].position, sig.position);
        }
    }
}
