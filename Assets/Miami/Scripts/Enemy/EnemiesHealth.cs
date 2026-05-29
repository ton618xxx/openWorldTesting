using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    private void Awake() => currentHealth = maxHealth;

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        currentHealth -= amount;
        Debug.Log($"{name} получил {amount} урона. Осталось: {currentHealth}");
        if (currentHealth <= 0f) Die();
    }

    private void Die()
    {
        // позже: анимация смерти, рагдолл и т.д.
        Destroy(gameObject);// потом можно отключать коллайдеры, анимацию и т.д., а не сразу уничтожать объект
    }
}
