using System.Collections;
using UnityEngine;

public class CambioDeAmbiente : MonoBehaviour
{
    [SerializeField] private AudioSource ambienteActual;
    [SerializeField] private AudioSource ambienteNuevo;
    [SerializeField] private float volumenNuevo = 0.3f;
    [SerializeField] private float duracion = 3f;

    private bool cambioAmbiente = false;

    private void OnTriggerEnter(Collider other)
    {
        if (cambioAmbiente || !other.CompareTag("Player")) return;

        cambioAmbiente = true;
        StartCoroutine(Crossfade());
    }

    private IEnumerator Crossfade()
    {
        float volumenInicial = ambienteActual.volume;
        ambienteNuevo.volume = 0f;
        ambienteNuevo.Play();

        float tiempo = 0f;
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float progreso = tiempo / duracion;
            ambienteActual.volume = Mathf.Lerp(volumenInicial, 0f, progreso);
            ambienteNuevo.volume = Mathf.Lerp(0f, volumenNuevo, progreso);
            yield return null;
        }

        ambienteActual.Stop();
    }
}
