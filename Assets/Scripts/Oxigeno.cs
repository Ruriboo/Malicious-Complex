using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Oxigeno : MonoBehaviour
{
    //timer es un cronometro que cuenta el tiempo transcurrido.
    private float timer;
    [SerializeField] private float damageCooldown = 1; //tiempo en el que el timer se reinicia.(duracion de los tics de daño)
    [SerializeField] private int oxigenoMax;
    [SerializeField] private LoseWinCondition loseWinCondition; //Referencia al script LoseWinCondition para cargar la escena de derrota
    [SerializeField] private Image barraOxigeno;
    [SerializeField] private GameObject uiOxigeno;

    private int oxigenoActual;
    private bool Isdead = false;
    private bool enAgua = false;

    public int OxigenoActual => oxigenoActual;
    public int OxigenoMax => oxigenoMax;
    public float PorcentajeOxigeno => oxigenoMax > 0 ? (float)oxigenoActual / oxigenoMax : 0f;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sfxOxigeno;

    void Start()
    {
        oxigenoActual = oxigenoMax;
        uiOxigeno.SetActive(false);
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        timer = timer + Time.deltaTime;
        if (enAgua)
        {
            uiOxigeno.SetActive(true);
            barraOxigeno.fillAmount = PorcentajeOxigeno;
        }
        else
        {
            uiOxigeno.SetActive(false);
        }
        SFXOxigeno();
    }

    //la funcion reducirOxigeno reduce la cantidad de oxigeno actual una cantidad ingresada.
    public void reducirOxigeno(int cantidad)
    {
        if (Isdead) return;

        if (timer >= damageCooldown) //esto indica cada cuento tiempo se aplica el if, ej cada 1 segundo
        {
            oxigenoActual = oxigenoActual - cantidad;
            timer = 0; //reiniciamos el timer para que vuelva a contar desde cero 
        }

        if (oxigenoActual <= 0)
        {
            Debug.Log("Moriste");
            Isdead = true;
            if (audioSource != null && audioSource.isPlaying) audioSource.Stop();

            if (loseWinCondition != null)
                loseWinCondition.CargarLoseScene();
            else
                Debug.LogError("[Oxigeno] Falta asignar LoseWinCondition en el Inspector.");
        }
    }

    public void rellenarOxigeno()
    {
        oxigenoActual = oxigenoMax;
        Isdead = false;
    }

    public void EntrarAgua()
    {
        if (enAgua) return;
        enAgua = true;
        uiOxigeno.SetActive(true);
    }

    public void SalirAgua()
    {
        if (!enAgua) return;
        enAgua = false;
        uiOxigeno.SetActive(false);
        rellenarOxigeno();
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
    }

    private void SFXOxigeno()
    {
        if (audioSource == null || sfxOxigeno == null) return;

        bool pocoOxigeno = enAgua && !Isdead && oxigenoActual > 0 && PorcentajeOxigeno <= 0.25f;

        if (pocoOxigeno)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.clip = sfxOxigeno;
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying && audioSource.clip == sfxOxigeno)
            {
                audioSource.Stop();
            }
        }
    }
}