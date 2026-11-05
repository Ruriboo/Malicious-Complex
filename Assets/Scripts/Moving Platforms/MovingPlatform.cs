using UnityEngine;

public abstract class MovingPlatform : MonoBehaviour
{
    protected Vector3 spawnpoint;
    [SerializeField] protected int speed;

    private Vector3 lastPosition;
    private CharacterController playerController;
    private Collider triggerCollider;

    protected virtual void Start()
    {
        spawnpoint = transform.position;
        lastPosition = transform.position;

        // Collider de deteccion (el que tiene Is Trigger activado)
        foreach (Collider c in GetComponents<Collider>())
        {
            if (c.isTrigger) { triggerCollider = c; break; }
        }
    }

    protected virtual void LateUpdate()
    {
        Vector3 platformMovement = transform.position - lastPosition;

        // OnTriggerExit no es fiable porque deshabilitamos el CharacterController al moverlo:
        // comprobamos cada frame que los pies del player sigan dentro del trigger.
        if (playerController != null && triggerCollider != null)
        {
            Bounds zona = triggerCollider.bounds;
            zona.Expand(0.1f);
            if (!zona.Contains(playerController.transform.position))
            {
                playerController = null;
            }
        }

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