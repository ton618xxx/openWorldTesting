using UnityEngine;

// Восстановленный скрипт из Legacy Particle Pack.
// На префабах эффектов (BulletImpact*, и т.п.) хранится поле lifeTime,
// поэтому имя поля менять нельзя — оно сериализовано в префабах.
public class ParticleEffectLifetime : MonoBehaviour
{
    public float lifeTime = 5f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}
