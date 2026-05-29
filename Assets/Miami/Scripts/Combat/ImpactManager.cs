using System;
using System.Collections.Generic;
using UnityEngine;

public class ImpactManager : MonoBehaviour
{
    public static ImpactManager Instance { get; private set; }

    [Serializable]
    public struct ImpactEntry
    {
        public SurfaceType surface;
        public GameObject impactEffect;   // particle (искры/кровь)
        public GameObject bulletHole;     // декаль-дырка (может быть пустым)
    }

    [SerializeField] private ImpactEntry[] impacts;
    [SerializeField] private float effectLifeTime = 5f;
    [SerializeField] private float holeLifeTime = 20f;

    private readonly Dictionary<SurfaceType, ImpactEntry> map = new();

    private void Awake()
    {
        Instance = this;
        foreach (var e in impacts) map[e.surface] = e;
    }

    public void SpawnImpact(RaycastHit hit)
    {
        SurfaceType type = SurfaceType.Default;
        var id = hit.collider.GetComponentInParent<SurfaceIdentifier>();
        if (id != null) type = id.surfaceType;

        if (!map.TryGetValue(type, out var entry))
            map.TryGetValue(SurfaceType.Default, out entry);

        Quaternion rot = Quaternion.LookRotation(hit.normal);

        if (entry.impactEffect != null)
        {
            var fx = Instantiate(entry.impactEffect, hit.point, rot);
            Destroy(fx, effectLifeTime);
        }

        if (entry.bulletHole != null)
        {
            var hole = Instantiate(entry.bulletHole,
                hit.point + hit.normal * 0.01f,
                Quaternion.LookRotation(-hit.normal));
            Destroy(hole, holeLifeTime);
        }
    }
}
