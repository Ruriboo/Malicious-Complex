using UnityEngine;

public class Oxigeno : MonoBehaviour
{
    //timer es un cronometro que cuenta el tiempo transcurrido.
    private float timer;
    [SerializeField] private float damageCooldown = 1 ; //tiempo en el que el timer se reinicia.(duracion de los tics de daño)
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

    //la funcion reducirOxigeno reduce la cantidad de oxigeno actual una cantidad ingresada.
    public void reducirOxigeno(int cantidad)
    {
        if(timer >= damageCooldown) //esto indica cada cuento tiempo se aplica el if, ej cada 1 segundo
        {
            oxigenoActual = oxigenoActual - cantidad;

            timer = 0; //reiniciamos el timer para que vuelva a contar desde cero 
        }
        if(oxigenoActual<=0)
        {
            Debug.Log("Moriste");
        }
    }

    public void rellenarOxigeno()
    {
        oxigenoActual = oxigenoMax;
    }
}
