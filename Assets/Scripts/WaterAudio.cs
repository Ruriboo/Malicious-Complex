
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class WaterAudio : MonoBehaviour
{
    [Header("Pasos de agua")]
    [SerializeField] private AudioClip[] pasosAgua;

    [SerializeField, Range(0f, 1f)]
    private float volumen = 0.8f;

    private AudioSource audioSource;
    private StarterAssets.ThirdPersonController thirdPersonController;

    private bool enAgua;
    private int ultimoClip = -1;
    private int zonasAgua;
    private bool bajoElAgua;


    private void Awake()
    {
        thirdPersonController =
            GetComponent<StarterAssets.ThirdPersonController>();

        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }
    public void EstablecerBajoElAgua(bool estado)
    {
        bajoElAgua = estado;
    }
    public void ReproducirPasoAnimacion()
    {
        if (!enAgua || bajoElAgua)
            return;

        if (thirdPersonController == null ||
            !thirdPersonController.Grounded)
            return;

        if (pasosAgua == null || pasosAgua.Length == 0)
            return;

        int clipIndex = Random.Range(0, pasosAgua.Length);

        if (pasosAgua.Length > 1 && clipIndex == ultimoClip)
        {
            clipIndex = (clipIndex +
                Random.Range(1, pasosAgua.Length)) % pasosAgua.Length;
        }

        ultimoClip = clipIndex;

        AudioClip clip = pasosAgua[clipIndex];

        if (clip != null)
            audioSource.PlayOneShot(clip, volumen);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<FloodWater>() == null)
            return;

        zonasAgua++;
        enAgua = true;

        if (thirdPersonController != null)
            thirdPersonController.EnAgua = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<FloodWater>() == null)
            return;

        zonasAgua = Mathf.Max(0, zonasAgua - 1);
        enAgua = zonasAgua > 0;

        if (thirdPersonController != null)
            thirdPersonController.EnAgua = enAgua;
    }
}
