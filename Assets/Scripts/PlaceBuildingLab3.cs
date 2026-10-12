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
    [SerializeField] private Camera arCamera;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private Material[] appearanceMaterials;
    [SerializeField] private float rotationAnimationSpeed = 45f;

    private GameObject placedBuilding;
    private Renderer buildingRenderer;
    private int currentMaterialIndex;

    private bool isDragging;
    private bool touchStartedOnUI;
    private bool twoFingerGestureActive;
    private bool touchStartedOnBuilding;
    private bool touchMoved;
    private bool animationActive;

    private Vector2 touchStartPosition;
    private float touchStartTime;
    private int tapCount;
    private float lastTapTime;

    private float startPinchDistance;
    private Vector3 startScale;
    private Vector2 previousFinger0;
    private Vector2 previousFinger1;

    private TwoFingerMode twoFingerMode = TwoFingerMode.Undetermined;

    private readonly List<ARRaycastHit> planeHits = new();
    private readonly List<RaycastResult> uiHits = new();

    private enum TwoFingerMode
    {
        Undetermined,
        Pinch,
        Rotate,
        Ignore
    }

    private void Awake()
    {
        if (raycastManager == null)
            raycastManager = GetComponent<ARRaycastManager>();

        if (arCamera == null)
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
            twoFingerMode = TwoFingerMode.Undetermined;
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

        // Новий Input System: wasPressedThisFrame відповідає TouchPhase.Began.
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

        // isPressed та зміна позиції відповідають переміщенню пальця.
        if (touch.press.isPressed && isDragging)
        {
            if (Vector2.Distance(touchStartPosition, screenPosition) > 30f)
                touchMoved = true;

            if (touchMoved)
                DragBuilding(screenPosition);
        }

        // wasReleasedThisFrame відповідає TouchPhase.Ended.
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
        if (buildingRenderer == null ||
            appearanceMaterials == null ||
            appearanceMaterials.Length < 2)
        {
            Debug.LogWarning(
                "Assign at least two materials in Appearance Materials.");
            return;
        }

        currentMaterialIndex =
            (currentMaterialIndex + 1) % appearanceMaterials.Length;

        buildingRenderer.material = appearanceMaterials[currentMaterialIndex];
    }

    private void BeginTwoFingerGesture(Vector2 finger0, Vector2 finger1)
    {
        twoFingerGestureActive = true;
        twoFingerMode = TwoFingerMode.Undetermined;
        isDragging = false;
        touchStartedOnBuilding = false;

        startPinchDistance = Vector2.Distance(finger0, finger1);
        startScale = placedBuilding.transform.localScale;
        previousFinger0 = finger0;
        previousFinger1 = finger1;
    }

    private void ContinueTwoFingerGesture(Vector2 finger0, Vector2 finger1)
    {
        Vector2 delta0 = finger0 - previousFinger0;
        Vector2 delta1 = finger1 - previousFinger1;

        if (twoFingerMode == TwoFingerMode.Undetermined &&
            delta0.sqrMagnitude > 1f &&
            delta1.sqrMagnitude > 1f)
        {
            // Dot близький до 1: пальці рухаються в одному напрямку — обертання.
            float directionDot =
                Vector2.Dot(delta0.normalized, delta1.normalized);

            if (directionDot > 0.9f)
                twoFingerMode = TwoFingerMode.Rotate;
            // Від’ємний Dot: пальці рухаються назустріч/у різні боки — pinch.
            else if (directionDot < -0.5f)
                twoFingerMode = TwoFingerMode.Pinch;
            else
                twoFingerMode = TwoFingerMode.Ignore;
        }

        if (twoFingerMode == TwoFingerMode.Pinch)
        {
            float currentDistance = Vector2.Distance(finger0, finger1);

            if (startPinchDistance > 0.01f && startScale.x > 0.001f)
            {
                float scaleFactor = currentDistance / startPinchDistance;
                float newScaleX =
                    Mathf.Clamp(startScale.x * scaleFactor, 0.025f, 0.15f);

                placedBuilding.transform.localScale =
                    startScale * (newScaleX / startScale.x);
            }
        }
        else if (twoFingerMode == TwoFingerMode.Rotate)
        {
            float averageDeltaX = (delta0.x + delta1.x) * 0.5f;

            placedBuilding.transform.Rotate(
                Vector3.up,
                averageDeltaX * 0.15f,
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
            placedBuilding =
                Instantiate(buildingPrefab, pose.position, pose.rotation);

            buildingRenderer = placedBuilding.GetComponentInChildren<Renderer>();
            currentMaterialIndex = 0;

            if (buildingRenderer != null &&
                appearanceMaterials != null &&
                appearanceMaterials.Length > 0)
            {
                buildingRenderer.material = appearanceMaterials[0];
            }
        }
    }

    private void DragBuilding(Vector2 screenPosition)
    {
        if (TryGetPlanePose(screenPosition, out Pose pose))
            placedBuilding.transform.position = pose.position;
    }

    private bool TryGetPlanePose(Vector2 screenPosition, out Pose pose)
    {
        if (raycastManager != null &&
            raycastManager.Raycast(
                screenPosition,
                planeHits,
                TrackableType.Planes))
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