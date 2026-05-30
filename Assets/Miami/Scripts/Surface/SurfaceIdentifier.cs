using UnityEngine;

public enum SurfaceType { Default, Metal, Stone, Wood, Enemy }

public class SurfaceIdentifier : MonoBehaviour
{
    public SurfaceType surfaceType = SurfaceType.Default;
}
