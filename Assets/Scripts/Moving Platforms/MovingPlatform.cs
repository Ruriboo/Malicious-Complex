using UnityEngine;

public abstract class MovingPlatform : MonoBehaviour
{
    protected Vector3 spawnpoint;
    [SerializeField] protected int speed;

    private Vector3 lastPosition;
    private CharacterController playerController;

    protected virtual void Start()
    {
        spawnpoint = transform.position;
        lastPosition = transform.position;
    }

    protected virtual void LateUpdate()
    {
        Vector3 platformMovement = transform.position - lastPosition;

        if (playerController != null && playerController.enabled)
        {
            playerController.enabled = false;
            playerController.transform.position += platformMovement;
            playerController.enabled = true;
        }

        lastPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerController = other.GetComponentInParent<CharacterController>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CharacterController exitingPlayer = other.GetComponentInParent<CharacterController>();
            if (exitingPlayer == playerController)
            {
                playerController = null;
            }
        }
    }
}