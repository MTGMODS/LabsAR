using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARSessionStatus : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText;

    void OnEnable()
    {
        ARSession.stateChanged += OnSessionStateChanged;
        UpdateText(ARSession.state);
    }

    void OnDisable()
    {
        ARSession.stateChanged -= OnSessionStateChanged;
    }

    private void OnSessionStateChanged(ARSessionStateChangedEventArgs args)
    {
        UpdateText(args.state);
    }

    private void UpdateText(ARSessionState state)
    {
        if (statusText != null)
        {
            statusText.text = "AR: " + state;
        }
    }
}