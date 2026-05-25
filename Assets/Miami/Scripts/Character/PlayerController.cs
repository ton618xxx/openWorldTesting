using UnityEngine; // Подключаем базовую библиотеку Unity: Transform, Vector3, MonoBehaviour, Mathf и т.д.
using UnityEngine.InputSystem; // Подключаем New Input System, чтобы работать с InputAction и ReadValue.


[DisallowMultipleComponent] // Этот атрибут запрещает добавлять более одного экземпляра этого скрипта на один GameObject в Unity Editor.
public class PlayerController : MonoBehaviour
{
    [Header("References")] // 
    [SerializeField] private Transform cameraTarget; //  

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f; // Скорость обычной ходьбы.
    [SerializeField] private float sprintSpeed = 6f; // Скорость бега при зажатом Sprint.
    [SerializeField] private float turnSpeed = 12f; // Скорость, с которой тело персонажа разворачивается в сторону движения.

    [Header("Jump / Gravity")]
    [SerializeField] private float jumpHeight = 1.2f; // Высота прыжка в Unity-метрах.
    [SerializeField] private float gravity = -20f; // Сила гравитации. Отрицательная, потому что вниз по оси Y.

    private CharacterController characterController; // Ссылка на компонент Character Controller на этом же объекте.
    private InputSystem_Actions inputActions; // Ссылка на сгенерированный Unity класс из InputSystem_Actions.inputactions.

    private Vector3 verticalVelocity; // Отдельно храним вертикальную скорость: падение и прыжок.

    private void Awake() 
    {
        characterController = GetComponent<CharacterController>(); // Берём CharacterController с того же объекта, где висит этот скрипт.

        inputActions = new InputSystem_Actions(); // Создаём экземпляр сгенерированного класса ввода.
    }

    private void OnEnable() // Вызывается, когда объект или компонент включается.
    {
        inputActions.Player.Enable(); // Включаем карту действий Player, чтобы Move/Jump/Sprint начали читать ввод.
    }

    private void OnDisable() // Вызывается, когда объект или компонент выключается.
    {
        inputActions.Player.Disable(); // Отключаем ввод, чтобы не читать его, когда персонаж выключен.
    }

    private void Update() // Update вызывается каждый кадр.
    {
        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>(); // Читаем WASD/стик. X = влево/вправо, Y = назад/вперёд.

        bool isSprinting = inputActions.Player.Sprint.IsPressed(); // Проверяем, зажата ли кнопка Sprint.

        bool jumpPressed = inputActions.Player.Jump.WasPressedThisFrame(); // Проверяем, был ли Jump нажат именно в этот кадр.

        Vector3 cameraForward = cameraTarget.forward; // Берём направление "вперёд" от CameraTarget.
        Vector3 cameraRight = cameraTarget.right; // Берём направление "вправо" от CameraTarget.

        cameraForward.y = 0f; // Убираем вертикаль, чтобы персонаж не пытался идти вверх/вниз из-за наклона камеры.
        cameraRight.y = 0f; // Убираем вертикаль у правого направления по той же причине.

        cameraForward.Normalize(); // Нормализуем, чтобы длина вектора была 1 и скорость не зависела от наклона.
        cameraRight.Normalize(); // Нормализуем правый вектор.

        Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x; // Превращаем ввод WASD в направление движения относительно камеры.

        if (moveDirection.sqrMagnitude > 1f) // Если диагональное движение получилось длиннее 1...
        {
            moveDirection.Normalize(); // ...нормализуем, чтобы по диагонали персонаж не бежал быстрее.
        }

        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed; // Если Sprint зажат, берём скорость бега, иначе скорость ходьбы.

        Vector3 horizontalMovement = moveDirection * currentSpeed; // Считаем горизонтальную скорость движения.

        if (moveDirection.sqrMagnitude > 0.01f) // Если игрок реально двигается, а не стоит на месте...
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection); // Создаём поворот, чтобы персонаж смотрел в сторону движения.

            transform.rotation = Quaternion.Slerp( // Плавно поворачиваем Character к нужному направлению.
                transform.rotation, // Текущий поворот персонажа.
                targetRotation, // Целевой поворот в сторону движения.
                turnSpeed * Time.deltaTime // Насколько быстро поворачиваемся, с учётом времени кадра.
            );
        }

        if (characterController.isGrounded && verticalVelocity.y < 0f) // Если персонаж на земле и сейчас падает вниз...
        {
            verticalVelocity.y = -2f; // Прижимаем его к земле маленькой отрицательной скоростью, чтобы isGrounded работал стабильнее.
        }

        if (jumpPressed && characterController.isGrounded) // Если нажали прыжок и персонаж стоит на земле...
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity); // Считаем начальную скорость прыжка по физической формуле.
        }

        verticalVelocity.y += gravity * Time.deltaTime; // Каждый кадр добавляем гравитацию к вертикальной скорости.

        Vector3 finalMovement = horizontalMovement + verticalVelocity; // Складываем горизонтальное движение и вертикальное падение/прыжок.

        characterController.Move(finalMovement * Time.deltaTime); // Двигаем CharacterController с учётом времени кадра.
    }
}
