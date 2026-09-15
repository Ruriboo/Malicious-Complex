using UnityEngine;

public abstract class ObjetoInteractuable : MonoBehaviour, IInteractuable
{
    protected bool jugadorEnRango = false;

    protected virtual void Update()
    {
        if (jugadorEnRango && Input.GetKeyDown(KeyCode.E))
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