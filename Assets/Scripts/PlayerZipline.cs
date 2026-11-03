using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerZipline : MonoBehaviour
{
    [SerializeField] private float checkOffset = 1f;
    [SerializeField] private float checkRadius = 2f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sfxZip;

    private void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            UseZipline();
        }
    }

    private void UseZipline()
    {
        RaycastHit[] hits = Physics.SphereCastAll(transform.position + new Vector3(0, checkOffset, 0), checkRadius, Vector3.up);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("Zipline"))
            {
                SFXZipline();
                hit.collider.GetComponent<Zipline>().StartZipline(gameObject);
                break;
            }
            
        }
    }
    private void SFXZipline()
    {
        if (audioSource == null || sfxZip == null) return;
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(sfxZip);
    }
}