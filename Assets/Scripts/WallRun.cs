using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class WallRun : MonoBehaviour
{
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private Collider wallRunDetector;
    [SerializeField] private float arcHeight = 2.5f;
    [SerializeField] private float minimumSpeed = 0.5f;

    private ThirdPersonController controller;
    private CharacterController characterController;
    private Animator animator;

    private WallRunWall currentWall;

    private bool wallRunning;
    private int animIDWallRunning;

    private Vector3 wallNormal;
    private Vector3 wallDirection;

    private float wallRunTimer;
    private float startHeight;

    public bool IsWallRunning => wallRunning;

    private void Start()
    {
        controller = GetComponent<ThirdPersonController>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        animIDWallRunning = Animator.StringToHash("IsWallRunning");

        if (animator)
            animator.applyRootMotion = false;
    }

    private void Update()
    {
        if (wallRunning)
            UpdateWallRun();
        else
            TryStartWallRun();
    }

    private void TryStartWallRun()
    {
        if (controller.Grounded)
            return;

        if (Keyboard.current == null ||
            !Keyboard.current.spaceKey.isPressed)
            return;

        Vector3 velocity = characterController.velocity;
        velocity.y = 0f;

        if (velocity.magnitude < minimumSpeed)
            return;

        if (!FindWall())
        {
            Debug.Log("NO ENCUENTRA WALLRUN");
            return;
        }
        Debug.Log("ENCUENTRA WALLRUN");
        StartWallRun();
    }

    private bool FindWall()
    {
        if (wallRunDetector == null)
            return false;

        Bounds bounds = wallRunDetector.bounds;

        Collider[] hits = Physics.OverlapBox(
            bounds.center,
            bounds.extents,
            wallRunDetector.transform.rotation,
            wallLayer,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            WallRunWall wall = hit.GetComponentInParent<WallRunWall>();

            if (wall == null)
                continue;

            Vector3 closestPoint = hit.ClosestPoint(transform.position);
            Vector3 normal = transform.position - closestPoint;

            normal.y = 0f;

            if (normal.sqrMagnitude < 0.001f)
                continue;

            currentWall = wall;
            wallNormal = normal.normalized;

            wallDirection = Vector3.Cross(Vector3.up, wallNormal);

            if (Vector3.Dot(wallDirection, transform.forward) < 0f)
                wallDirection = -wallDirection;

            return true;
        }

        currentWall = null;
        return false;
    }

    private void StartWallRun()
    {
        wallRunning = true;
        wallRunTimer = 0f;
        startHeight = transform.position.y;

        float side = Vector3.Dot(transform.right, wallNormal);

        if (animator)
        {
            if (side > 0f)
            {
                animator.SetBool("IsWallRunningLeft", true);
                animator.SetBool("IsWallRunning", false);
            }
            else
            {
                animator.SetBool("IsWallRunningLeft", false);
                animator.SetBool("IsWallRunning", true);
            }
        }
}
    private void UpdateWallRun()
    {
        if (Keyboard.current == null ||
            !Keyboard.current.spaceKey.isPressed)
        {
            StopWallRun();
            return;
        }

        if (controller.Grounded || !FindWall())
        {
            StopWallRun();
            return;
        }

        wallRunTimer += Time.deltaTime;

        float duration = currentWall.WallRunDuration;
        float speed = currentWall.WallRunSpeed;

        float t = Mathf.Clamp01(wallRunTimer / duration);
        float arc = Mathf.Sin(t * Mathf.PI);

        float targetHeight = startHeight + arc * arcHeight;
        float verticalSpeed =
            (targetHeight - transform.position.y) / Time.deltaTime;

        Vector3 movement =
            wallDirection * speed +
            Vector3.up * verticalSpeed;

        characterController.Move(movement * Time.deltaTime);

        if (t >= 1f)
            StopWallRun();
    }

    private void StopWallRun()
    {
        if (!wallRunning)
            return;

        wallRunning = false;
        currentWall = null;

        if (animator)
        {
            animator.SetBool("IsWallRunning", false);
            animator.SetBool("IsWallRunningLeft", false);
        }
    
}
}