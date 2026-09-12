using StarterAssets;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class WallJump : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallJumpForce = 5f;
    [SerializeField] private float wallCheckDistance = 1f;
    [SerializeField] private float minJumpHeight = 1f;

    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool wallLeft;
    private bool wallRight;

    private ThirdPersonController controller;
    private CharacterController characterController;

    private bool canWallJump = true;
    private float wallJumpTimer;
    private float bufferCounter;

    void Start()
    {
        controller = GetComponent<ThirdPersonController>();
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame) bufferCounter = 0.25f;
        else bufferCounter -= Time.deltaTime;

        if (!canWallJump) { wallJumpTimer += Time.deltaTime; if (wallJumpTimer > 0.5f) canWallJump = true; }

        CheckForWall();
        StateMachine();
    }

    void CheckForWall()
    {
        if (!canWallJump) { wallLeft = wallRight = false; return; }
        var origin = transform.position + Vector3.up;
        wallRight = Physics.Raycast(origin, transform.right, out rightWallHit, wallCheckDistance, wallLayer);
        wallLeft = Physics.Raycast(origin, -transform.right, out leftWallHit, wallCheckDistance, wallLayer);
    }

    bool AboveGround() => !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, groundLayer);

    void StateMachine()
    {
        bool onWall = (wallLeft || wallRight) && AboveGround() && !controller.Grounded && canWallJump;

        if (onWall && !controller.wallJumping) StartWallJump();
        if (!onWall && controller.wallJumping) StopWallJump();

        if (controller.wallJumping)
        {
            var normal = wallRight ? rightWallHit.normal : leftWallHit.normal;
            controller.SetVerticalVelocity(-1.2f);
            characterController.Move(-normal * Time.deltaTime * 1.5f);

            if (bufferCounter > 0)
            {
                controller.SetVerticalVelocity(8f);
                characterController.Move(normal * wallJumpForce * Time.deltaTime * 2f);
                bufferCounter = 0; canWallJump = false; wallJumpTimer = 0;
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
