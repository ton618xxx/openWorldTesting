using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("Настройки оружия")]
    [SerializeField] private float fireRate = 0.1f;
    [SerializeField] private float range = 100f;
    [SerializeField] private LayerMask hitLayers;

    [Header("Ссылки")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private LineRenderer tracerPrefab;
    [SerializeField] private Transform cameraTransform;

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

        // Raycast идет из центра камеры вперед
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, range, hitLayers))
        {
            targetPoint = hit.point;
            SpawnHitEffect(hit);
        }
        else
        {
            // Если никуда не попали, пуля летит в "пустоту" на макс. дистанцию
            targetPoint = ray.GetPoint(range);
        }

        SpawnTracer(targetPoint);
    }

    private void SpawnHitEffect(RaycastHit hit)
    {
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        }
    }

    private void SpawnTracer(Vector3 targetPoint)
    {
        if (tracerPrefab == null || muzzlePoint == null) return;

        LineRenderer tracer = Instantiate(tracerPrefab, muzzlePoint.position, Quaternion.identity);
        tracer.SetPosition(0, muzzlePoint.position);
        tracer.SetPosition(1, targetPoint);
        Destroy(tracer.gameObject, 0.04f); // Трассер живет очень мало
    }
}
