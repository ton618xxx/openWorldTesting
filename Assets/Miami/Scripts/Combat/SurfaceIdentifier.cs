using UnityEngine;

public enum SurfaceType { Default, Metal, Stone, Wood, Sand, Flesh }

public class SurfaceIdentifier : MonoBehaviour
{
    public SurfaceType surfaceType = SurfaceType.Default;
}
