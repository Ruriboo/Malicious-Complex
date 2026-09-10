using UnityEngine;
using UnityEngine.InputSystem;

public class FloodWater : MonoBehaviour
{
    [SerializeField] private int waterDamage; //cantidad que reduce de oxigeno.

    private Oxigeno oxigeno; //iniciamos una variable de tipo Oxigeno.

    [SerializeField] private float velocidad = 1f;

    [SerializeField]public bool flood = false; //para activar y desactivar el movimiento del agua.


    void Update()
    {
        if (flood)
        {
            transform.Translate(Vector3.up * velocidad * Time.deltaTime); //si flood es true mueve el agua hacia arriba.
        }
    }


    private void OnTriggerStay(Collider colliderOxigeno)
    {
        oxigeno = colliderOxigeno.GetComponent<Oxigeno>(); // guardamos el componente que obtenemos de la colision.

        if (oxigeno != null)
        {
            oxigeno.reducirOxigeno(waterDamage); //esto llama a la funcion que esta adentro de oxigeno
        }
    }

    private void OnTriggerExit(Collider colliderOxigeno)
    {
        oxigeno = colliderOxigeno.GetComponent<Oxigeno>();

        if (oxigeno != null)
        {
            oxigeno.rellenarOxigeno();
            Debug.Log("Salió del agua → oxígeno rellenado");
        }
    }
}
