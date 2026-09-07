using UnityEngine;
using UnityEngine.InputSystem;

public class FloodWater : MonoBehaviour
{
    [SerializeField] private int waterDamage;

    private Oxigeno oxigeno;



    [SerializeField] private float velocidad = 1f;

    public bool flood = false;

    void Update()
    {
        //Debug.Log("Moviéndose: " + moviendose);
        if (flood)
        {
            transform.Translate(Vector3.up * velocidad * Time.deltaTime);
        }
    }


    private void OnTriggerStay(Collider otro)
    {
        Oxigeno oxigeno = otro.GetComponent<Oxigeno>();

        if (oxigeno != null)
        {
            oxigeno.reducirOxigeno(waterDamage);
        }
    }

    private void OnTriggerExit(Collider otro)
    {
        Oxigeno oxigeno = otro.GetComponent<Oxigeno>();

        if (oxigeno != null)
        {
            oxigeno.rellenarOxigeno();
            Debug.Log("Salió del agua → oxígeno rellenado");
        }
    }

}
