using Unity.Cinemachine;
using UnityEngine;

public class ReturnControl : MonoBehaviour
{
    [SerializeField] private CinemachineCamera fixedCamera;

    public void ResetCamera()
    {
        fixedCamera.Priority = -2;
    }
}
