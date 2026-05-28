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
    [SerializeField] private float aimWalkSpeed = 2.5f;
    [SerializeField] private float turnSpeed = 12f;

    [Header("Jump / Gravity")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpCooldown = 0.15f;

    [Header("Animation")]
    [SerializeField] private float speedDamp = 0.1f;

    [Header("Aiming")]
    [SerializeField] private GameObject aimCamera;
    [SerializeField] private float aimTurnSpeed = 20f;
    [SerializeField] private int aimPriority = 20;
    [SerializeField] private int defaultPriority = 5;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    private CharacterController characterController;
    private InputSystem_Actions inputActions;
    private Unity.Cinemachine.CinemachineCamera aimVcam;
    private Camera mainCamera;
    private Transform myTransform;

    private Vector3 verticalVelocity;
    private bool isJumping;
    private float nextJumpTime;
    private bool isAiming;
    private int currentVcamPriority;

    private void Awake()
    {
        myTransform = transform;
        if (!TryGetComponent(out characterController))
        {
            Debug.LogError("CharacterController missing on Player", this);
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        inputActions = new InputSystem_Actions();
        
        if (aimCamera != null)
        {
            if (aimCamera.TryGetComponent(out aimVcam))
            {
                // Убеждаемся, что камера активна, чтобы избежать лага при первой активации объекта
                // Но делаем это только если она еще не активна
                if (!aimCamera.activeSelf) aimCamera.SetActive(true);
                
                currentVcamPriority = defaultPriority;
                aimVcam.Priority.Value = defaultPriority;
            }
        }

        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        inputActions?.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Player.Disable();
    }

    private void OnDestroy()
    {
        inputActions?.Dispose();
    }

    private void Start()
    {
        // Принудительно выставляем приоритет в Start, чтобы Cinemachine точно его подхватил
        if (aimVcam != null)
        {
            aimVcam.Priority.Value = defaultPriority;
            Debug.Log($"[CameraDebug] Initialized AimCamera Priority to {defaultPriority}");
        }
    }

    private void Update()
    {
        // Считываем ввод
        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        bool isSprinting = inputActions.Player.Sprint.IsPressed();
        bool jumpPressed = inputActions.Player.Jump.WasPressedThisFrame();
        
        bool grounded = characterController.isGrounded;
        
        // Прицеливание разрешено только на земле и если мы не прыгаем
        bool newIsAiming = inputActions.Player.Aim.IsPressed() && grounded && !isJumping && !jumpPressed;

        if (newIsAiming != isAiming)
        {
            isAiming = newIsAiming;
            // Переключаем приоритет только при изменении состояния
            if (aimVcam != null)
            {
                currentVcamPriority = isAiming ? aimPriority : defaultPriority;
                aimVcam.Priority.Value = currentVcamPriority;
            }
            
            if (animator != null)
            {
                animator.SetBool("IsAiming", isAiming);
            }
        }

        Vector3 cameraForward = cameraTarget.forward;
        Vector3 cameraRight = cameraTarget.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;

        // Это предотвратит ускорение при беге по диагонали
        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        if (isAiming)
        {
            // Если прицеливаемся — персонаж всегда смотрит туда, куда смотрит камера
            Vector3 targetLook = cameraTarget.forward;
            targetLook.y = 0; 
            Quaternion targetRotation = Quaternion.LookRotation(targetLook);

            myTransform.rotation = Quaternion.Slerp(
                myTransform.rotation,
                targetRotation,
                aimTurnSpeed * Time.deltaTime);
        }
        else if (moveDirection.sqrMagnitude > 0.01f)
        {
            // Старая логика: поворот в сторону бега
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            myTransform.rotation = Quaternion.Slerp(
                myTransform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime);
        }

        float currentSpeed = isAiming ? aimWalkSpeed : (isSprinting ? sprintSpeed : walkSpeed);
        Vector3 horizontalMovement = moveDirection * currentSpeed;

        if (grounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }

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
            // Порог, чтобы ноги не дергались при микро-движениях
            if (planarSpeed < 0.1f) planarSpeed = 0f;
            animator.SetFloat(SpeedHash, planarSpeed, speedDamp, Time.deltaTime);
        }
    }
    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || cameraTarget == null) return;

        if (isAiming)
        {
            // Используем направление cameraTarget напрямую для исключения лага в 1 кадр
            Vector3 aimAtPos = cameraTarget.position + cameraTarget.forward * 20f;

            // Веса: Тело (Body) = 0.2 для мягкости, Голова (Head) = 1.0 для точности, Clamp = 0.5
            animator.SetLookAtWeight(1f, 0.2f, 1f, 1f, 0.5f);
            animator.SetLookAtPosition(aimAtPos);

            // Поворачиваем руку за целью
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);
            animator.SetIKRotation(AvatarIKGoal.RightHand, cameraTarget.rotation);
        }
        else
        {
            animator.SetLookAtWeight(0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
        }
    }
}
