using UnityEngine;
using UnityEngine.InputSystem;


[DisallowMultipleComponent]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float sprintSpeed = 6f;
    [SerializeField] private float turnSpeed = 12f;

    [Header("Jump / Gravity")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpCooldown = 0.15f;

    [Header("Animation")]
    [SerializeField] private float speedDamp = 0.1f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    private CharacterController characterController;
    private InputSystem_Actions inputActions;

    private Vector3 verticalVelocity;
    private bool isJumping;
    private float nextJumpTime;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Update()
    {
        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        bool isSprinting = inputActions.Player.Sprint.IsPressed();
        bool jumpPressed = inputActions.Player.Jump.WasPressedThisFrame();

        Vector3 cameraForward = cameraTarget.forward;
        Vector3 cameraRight = cameraTarget.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        Vector3 horizontalMovement = moveDirection * currentSpeed;

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime);
        }

        bool grounded = characterController.isGrounded;

        if (grounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }

        // Сбрасываем только при приземлении (падаем вниз), не на вершине прыжка где velocity.y ≈ 0.
        if (isJumping && grounded && verticalVelocity.y < 0f)
        {
            isJumping = false;
        }

        if (jumpPressed && grounded && !isJumping && Time.time >= nextJumpTime)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            isJumping = true;
            nextJumpTime = Time.time + jumpCooldown;

            if (animator != null)
            {
                animator.ResetTrigger(JumpHash);
                animator.SetTrigger(JumpHash);
            }
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        characterController.Move((horizontalMovement + verticalVelocity) * Time.deltaTime);

        if (animator != null)
        {
            float planarSpeed = new Vector2(characterController.velocity.x, characterController.velocity.z).magnitude;
            animator.SetFloat(SpeedHash, planarSpeed, speedDamp, Time.deltaTime);
        }
    }
}
