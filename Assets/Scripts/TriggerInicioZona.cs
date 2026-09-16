using UnityEngine;
public class TriggerInicioZona : TriggerEventos
{
    protected override void EjecutarAccion(GameObject jugador)
    {
        CheckpointManager.Instance.ActivarSiguienteZona();
    }
}