using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public class FloodWater : MonoBehaviour
{
    [SerializeField] private int waterDamage; //cantidad que reduce de oxigeno.

    private Oxigeno oxigeno; //iniciamos una variable de tipo Oxigeno.

    [SerializeField] private float velocidad = 1f;
    [SerializeField] private bool flood = true;//para activar y desactivar el movimiento del agua.

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sfxUnderwater;

    public bool Flood { get => flood; set => flood = value; }

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }
    void Update()
    {
        if (flood)
        {
            transform.Translate(Vector3.up * velocidad * Time.deltaTime); //si flood es true mueve el agua hacia arriba.
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WaterStop"))
        {
            flood = false;
        }
    }


    private void OnTriggerStay(Collider colliderOxigeno)
    {
        oxigeno = colliderOxigeno.GetComponent<Oxigeno>(); // guardamos el componente que obtenemos de la colision.

        if (oxigeno != null)
        {
            oxigeno.EntrarAgua();
            oxigeno.reducirOxigeno(waterDamage); //esto llama a la funcion que esta adentro de oxigeno
            Debug.Log("DAMAGE");
            if (!audioSource.isPlaying)
            {
                audioSource.clip = sfxUnderwater;
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.Play();
            }
        }
    }

    private void OnTriggerExit(Collider colliderOxigeno)
    {
        oxigeno = colliderOxigeno.GetComponent<Oxigeno>();

        if (oxigeno != null)
        {
            oxigeno.SalirAgua();
            Debug.Log("Salió del agua → oxígeno rellenado");
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
}
