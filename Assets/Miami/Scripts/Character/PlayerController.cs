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
    [SerializeField] private GameObject crosshair;
    [SerializeField] private float aimTurnSpeed = 30f; // Увеличили для большей отзывчивости
    [SerializeField] private int aimPriority = 20;
    [SerializeField] private int defaultPriority = 5;
    [SerializeField] private Vector3 aimHandRotationOffset = Vector3.zero;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int InputXHash = Animator.StringToHash("InputX");
    private static readonly int InputYHash = Animator.StringToHash("InputY");

    private CharacterController characterController;
    private InputSystem_Actions inputActions;
    private Unity.Cinemachine.CinemachineCamera aimVcam;
    private Transform myTransform;

    private Vector3 verticalVelocity;
    private bool isJumping;
    private float nextJumpTime;
    private bool isAiming;
    private float airTime;
    public bool IsAiming => isAiming;
    private float ikWeight = 0f; // Плавный переход для IK

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
                if (!aimCamera.activeSelf) aimCamera.SetActive(true);
                aimVcam.Priority.Value = defaultPriority;
            }
        }

        if (crosshair != null)
        {
            crosshair.SetActive(false);
        }
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
        if (aimVcam != null)
        {
            aimVcam.Priority.Value = defaultPriority;
        }
    }

    private void Update()
    {
        // Считываем ввод
        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        bool isSprinting = inputActions.Player.Sprint.IsPressed();
        bool jumpPressed = inputActions.Player.Jump.WasPressedThisFrame();
        
        bool grounded = characterController.isGrounded;
        
        // Накапливаем время в воздухе, чтобы избежать мерцания прицела на кочках
        if (grounded) airTime = 0f;
        else airTime += Time.deltaTime;

        bool isStableGrounded = grounded || airTime < 0.15f;

        // Прицеливание разрешено только на стабильной поверхности и если мы не прыгаем
        bool newIsAiming = inputActions.Player.Aim.IsPressed() && isStableGrounded && !isJumping && !jumpPressed;

        if (newIsAiming != isAiming)
        {
            isAiming = newIsAiming;
            // Переключаем приоритет только при изменении состояния
            if (aimVcam != null)
            {
                aimVcam.Priority.Value = isAiming ? aimPriority : defaultPriority;
            }
            
            if (animator != null)
            {
                animator.SetBool("IsAiming", isAiming);
            }

            if (crosshair != null)
            {
                crosshair.SetActive(isAiming);
            }
        }

        // Плавное изменение веса IK
        ikWeight = Mathf.Lerp(ikWeight, isAiming ? 1f : 0f, Time.deltaTime * 10f);

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

            if (isAiming)
            {
                // Переводим мировую скорость в локальную для 2D Blend Tree
                Vector3 localVelocity = myTransform.InverseTransformDirection(characterController.velocity);
                
                // Делим на walkSpeed, чтобы получить значения в районе [-1, 1] для аниматора
                float x = localVelocity.x / walkSpeed;
                float y = localVelocity.z / walkSpeed;

                animator.SetFloat(InputXHash, x, speedDamp, Time.deltaTime);
                animator.SetFloat(InputYHash, y, speedDamp, Time.deltaTime);
            }
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || cameraTarget == null) return;

        if (ikWeight > 0.01f)
        {
            // Точка, куда мы смотрим (далеко впереди по направлению прицела)
            Vector3 lookAtPos = cameraTarget.position + cameraTarget.forward * 20f;

            // Настройка весов LookAt: Голова и глаза смотрят на цель, тело немного доворачивается
            animator.SetLookAtWeight(ikWeight, 0.3f, 1f, 1f, 0.5f);
            animator.SetLookAtPosition(lookAtPos);

            // Направляем правую руку вперед по линии прицела со смещением
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, ikWeight);
            animator.SetIKRotation(AvatarIKGoal.RightHand, cameraTarget.rotation * Quaternion.Euler(aimHandRotationOffset));
        }
        else
        {
            animator.SetLookAtWeight(0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
        }
    }
}
