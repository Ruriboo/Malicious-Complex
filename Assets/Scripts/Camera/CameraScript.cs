using UnityEngine;
using Unity.Cinemachine;

public class CameraScript : MonoBehaviour
{
    [SerializeField] private CinemachineCamera fixedCamera;
    [SerializeField] private Transform cameraTransform;

    private void Start()
    {
        fixedCamera.transform.position = cameraTransform.position;
        fixedCamera.transform.rotation = cameraTransform.rotation;
        fixedCamera.transform.localScale = cameraTransform.localScale;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            fixedCamera.Priority = 20;
        }
    }
}
