using UnityEngine;

public class TesterObserver : MonoBehaviour
{
    private void OnEnable()
    {
        // Suscripción a los eventos del CheckpointManager
        CheckpointManager.OnMuertesCambiadas += ProbarMuertes;
        CheckpointManager.OnCheckpointAlcanzado += ProbarCheckpoints;
        CheckpointManager.OnZonaCambiada += ProbarZona;
    }

    private void OnDisable()
    {
        // Desuscripción obligatoria
        CheckpointManager.OnMuertesCambiadas -= ProbarMuertes;
        CheckpointManager.OnCheckpointAlcanzado -= ProbarCheckpoints;
        CheckpointManager.OnZonaCambiada -= ProbarZona;
    }

    private void ProbarMuertes(int nuevasMuertes)
    {
        Debug.Log($"<color=red>[OBSERVER] Evento recibido: El jugador murió. Total muertes: {nuevasMuertes}</color>");
    }

    private void ProbarCheckpoints(int cantidadCheckpoints)
    {
        Debug.Log($"<color=green>[OBSERVER] Evento recibido: Checkpoint alcanzado. Total: {cantidadCheckpoints}</color>");
    }

    private void ProbarZona(string nombreZona)
    {
        Debug.Log($"<color=cyan>[OBSERVER] Evento recibido: Nueva zona cargada: {nombreZona}</color>");
    }
}