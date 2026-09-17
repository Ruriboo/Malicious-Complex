using UnityEngine;
using TMPro;

public class StatsObserver : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textoMuertes;
    [SerializeField] private TextMeshProUGUI textoCheckpoints;
    [SerializeField] private TextMeshProUGUI textoTiempo;

    private void OnEnable()
    {
        // Suscripción a los eventos del CheckpointManager
        CheckpointManager.OnMuertesCambiadas += ActualizarTextoMuertes;
        CheckpointManager.OnCheckpointAlcanzado += ActualizarTextoCheckpoints;
        CheckpointManager.OnTiempoActualizado += ActualizarTextoTiempo;
    }

    private void OnDisable()
    {
        // Desuscripción obligatoria
        CheckpointManager.OnMuertesCambiadas -= ActualizarTextoMuertes;
        CheckpointManager.OnCheckpointAlcanzado -= ActualizarTextoCheckpoints;
        CheckpointManager.OnTiempoActualizado -= ActualizarTextoTiempo;
    }

    private void ActualizarTextoMuertes(int nuevasMuertes)
    {
        if (textoMuertes != null)
            textoMuertes.text = $"Muertes: {nuevasMuertes}";
    }

    private void ActualizarTextoCheckpoints(int cantidad)
    {
        if (textoCheckpoints != null)
            textoCheckpoints.text = $"Checkpoints: {cantidad}";
    }

    private void ActualizarTextoTiempo(float tiempoSegundos)
    {
        if (textoTiempo != null)
        {
            int minutos = Mathf.FloorToInt(tiempoSegundos / 60F);
            int segundos = Mathf.FloorToInt(tiempoSegundos % 60F);
            textoTiempo.text = string.Format("{0:00}:{1:00}", minutos, segundos);
        }
    }
}