using System.Collections;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    /// <summary>
    /// Referencias Principales
    /// </summary>
    public Transform player;            
    public Transform water;             

    /// <summary>
    /// Configuración de Respawn y Agua
    /// </summary>
    public Vector3 currentSpawnPoint;    
    public float targetWaterHeight;       
    public float waterLowerSpeed = 5f;

    private float currentWaterTargetY;
    private bool isResettingWater = false;

    private void Start()
    {
        if (player != null)
        {
            currentSpawnPoint = player.position;
        }

        if (water != null)
        {
            targetWaterHeight = water.position.y;
        }
    }

    private void Update()
    {
        if (isResettingWater && water != null)
        {
            Vector3 currentPos = water.position;
            currentPos.y = Mathf.MoveTowards(currentPos.y, currentWaterTargetY, waterLowerSpeed * Time.deltaTime);
            water.position = currentPos;

            if (Mathf.Approximately(water.position.y, currentWaterTargetY))
            {
                isResettingWater = false;
            }
        }
    }

    /// <summary>
    /// Registra un nuevo checkpoint.
    /// </summary>
    public void SetCheckpoint(Vector3 newSpawnPosition, float waterLevelForThisCheckpoint)
    {
        currentSpawnPoint = newSpawnPosition;
        targetWaterHeight = waterLevelForThisCheckpoint;
    }

    /// <summary>
    /// Teletransporta al jugador al último checkpoint y ajusta el nivel del agua.
    /// </summary>
    public void RespawnPlayer()
    {
        if (player == null)
        {
            Debug.LogWarning("No se asignó la referencia del Jugador en el CheckpointManager.");
            return;
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.position = currentSpawnPoint;

        if (cc != null) cc.enabled = true;

        if (water != null)
        {
            currentWaterTargetY = targetWaterHeight;
            isResettingWater = true;
        }
    }
}