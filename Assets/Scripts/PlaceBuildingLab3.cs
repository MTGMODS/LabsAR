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

        var touch = Touchscreen.current.primaryTouch;
        Vector2 screenPosition = touch.position.ReadValue();

        if (touch.press.wasPressedThisFrame)
        {
            if (IsTouchOverUI(screenPosition))
            {
                touchStartedOnUI = true;
                return;
            }

            touchStartedOnUI = false;

            if (placedBuilding == null)
            {
                PlaceBuildingOnPlane(screenPosition);
            }
            else if (TouchIsOnBuilding(screenPosition))
            {
                isDragging = true;
            }
        }

        if (touchStartedOnUI)
        {
            if (touch.press.wasReleasedThisFrame)
                touchStartedOnUI = false;

            return;
        }

        if (touch.press.isPressed && isDragging)
            DragBuilding(screenPosition);

        if (touch.press.wasReleasedThisFrame)
            isDragging = false;
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