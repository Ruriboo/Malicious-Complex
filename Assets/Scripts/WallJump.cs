using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class WallJump : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallJumpForce = 5f;
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

    void Start()
    {
        controller = GetComponent<ThirdPersonController>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame) bufferCounter = 0.25f;
        else bufferCounter -= Time.deltaTime;

        if (!canWallJump)
        {
            wallJumpTimer += Time.deltaTime;
            if (wallJumpTimer > 0.5f) canWallJump = true;
        }

        CheckForWall();
        StateMachine();
    }

    void CheckForWall()
    {
        if (!canWallJump)
        {
            hasWall = false;
            return;
        }

        Vector3 origin = transform.position + Vector3.up;

        bool rightWall = Physics.Raycast(
            origin,
            transform.right,
            out wallHit,
            wallCheckDistance,
            wallLayer
        );

        bool leftWall = Physics.Raycast(
            origin,
            -transform.right,
            out wallHit,
            wallCheckDistance,
            wallLayer
        );

        bool frontWall = Physics.Raycast(
            origin,
            transform.forward,
            out wallHit,
            wallCheckDistance,
            wallLayer
        );

        hasWall = rightWall || leftWall || frontWall;

        Debug.DrawRay(
            origin,
            transform.right * wallCheckDistance,
            rightWall ? Color.green : Color.red
        );

        Debug.DrawRay(
            origin,
            -transform.right * wallCheckDistance,
            leftWall ? Color.green : Color.red
        );

        Debug.DrawRay(
            origin,
            transform.forward * wallCheckDistance,
            frontWall ? Color.green : Color.red
        );
    }

    bool IsOnTopOfWall()
    {
        Vector3 feet = transform.position + characterController.center;
        feet.y -= characterController.height / 2f - 0.15f;

        return Physics.CheckSphere(
            feet,
            footCheckRadius,
            wallLayer
        );
    }

    bool IsGrounded()
    {
        if (IsOnTopOfWall())
            return true;

        if (Physics.Raycast(
            transform.position,
            Vector3.down,
            minJumpHeight,
            groundLayer))
            return true;

        return false;
    }

    void StateMachine()
    {
        if (IsGrounded())
        {
            if (controller.wallJumping)
                StopWallJump();

            return;
        }

        bool onWall = hasWall && !controller.Grounded && canWallJump;

        if (onWall && !controller.wallJumping)
            StartWallJump();

        if (!onWall && controller.wallJumping)
            StopWallJump();

        if (controller.wallJumping)
        {
            controller.SetVerticalVelocity(-1.2f);

            if (bufferCounter > 0)
            {
                controller.SetVerticalVelocity(8f);

                characterController.Move(
                    wallHit.normal *
                    wallJumpForce *
                    Time.deltaTime *
                    2f
                );

                Debug.Log("WALL JUMP ANIMATION");
                animator.SetTrigger("Walljump");

                bufferCounter = 0;
                canWallJump = false;
                wallJumpTimer = 0;
                StopWallJump();
            }
        }
    }

    void StartWallJump()
    {
        controller.wallJumping = true;
    }

    void StopWallJump()
    {
        controller.wallJumping = false;
    }
}