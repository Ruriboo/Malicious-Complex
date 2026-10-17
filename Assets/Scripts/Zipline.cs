using UnityEngine;
using StarterAssets;

public class Zipline : MonoBehaviour
{
    [SerializeField] private Zipline targetZip;
    [SerializeField] private float zipSpeed = 5f;
    [SerializeField] private float zipScale = 0.2f;
    [SerializeField] private float arrivalThreshold = 0.4f;
    [SerializeField] private LineRenderer cable;
    [SerializeField] private AudioSource ziplineAudio;
    [SerializeField] private AudioSource detachAudio;
    [SerializeField] private AudioClip ziplineVariation1;
    [SerializeField] private AudioClip ziplineVariation2;

    private AudioClip originalZiplineClip;

    public Transform ZipTransform;

    private bool zipping = false;
    private GameObject localZip;
    private CharacterController characterController;
    private ThirdPersonController controller;
    private Animator animator;
    private int animIDZiplining;

    void Awake()
    {
        if (ziplineAudio != null)
            originalZiplineClip = ziplineAudio.clip;
        cable.SetPosition(0, ZipTransform.position);
        cable.SetPosition(1, targetZip.ZipTransform.position);
    }

    void Update()
    {
        if (!zipping || localZip == null) return;


        Vector3 dir = (targetZip.ZipTransform.position - ZipTransform.position).normalized;
        localZip.transform.position += dir * zipSpeed * Time.deltaTime;

        if (Vector3.Distance(localZip.transform.position, targetZip.ZipTransform.position) <= arrivalThreshold)
        {
            ResetZipline();
        }
    }
    public void StartZipline(GameObject player)
    {
        if (zipping) return;


        

        localZip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        localZip.transform.position = ZipTransform.position;
        localZip.transform.localScale = Vector3.one * zipScale;
        localZip.GetComponent<Collider>().isTrigger = true;
        localZip.GetComponent<MeshRenderer>().enabled = false;

        characterController = player.GetComponent<CharacterController>();
        controller = player.GetComponent<ThirdPersonController>();
        animator = player.GetComponent<Animator>();
        animIDZiplining = Animator.StringToHash("IsZiplining");

        if (controller) controller.enabled = false;
        if (characterController) characterController.enabled = false;

        player.transform.parent = localZip.transform;
        player.transform.localPosition = new Vector3(0, -10f, 0);
        player.transform.localRotation = Quaternion.identity;

        if (animator)
            animator.SetBool(animIDZiplining, true);

        zipping = true; Debug.Log("START ZIPLINE: se ejecuto");

        if (ziplineAudio == null)
        {
            Debug.LogError("ZIPLINE AUDIO ES NULL");
            return;
        }

        Debug.Log("Audio: " + ziplineAudio.clip.name);
        Debug.Log("Volumen: " + ziplineAudio.volume);
        Debug.Log("Mute: " + ziplineAudio.mute);
        ziplineAudio.Stop();

        ziplineAudio.Stop();

        AudioClip[] clips =
        {
    originalZiplineClip,
    ziplineVariation1,
    ziplineVariation2
};

        System.Collections.Generic.List<AudioClip> validClips =
            new System.Collections.Generic.List<AudioClip>();

        foreach (AudioClip clip in clips)
        {
            if (clip != null)
                validClips.Add(clip);
        }

        if (validClips.Count > 0)
        {
            ziplineAudio.clip = validClips[Random.Range(0, validClips.Count)];
            ziplineAudio.time = 0f;
            ziplineAudio.Play();
        }

        Debug.Log("INICIO: " + gameObject.name);
        Debug.Log("Audio reproduciéndose: " + ziplineAudio.isPlaying);
    }

    private void ResetZipline()
    {
        if (!zipping) return;

        Debug.Log("FINAL: " + gameObject.name);

        if (detachAudio != null)
            detachAudio.Play();

        GameObject player = localZip.transform.GetChild(0).gameObject;
        player.transform.parent = null;


        player.transform.position = targetZip.ZipTransform.position;

        if (characterController) characterController.enabled = true;
        if (controller) controller.enabled = true;

        if (animator)
            animator.SetBool(animIDZiplining, false);


        Destroy(localZip);
        localZip = null;
        zipping = false;

    }
}
