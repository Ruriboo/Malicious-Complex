using UnityEngine;

public class TriggerCierrePuerta : TriggerEventos
{
    [SerializeField] private MonoBehaviour objetoInteractuable;

    protected override void EjecutarAccion(GameObject jugador)
    {
        if (objetoInteractuable is IInteractuable interactuable)
        {
            interactuable.Interactuar();
        }
    }
}