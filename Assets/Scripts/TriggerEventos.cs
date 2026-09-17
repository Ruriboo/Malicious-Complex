using UnityEngine;

public abstract class TriggerEventos : MonoBehaviour
{
    [SerializeField] protected bool soloUnaVez = true;
    protected bool yaFueActivado = false;
    private void OnTriggerEnter(Collider other)
    {
        if (yaFueActivado && soloUnaVez) return;

        if (other.CompareTag("Player"))
        {
            EjecutarAccion(other.gameObject);
            yaFueActivado = true;

            if (soloUnaVez)
            {
                gameObject.SetActive(false);
            }
        }
    }

    protected abstract void EjecutarAccion(GameObject jugador);
}