using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 90f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private LayerMask hitLayers;

    private float damage;
    private Vector3 direction;

    // Вызывается из WeaponController сразу после создания пули
    public void Launch(Vector3 dir, float dmg)
    {
        direction = dir.normalized;
        damage = dmg;
        transform.forward = direction;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;

        // Луч на длину кадрового шага — чтобы пуля не "проскакивала" тонкие объекты
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, step, hitLayers))
        {
            HandleHit(hit);
            return;
        }

        transform.position += direction * step;
    }

    private void HandleHit(RaycastHit hit)
    {
        // Урон, если попали в то, что умеет его получать
        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
        target?.TakeDamage(damage, hit.point, hit.normal);

        // Эффект попадания спросим у менеджера эффектов (см. Задачу 6)
        ImpactManager.Instance?.SpawnImpact(hit);

        Destroy(gameObject);
    }
}
