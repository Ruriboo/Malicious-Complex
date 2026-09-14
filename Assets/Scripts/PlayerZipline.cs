using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerZipline : MonoBehaviour
{
    [SerializeField] private float checkOffset = 1f;
    [SerializeField] private float checkRadius = 2f;

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
                hit.collider.GetComponent<Zipline>().StartZipline(gameObject);
                break;
            }
        }
    }
}