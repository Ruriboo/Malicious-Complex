using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class WallJump : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallJumpForce = 6f;
    [SerializeField] private float wallJumpUpForce = 5f;
    [SerializeField] private float wallCheckDistance = 1.5f;
    [SerializeField] private float minJumpHeight = 1f;
    [SerializeField] private float footCheckRadius = 0.35f;

    private RaycastHit wallHit;
    private bool hasWall;

    private ThirdPersonController controller;
    private CharacterController characterController;
    private Animator animator;

    private bool canWallJump = true;
    private float wallJumpTimer;
    private float bufferCounter;

    private void Start()
    {
        controller = GetComponent<ThirdPersonController>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            bufferCounter = 0.25f;
        }
        else
        {
            bufferCounter -= Time.deltaTime;
        }

        if (!canWallJump)
        {
            wallJumpTimer += Time.deltaTime;

            if (wallJumpTimer > 0.5f)
                canWallJump = true;
        }

        CheckForWall();
        StateMachine();
    }
    private void CheckForWall()
    {
        hasWall = false;

        if (!canWallJump)
            return;

        Vector3 origin = transform.position + Vector3.up;

        if (Physics.Raycast(
            origin,
            transform.right,
            out wallHit,
            wallCheckDistance,
            wallLayer))
        {
            hasWall = true;
            return;
        }

        if (Physics.Raycast(
            origin,
            -transform.right,
            out wallHit,
            wallCheckDistance,
            wallLayer))
        {
            hasWall = true;
            return;
        }

        if (Physics.Raycast(
            origin,
            transform.forward,
            out wallHit,
            wallCheckDistance,
            wallLayer))
        {
            hasWall = true;
        }

        Debug.DrawRay(
            origin,
            transform.right * wallCheckDistance,
            Color.red
        );

        Debug.DrawRay(
            origin,
            -transform.right * wallCheckDistance,
            Color.red
        );

        Debug.DrawRay(
            origin,
            transform.forward * wallCheckDistance,
            Color.red
        );
    }

    private bool IsOnTopOfWall()
    {
        Vector3 feet = transform.position + characterController.center;
        feet.y -= characterController.height / 2f - 0.15f;

        return Physics.CheckSphere(
            feet,
            footCheckRadius,
            wallLayer
        );
    }

    private bool IsGrounded()
    {
        if (IsOnTopOfWall())
            return true;

        if (Physics.Raycast(
            transform.position,
            Vector3.down,
            minJumpHeight,
            groundLayer))
        {
            return true;
        }

        return false;
    }

    private void StateMachine()
    {
        if (IsGrounded())
        {
            if (controller.wallJumping)
                StopWallJump();

            return;
        }

        if (bufferCounter <= 0f)
            return;

        if (!hasWall)
            return;

        if (!canWallJump)
            return;

        PerformWallJump();
    }

    private void PerformWallJump()
    {
        controller.wallJumping = true;

        controller.SetVerticalVelocity(wallJumpUpForce);

        characterController.Move(
            wallHit.normal *
            wallJumpForce *
            Time.deltaTime *
            2f
        );

        if (animator)
            animator.SetTrigger("Walljump");

        bufferCounter = 0f;
        canWallJump = false;
        wallJumpTimer = 0f;

        StopWallJump();
    }

    private void StopWallJump()
    {
        controller.wallJumping = false;
    }
}