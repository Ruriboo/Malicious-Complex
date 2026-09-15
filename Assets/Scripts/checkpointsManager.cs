using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("Referencias Principales")]
    public Transform player;            
    public Transform water;             

    [Header("Configuración de Respawn y Agua")]
    public Vector3 currentSpawnPoint;    
    public float targetWaterHeight;       
    public float waterLowerSpeed = 5f;

    [Header("Estadísticas")]
    public int muertes = 0;
    public int checkpointsAlcanzados = 0;
    public float tiempoTranscurrido = 0f;

    private bool juegoTerminado = false;

    private BotonPuerta[] todosLosBotones;
    private Dictionary<BotonPuerta, bool> estadosBotonesGuardados = new Dictionary<BotonPuerta, bool>();

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

        if (player != null)
        {
            currentSpawnPoint = player.position;
        }

        if (water != null)
        {
            targetWaterHeight = water.position.y;
        }

        todosLosBotones = FindObjectsByType<BotonPuerta>(FindObjectsSortMode.None);
        GuardarEstadoActualBotones();
    }

    private void Update()
    {
        if (!juegoTerminado)
        {
            tiempoTranscurrido += Time.deltaTime;
        }
    }

    public void SetCheckpoint(Vector3 newSpawnPosition, float waterLevelForThisCheckpoint)
    {
        currentSpawnPoint = newSpawnPosition;
        targetWaterHeight = waterLevelForThisCheckpoint;

        checkpointsAlcanzados++;

        GuardarEstadoActualBotones();
    }

    private void GuardarEstadoActualBotones()
    {
        estadosBotonesGuardados.Clear();

        foreach (BotonPuerta boton in todosLosBotones)
        {
            if (boton != null)
            {
                estadosBotonesGuardados[boton] = boton.ObtenerEstado();
            }
        }
    }

    public void RespawnPlayer()
    {
        if (player == null)
        {
            Debug.LogWarning("No se asignó la referencia del Jugador en el CheckpointManager.");
            return;
        }

        muertes++;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.position = currentSpawnPoint;

        if (cc != null) cc.enabled = true;

        if (water != null)
        {
            Vector3 newWaterPos = water.position;
            newWaterPos.y = targetWaterHeight;
            water.position = newWaterPos;
        }

        RestaurarEstadoBotones();
    }

    private void RestaurarEstadoBotones()
    {
        foreach (KeyValuePair<BotonPuerta, bool> entrada in estadosBotonesGuardados)
        {
            BotonPuerta boton = entrada.Key;
            bool estadoGuardado = entrada.Value;

            if (boton != null)
            {
                boton.EstablecerEstado(estadoGuardado);
            }
        }
    }

    public void FinalizarJuego()
    {
        juegoTerminado = true;
    }
}