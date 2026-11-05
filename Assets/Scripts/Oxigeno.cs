using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;
public class Oxigeno : MonoBehaviour
{
    //timer es un cronometro que cuenta el tiempo transcurrido.
    private float timer;
    [SerializeField] private float damageCooldown = 1; //tiempo en el que el timer se reinicia.(duracion de los tics de daño)
    [SerializeField] private int oxigenoMax;
    [SerializeField] private LoseWinCondition loseWinCondition; //Referencia al script LoseWinCondition para cargar la escena de derrota
    [SerializeField] private Image barraOxigeno;
    [SerializeField] private GameObject uiOxigeno;
    [SerializeField] private AudioMixerSnapshot snapshotNormal;
    [SerializeField] private AudioMixerSnapshot snapshotBajoAgua;

    private int oxigenoActual;
    private bool Isdead = false;
    private bool enAgua = false;
    [SerializeField] private float duracionBarraGolpe = 2.5f; // segundos que se ve la barra tras un golpe
    private float mostrarBarraHasta;

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
        // Bajo el agua, o un rato despues de recibir un golpe (para ver cuanto oxigeno queda).
        if (enAgua || Time.time < mostrarBarraHasta)
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
            Morir();
        }
    }

    // Si un golpe de ese porcentaje dejaria al jugador sin oxigeno.
    public bool GolpeSeriaLetal(float porcentaje) => oxigenoActual - Mathf.CeilToInt(oxigenoMax * porcentaje) <= 0;

    // Golpe de enemigo: quita un porcentaje del oxigeno maximo, sin el cooldown del agua.
    // Si lo deja sin oxigeno, muere.
    public void RecibirGolpe(float porcentaje)
    {
        if (Isdead) return;

        oxigenoActual = Mathf.Max(0, oxigenoActual - Mathf.CeilToInt(oxigenoMax * porcentaje));
        mostrarBarraHasta = Time.time + duracionBarraGolpe;

        if (oxigenoActual <= 0) Morir();
    }

    // Muerte inmediata del jugador (sin oxigeno o atrapado por un enemigo).
    public void Morir()
    {
        if (Isdead) return;

        Debug.Log("Moriste");
        Isdead = true;
        oxigenoActual = 0;
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();

        if (loseWinCondition != null)
            loseWinCondition.CargarLoseScene();
        else
            Debug.LogError("[Oxigeno] Falta asignar LoseWinCondition en el Inspector.");
    }

    public bool EstaMuerto => Isdead;

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
        snapshotBajoAgua.TransitionTo(0.3f);
    }

    public void SalirAgua()
    {
        if (!enAgua) return;
        enAgua = false;
        uiOxigeno.SetActive(false);
        rellenarOxigeno();
        snapshotNormal.TransitionTo(0.5f);
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