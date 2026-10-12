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
    [SerializeField] private Material alternateMaterial;
    [SerializeField] private float rotationAnimationSpeed = 45f;

    private ARRaycastManager raycastManager;
    private Camera arCamera;
    private GameObject placedBuilding;
    private Renderer buildingRenderer;
    private Material originalMaterial;

    private bool isDragging;
    private bool touchStartedOnUI;
    private bool twoFingerGestureActive;
    private bool touchStartedOnBuilding;
    private bool touchMoved;
    private bool usingAlternateMaterial;
    private bool animationActive;

    private Vector2 touchStartPosition;
    private float touchStartTime;
    private int tapCount;
    private float lastTapTime;

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
        ResolveTapSequence();

        if (animationActive && placedBuilding != null)
        {
            placedBuilding.transform.Rotate(
                Vector3.up,
                rotationAnimationSpeed * Time.deltaTime,
                Space.World);
        }

        if (Touchscreen.current == null)
            return;

        var touches = Touchscreen.current.touches;
        int fingerCount = 0;
        Vector2 finger0 = Vector2.zero;
        Vector2 finger1 = Vector2.zero;

        for (int i = 0; i < touches.Count; i++)
        {
            var currentTouch = touches[i];
            Vector2 position = currentTouch.position.ReadValue();

            if (currentTouch.press.wasPressedThisFrame && IsTouchOverUI(position))
                touchStartedOnUI = true;

            if (!currentTouch.press.isPressed)
                continue;

            if (fingerCount == 0)
                finger0 = position;
            else if (fingerCount == 1)
                finger1 = position;

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
            touchStartedOnBuilding = false;
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
            {
                PlaceBuildingOnPlane(screenPosition);
            }
            else if (TouchIsOnBuilding(screenPosition))
            {
                isDragging = true;
                touchStartedOnBuilding = true;
                touchMoved = false;
                touchStartPosition = screenPosition;
                touchStartTime = Time.time;
            }
        }

        if (touch.press.isPressed && isDragging)
        {
            if (Vector2.Distance(touchStartPosition, screenPosition) > 30f)
                touchMoved = true;

            if (touchMoved)
                DragBuilding(screenPosition);
        }

        if (touch.press.wasReleasedThisFrame)
        {
            if (touchStartedOnBuilding &&
                !touchMoved &&
                Time.time - touchStartTime < 0.35f)
            {
                RegisterTap();
            }

            isDragging = false;
            touchStartedOnBuilding = false;
            touchMoved = false;

            if (fingerCount == 0)
                touchStartedOnUI = false;
        }
    }

    private void RegisterTap()
    {
        if (Time.time - lastTapTime > 0.55f)
            tapCount = 0;

        tapCount++;
        lastTapTime = Time.time;
    }

    private void ResolveTapSequence()
    {
        if (tapCount == 0 || Time.time - lastTapTime < 0.55f)
            return;

        if (tapCount == 2)
            ToggleMaterial();
        else if (tapCount >= 3)
            animationActive = !animationActive;

        tapCount = 0;
    }

    private void ToggleMaterial()
    {
        if (alternateMaterial == null || buildingRenderer == null || originalMaterial == null)
        {
            Debug.LogWarning("Assign the Alternate Material in the Inspector.");
            return;
        }

        usingAlternateMaterial = !usingAlternateMaterial;
        buildingRenderer.material =
            usingAlternateMaterial ? alternateMaterial : originalMaterial;
    }

    private void BeginTwoFingerGesture(Vector2 finger0, Vector2 finger1)
    {
        twoFingerGestureActive = true;
        isDragging = false;
        touchStartedOnBuilding = false;

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

        if (delta0.sqrMagnitude > 1f &&
            delta1.sqrMagnitude > 1f &&
            Vector2.Dot(delta0.normalized, delta1.normalized) > 0.7f)
        {
            float averageMovement = (delta0.x + delta1.x) * 0.5f;
            placedBuilding.transform.Rotate(
                Vector3.up,
                averageMovement * 0.15f,
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
        {
            placedBuilding = Instantiate(buildingPrefab, pose.position, pose.rotation);
            buildingRenderer = placedBuilding.GetComponentInChildren<Renderer>();

            if (buildingRenderer != null)
                originalMaterial = buildingRenderer.material;
        }
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

            return hitTransform == placedBuilding.transform ||
                   hitTransform.IsChildOf(placedBuilding.transform);
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