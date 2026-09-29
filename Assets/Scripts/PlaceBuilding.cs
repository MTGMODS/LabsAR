using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))]
public class PlaceBuilding : MonoBehaviour
{
    [SerializeField] private GameObject buildingPrefab;

    private ARRaycastManager raycastManager;
    private GameObject placedBuilding;

    private readonly List<ARRaycastHit> hits = new();
    private readonly List<RaycastResult> uiHits = new();

    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
        if (Touchscreen.current == null || buildingPrefab == null)
            return;

        var touch = Touchscreen.current.primaryTouch;

        if (!touch.press.wasPressedThisFrame)
            return;

        Vector2 screenPosition = touch.position.ReadValue();

        if (IsTouchOverUI(screenPosition))
            return;

        if (!raycastManager.Raycast(
                screenPosition,
                hits,
                TrackableType.PlaneWithinPolygon))
            return;

        Pose pose = hits[0].pose;

        if (placedBuilding == null)
        {
            placedBuilding = Instantiate(
                buildingPrefab, pose.position, pose.rotation);
        }
        else
        {
            placedBuilding.transform.SetPositionAndRotation(
                pose.position, pose.rotation);
        }
    }

    private bool IsTouchOverUI(Vector2 position)
    {
        if (EventSystem.current == null)
            return false;

        var pointer = new PointerEventData(EventSystem.current)
        {
            position = position
        };

        uiHits.Clear();
        EventSystem.current.RaycastAll(pointer, uiHits);

        foreach (var hit in uiHits)
        {
            if (hit.module is GraphicRaycaster)
                return true;
        }

        return false;
    }
}