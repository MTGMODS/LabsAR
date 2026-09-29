using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARPlaneManager))]
public class PlaneVisibility : MonoBehaviour
{
    private ARPlaneManager planeManager;
    private bool planesVisible = true;

    void Awake()
    {
        planeManager = GetComponent<ARPlaneManager>();
    }

    public void TogglePlanes()
    {
        planesVisible = !planesVisible;
        ApplyVisibility();
    }

    void LateUpdate()
    {
        ApplyVisibility();
    }

    private void ApplyVisibility()
    {
        foreach (var plane in planeManager.trackables)
        {
            if (plane.TryGetComponent<MeshRenderer>(out var meshRenderer))
            {
                meshRenderer.forceRenderingOff = !planesVisible;
            }
        }
    }
}