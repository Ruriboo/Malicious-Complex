using System.Collections;
using UnityEngine;

public class ElectricFlashController : MonoBehaviour
{
    public delegate void Descarga();
    public event Descarga OnDescarga;

    [SerializeField] private Light flashLight;
    [SerializeField] private ParticleSystem electricSparks;

    [SerializeField] private float intervaloMinimo = 2f;
    [SerializeField] private float intervaloMaximo = 5f;

    [SerializeField] private int cantidadParpadeos = 3;
    [SerializeField] private float duracionParpadeo = 0.06f;

    [SerializeField] private float duracionLuz = 3f;
    [SerializeField] private float intensidad = 4f;

    [SerializeField] private AudioSource audioRayo;
    void Start()
    {
        flashLight.intensity = intensidad;
        flashLight.enabled = false;
        electricSparks.Stop();

        StartCoroutine(DescargasAleatorias());
    }

    IEnumerator DescargasAleatorias()
    {
        while (true)
        {
            float espera = Random.Range(intervaloMinimo, intervaloMaximo);
            yield return new WaitForSeconds(espera);

            electricSparks.Play();
            if (OnDescarga != null)
            {
                OnDescarga();
            }
            audioRayo.Play();

            for (int i = 0; i < cantidadParpadeos; i++)
            {
                flashLight.enabled = true;
                yield return new WaitForSeconds(duracionParpadeo);
                flashLight.enabled = false;
                yield return new WaitForSeconds(duracionParpadeo);
            }

            flashLight.enabled = true;
            yield return new WaitForSeconds(duracionLuz);
            flashLight.enabled = false;
        }
    }
}