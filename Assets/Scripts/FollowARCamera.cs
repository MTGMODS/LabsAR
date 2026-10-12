using UnityEngine;

public class FollowARCamera : MonoBehaviour
{
    [SerializeField] private Transform arCamera;
    [SerializeField] private Vector3 offset = new Vector3(0f, -0.1f, 1.2f);
    [SerializeField] private float smoothSpeed = 12f;
    [SerializeField] private Vector3 rotationOffsetEuler = Vector3.zero;

    private void Awake()
    {
        if (arCamera == null && Camera.main != null)
            arCamera = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (arCamera == null)
            return;

        Vector3 targetPosition = arCamera.position
                               + arCamera.forward * offset.z
                               + arCamera.right * offset.x
                               + arCamera.up * offset.y;

        float followAmount = Mathf.Clamp01(smoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followAmount);

        Quaternion targetRotation = Quaternion.LookRotation(
            -arCamera.forward,
            arCamera.up) * Quaternion.Euler(rotationOffsetEuler);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            followAmount);
    }
}
