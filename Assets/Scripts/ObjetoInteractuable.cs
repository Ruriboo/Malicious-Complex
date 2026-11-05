using UnityEngine;
using UnityEngine.InputSystem;

public abstract class ObjetoInteractuable : MonoBehaviour, IInteractuable
{
    protected bool jugadorEnRango = false;

    protected virtual void Update()
    {
        if (jugadorEnRango && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Interactuar();
        }
    }

    public abstract void Interactuar();

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorEnRango = true;
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorEnRango = false;
        }
    }
}