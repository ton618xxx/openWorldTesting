using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("Настройки оружия")]
    [SerializeField] private float fireRate = 0.1f;
    [SerializeField] private float range = 100f;
    [SerializeField] private LayerMask hitLayers;


    [Header("Bullet")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private float bulletDamage = 25f;  


    [Header("Ссылки")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private ParticleSystem shellEjectEffect; 

    private float nextFireTime;
    private InputSystem_Actions inputActions;
    private bool isAttacking;
    private PlayerController playerController;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        playerController = GetComponent<PlayerController>();

        // Если камера не назначена, берем главную
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        // Подписываемся на ввод из Input System
        inputActions.Player.Attack.started += ctx => isAttacking = true;
        inputActions.Player.Attack.canceled += ctx => isAttacking = false;
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Update()
    {
        // Стреляем только если нажата кнопка, прошло время отката и игрок целится
        if (isAttacking && playerController.IsAiming && Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + fireRate;
        }
    }

    private void Fire()
    {
        if (muzzleFlash != null) muzzleFlash.Play();
        if (shellEjectEffect != null) shellEjectEffect.Play();
        // 1) Куда целимся: луч из центра камеры
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, range, hitLayers)
            ? hit.point
            : ray.GetPoint(range);
        // 2) Направление от дула к точке прицела
        Vector3 dir = (targetPoint - muzzlePoint.position).normalized;
        // 3) Создаём пулю-объект
        if (bulletPrefab != null && muzzlePoint != null)
        {
            Bullet bullet = Instantiate(bulletPrefab, muzzlePoint.position, Quaternion.LookRotation(dir));
            bullet.Launch(dir, bulletDamage);
        }
    }
}
