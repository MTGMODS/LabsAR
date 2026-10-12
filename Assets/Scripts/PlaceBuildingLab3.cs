using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))]
public class PlaceBuildingLab3 : MonoBehaviour
{
    [SerializeField] private GameObject buildingPrefab;

    private ARRaycastManager raycastManager;
    private Camera arCamera;
    private GameObject placedBuilding;
    private bool isDragging;
    private bool touchStartedOnUI;
    private bool twoFingerGestureActive;

    private float startPinchDistance;
    private Vector3 startScale;
    private Vector2 previousFinger0;
    private Vector2 previousFinger1;

    private readonly List<ARRaycastHit> planeHits = new();
    private readonly List<RaycastResult> uiHits = new();

    private void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        arCamera = Camera.main;
    }

    private void Update()
    {
        if (Touchscreen.current == null)
            return;

        var touches = Touchscreen.current.touches;
        int fingerCount = 0;
        Vector2 finger0 = Vector2.zero;
        Vector2 finger1 = Vector2.zero;

        for (int i = 0; i < touches.Count; i++)
        {
            var currentTouch = touches[i];

            if (currentTouch.press.wasPressedThisFrame &&
                IsTouchOverUI(currentTouch.position.ReadValue()))
            {
                touchStartedOnUI = true;
            }

            if (!currentTouch.press.isPressed)
                continue;

            if (fingerCount == 0)
                finger0 = currentTouch.position.ReadValue();
            else if (fingerCount == 1)
                finger1 = currentTouch.position.ReadValue();

            fingerCount++;
        }

        if (fingerCount >= 2)
        {
            if (!touchStartedOnUI && placedBuilding != null)
            {
                if (!twoFingerGestureActive)
                    BeginTwoFingerGesture(finger0, finger1);
                else
                    ContinueTwoFingerGesture(finger0, finger1);
            }

            return;
        }

        if (twoFingerGestureActive)
        {
            twoFingerGestureActive = false;
            isDragging = false;
            return;
        }

        var touch = Touchscreen.current.primaryTouch;
        Vector2 screenPosition = touch.position.ReadValue();

        if (touchStartedOnUI)
        {
            if (touch.press.wasReleasedThisFrame)
                touchStartedOnUI = false;

            return;
        }

        if (touch.press.wasPressedThisFrame)
        {
            if (IsTouchOverUI(screenPosition))
            {
                touchStartedOnUI = true;
                return;
            }

            if (placedBuilding == null)
                PlaceBuildingOnPlane(screenPosition);
            else if (TouchIsOnBuilding(screenPosition))
                isDragging = true;
        }

        if (touch.press.isPressed && isDragging)
            DragBuilding(screenPosition);

        if (touch.press.wasReleasedThisFrame)
            isDragging = false;
    }

    private void BeginTwoFingerGesture(Vector2 finger0, Vector2 finger1)
    {
        twoFingerGestureActive = true;
        isDragging = false;

        startPinchDistance = Vector2.Distance(finger0, finger1);
        startScale = placedBuilding.transform.localScale;

        previousFinger0 = finger0;
        previousFinger1 = finger1;
    }

    private void ContinueTwoFingerGesture(Vector2 finger0, Vector2 finger1)
    {
        float currentDistance = Vector2.Distance(finger0, finger1);

        if (startPinchDistance > 0.01f)
        {
            float scaleFactor = currentDistance / startPinchDistance;
            float newScale = Mathf.Clamp(startScale.x * scaleFactor, 0.025f, 0.15f);
            placedBuilding.transform.localScale = Vector3.one * newScale;
        }

        Vector2 delta0 = finger0 - previousFinger0;
        Vector2 delta1 = finger1 - previousFinger1;

        if (delta0.sqrMagnitude > 1f && delta1.sqrMagnitude > 1f &&
            Vector2.Dot(delta0.normalized, delta1.normalized) > 0.7f)
        {
            float averageHorizontalMovement = (delta0.x + delta1.x) * 0.5f;
            placedBuilding.transform.Rotate(
                Vector3.up,
                averageHorizontalMovement * 0.15f,
                Space.World);
        }

        previousFinger0 = finger0;
        previousFinger1 = finger1;
    }

    private void PlaceBuildingOnPlane(Vector2 screenPosition)
    {
        if (buildingPrefab == null)
        {
            Debug.LogWarning("Building Prefab is not assigned.");
            return;
        }

        if (TryGetPlanePose(screenPosition, out Pose pose))
            placedBuilding = Instantiate(buildingPrefab, pose.position, pose.rotation);
    }

    private void DragBuilding(Vector2 screenPosition)
    {
        if (TryGetPlanePose(screenPosition, out Pose pose))
            placedBuilding.transform.position = pose.position;
    }

    private bool TryGetPlanePose(Vector2 screenPosition, out Pose pose)
    {
        if (raycastManager.Raycast(
                screenPosition,
                planeHits,
                TrackableType.PlaneWithinPolygon))
        {
            pose = planeHits[0].pose;
            return true;
        }

        pose = default;
        return false;
    }

    private bool TouchIsOnBuilding(Vector2 screenPosition)
    {
        if (arCamera == null || placedBuilding == null)
            return false;

        Ray ray = arCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Transform hitTransform = hit.collider.transform;

            return hitTransform == placedBuilding.transform
                || hitTransform.IsChildOf(placedBuilding.transform);
        }

        return false;
    }

    private bool IsTouchOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        var pointer = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        uiHits.Clear();
        EventSystem.current.RaycastAll(pointer, uiHits);

        foreach (RaycastResult hit in uiHits)
        {
            if (hit.module is GraphicRaycaster)
                return true;
        }

        return false;
    }
}