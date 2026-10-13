using System;
using TMPro;
using UnityEngine;

public class Lab5ObjectSelector : MonoBehaviour
{
    [Serializable]
    public class ObjectOption
    {
        public string displayName;
        public GameObject prefab;
    }

    [SerializeField] private Lab5PlacementController placementController;
    [SerializeField] private GameObject cameraPanel;
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private TMP_Text selectedObjectText;
    [SerializeField] private ObjectOption[] objects = Array.Empty<ObjectOption>();

    private void Start()
    {
        // Спочатку об’єкт не вибраний, тому кнопка розміщення заблокована.
        if (placementController != null)
            placementController.SelectPrefab(null);
        if (selectedObjectText != null)
            selectedObjectText.text = "Об’єкт не вибрано";
        CloseSelection();
    }

    public void OpenSelection()
    {
        if (placementController != null)
            placementController.SetSelectionOpen(true);
        if (cameraPanel != null)
            cameraPanel.SetActive(false);
        if (selectionPanel != null)
            selectionPanel.SetActive(true);
    }

    public void CloseSelection()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(false);
        if (cameraPanel != null)
            cameraPanel.SetActive(true);
        if (placementController != null)
            placementController.SetSelectionOpen(false);
    }

    // On Click карток передає індекс 0, 1 або 2 з масиву в інспекторі.
    public void SelectObject(int index)
    {
        if (placementController == null || objects == null || index < 0 || index >= objects.Length ||
            objects[index] == null || objects[index].prefab == null)
        {
            Debug.LogWarning("ЛР5: для цієї картки не призначений префаб або менеджер розміщення.", this);
            return;
        }

        ObjectOption option = objects[index];
        placementController.SelectPrefab(option.prefab);
        if (selectedObjectText != null)
            selectedObjectText.text = "Обрано: " + option.displayName;
        CloseSelection();
    }
}
