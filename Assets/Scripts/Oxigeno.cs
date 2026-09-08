using UnityEngine;

public class Oxigeno : MonoBehaviour
{
    [SerializeField] private float timer;

    [SerializeField] private int oxigenoMax;


    private int oxigenoActual;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        oxigenoActual = oxigenoMax;
    }

    // Update is called once per frame
    void Update()
    {
        timer = timer + Time.deltaTime;

    }

    public void reducirOxigeno(int cantidad)
    {
        if(timer >= 1)
        {
            oxigenoActual = oxigenoActual - cantidad;
            timer = 0;
        }
        if(oxigenoActual<=0)
        {
            Debug.Log("Moriste -1000 de aura");

        }
        

    }

    public void rellenarOxigeno()
    {
        oxigenoActual = oxigenoMax;

    }

}
