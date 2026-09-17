using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("Referencias Principales")]
    [SerializeField] private Transform player;
    [SerializeField] private Oxigeno oxigeno;

    [Header("Zonas del Nivel (en orden de progresión)")]
    [Tooltip("Agregar en orden: Zona 1, Zona 2, Zona 3, Zona 4...")]
    [SerializeField] private List<ZonaInundacion> zonasDelNivel = new();

    [Header("Configuración de Respawn")]
    [SerializeField] private Vector3 currentSpawnPoint;

    [Header("Estadísticas")]
    [SerializeField] private int muertes = 0;
    [SerializeField] private int checkpointsAlcanzados = 0;
    [SerializeField] private float tiempoTranscurrido = 0f;

    public int Muertes => muertes;
    public int CheckpointsAlcanzados => checkpointsAlcanzados;
    public float TiempoTranscurrido => tiempoTranscurrido;
    public string NombreZonaActual => zonaActual.nombreZona;

    public Transform Water => (zonaActual.aguas != null && zonaActual.aguas.Count > 0 && zonaActual.aguas[0] != null) 
    ? zonaActual.aguas[0].transform 
    : null;


    private Dictionary<BotonPuerta, bool> estadosBotonesGuardados = new();

    private Queue<ZonaInundacion> zonasPendientes = new();

    private Stack<ZonaInundacion> zonasJugadas = new();

    private ZonaInundacion zonaActual;
    private bool juegoTerminado = false;
    private BotonPuerta[] todosLosBotones;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        RefrescarBotones();
        InicializarZonas();
    }

    private void Update()
    {
        if (!juegoTerminado) tiempoTranscurrido += Time.deltaTime;
    }

    // ZONAS

    /// <summary>
    /// Carga las zonas del nivel en la Queue, en el orden del Inspector.
    /// </summary>
    private void InicializarZonas()
    {
        zonasPendientes.Clear();
        zonasJugadas.Clear();

        foreach (var zona in zonasDelNivel)
        {
            if (zona.EsValida())
                zonasPendientes.Enqueue(zona);
            else
                Debug.LogWarning($"[CheckpointManager] Zona inválida: {zona.nombreZona}");
        }

        if (zonasPendientes.Count > 0)
            ActivarSiguienteZona();
    }

    public void ActivarSiguienteZona()
    {
        if (zonasPendientes.Count == 0)
        {
            return;
        }

        // Detener todas las aguas del nivel
        DetenerTodasLasAguas();

        // Desencolar la siguiente zona
        zonaActual = zonasPendientes.Dequeue();
        zonasJugadas.Push(zonaActual);

        // Activar todas las aguas de la nueva zona
        if (zonaActual.aguas != null)
        {
            foreach (var agua in zonaActual.aguas)
            {
                if (agua != null) agua.Flood = true;
            }
        }

        GuardarEstadoActualBotones();
    }

    private void DetenerTodasLasAguas()
    {
        foreach (var zona in zonasDelNivel)
        {
            if (zona.aguas != null)
            {
                foreach (var agua in zona.aguas)
                {
                    if (agua != null) agua.Flood = false;
                }
            }
        }
    }

    // CHECKPOINTS

    public void SetCheckpoint(Vector3 newSpawnPosition, float waterLevelForThisCheckpoint)
    {
        currentSpawnPoint = newSpawnPosition;
        checkpointsAlcanzados++;
        RefrescarBotones();
        GuardarEstadoActualBotones();
    }

    private void RefrescarBotones()
    {
        todosLosBotones = FindObjectsByType<BotonPuerta>(FindObjectsSortMode.None);
    }

    private void GuardarEstadoActualBotones()
    {
        estadosBotonesGuardados.Clear();
        foreach (BotonPuerta boton in todosLosBotones)
            if (boton != null)
                estadosBotonesGuardados[boton] = boton.ObtenerEstado();
    }

    // RESPAWN

    public void RespawnPlayer()
    {
        if (player == null)
        {
            Debug.LogWarning("[CheckpointManager] No se asignó el Jugador.");
            return;
        }

        muertes++;
        juegoTerminado = false;

        // Usar la última zona del Stack 
        if (zonasJugadas.Count > 0)
        {
            ZonaInundacion zonaRespawn = zonasJugadas.Peek();

            // Mover jugador al punto de entrada de la zona
            if (zonaRespawn.puntoEntrada != null)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.position = zonaRespawn.puntoEntrada.position;
                if (cc != null) cc.enabled = true;
            }

            // Resetear todas las aguas de la zona a su altura inicial
            if (zonaRespawn.aguas != null)
            {
                foreach (var agua in zonaRespawn.aguas)
                {
                    if (agua != null)
                    {
                        Vector3 pos = agua.transform.position;
                        pos.y = zonaRespawn.alturaInicial;
                        agua.transform.position = pos;
                        agua.Flood = true;
                    }
                }
            }
        }

        if (oxigeno != null) oxigeno.rellenarOxigeno();

        RestaurarEstadoBotones();
    }

    private void RestaurarEstadoBotones()
    {
        foreach (var entrada in estadosBotonesGuardados)
            if (entrada.Key != null)
                entrada.Key.EstablecerEstado(entrada.Value);
    }

    public void FinalizarJuego() => juegoTerminado = true;
}