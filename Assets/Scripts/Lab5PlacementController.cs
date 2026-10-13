using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class Lab5PlacementController : MonoBehaviour
{
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private GameObject reticlePrefab;
    [SerializeField] private GameObject scanPopup;
    [SerializeField] private Button placeButton;
    [SerializeField] private TMP_Text placementStatusText;
    [SerializeField] private Transform placedObjectsRoot;

    private readonly List<ARRaycastHit> hits = new();
    private GameObject reticleInstance;
    private GameObject selectedPrefab;
    private Pose placementPose;
    private bool placementPoseIsValid;
    private bool selectionOpen;

    private void Awake()
    {
        if (planeManager == null)
            planeManager = GetComponent<ARPlaneManager>();
        if (raycastManager == null)
            raycastManager = GetComponent<ARRaycastManager>();

        if (reticlePrefab != null)
        {
            reticleInstance = Instantiate(reticlePrefab);
            reticleInstance.SetActive(false);
        }
    }

    private void OnEnable()
    {
        placementPoseIsValid = false;
        if (placeButton != null)
            placeButton.interactable = false;
        if (scanPopup != null)
            scanPopup.SetActive(!selectionOpen);
    }

    private void Update()
    {
        UpdatePlacementPose();
        RefreshInterface();
    }

    private void UpdatePlacementPose()
    {
        placementPoseIsValid = false;

        // Не використовуємо старе влучання після втрати трекінгу або відкриття меню.
        if (selectionOpen || ARSession.state != ARSessionState.SessionTracking ||
            planeManager == null || !planeManager.isActiveAndEnabled ||
            raycastManager == null || !raycastManager.isActiveAndEnabled)
            return;

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        hits.Clear();
        if (!raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon) || hits.Count == 0)
            return;

        ARPlane plane = planeManager.GetPlane(hits[0].trackableId);
        if (plane == null || plane.trackingState != TrackingState.Tracking)
            return;

        placementPose = hits[0].pose;
        placementPoseIsValid = true;
        if (reticleInstance != null)
            reticleInstance.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
    }

    private void RefreshInterface()
    {
        bool hasPlanes = ARSession.state == ARSessionState.SessionTracking &&
            planeManager != null && planeManager.isActiveAndEnabled && planeManager.trackables.count > 0;

        // Попап залежить від знайдених площин, а індикатор — від влучання в центрі екрана.
        if (scanPopup != null)
            scanPopup.SetActive(!selectionOpen && !hasPlanes);
        if (reticleInstance != null)
            reticleInstance.SetActive(!selectionOpen && placementPoseIsValid);
        if (placeButton != null)
            placeButton.interactable = !selectionOpen && placementPoseIsValid && selectedPrefab != null && reticleInstance != null;

        if (placementStatusText != null)
        {
            if (!hasPlanes)
                placementStatusText.text = "Шукаємо площину…";
            else if (selectedPrefab == null)
                placementStatusText.text = "Оберіть об’єкт для розміщення";
            else if (!placementPoseIsValid)
                placementStatusText.text = "Наведіть центр екрана на площину";
            else
                placementStatusText.text = "Натисніть «Розмістити»";
        }
    }

    public void SelectPrefab(GameObject prefab)
    {
        selectedPrefab = prefab;
        RefreshInterface();
    }

    public void SetSelectionOpen(bool open)
    {
        selectionOpen = open;
        UpdatePlacementPose();
        RefreshInterface();
    }

    public void PlaceSelectedObject()
    {
        if (!isActiveAndEnabled || selectedPrefab == null || reticleInstance == null)
            return;

        // Повторна перевірка захищає навіть від прямого виклику заблокованої кнопки.
        UpdatePlacementPose();
        RefreshInterface();
        if (!placementPoseIsValid)
            return;

        Instantiate(selectedPrefab, placementPose.position, placementPose.rotation, placedObjectsRoot);
    }

    private void OnDisable()
    {
        placementPoseIsValid = false;
        if (reticleInstance != null)
            reticleInstance.SetActive(false);
        if (placeButton != null)
            placeButton.interactable = false;
        if (scanPopup != null)
            scanPopup.SetActive(false);
    }

    private void OnDestroy()
    {
        if (reticleInstance != null)
            Destroy(reticleInstance);
    }
}
