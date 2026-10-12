using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;

public class ARLightEstimationController : MonoBehaviour
{
    [SerializeField] private ARCameraManager arCameraManager;
    [SerializeField] private Light directionalLight;
    [SerializeField] private TMP_Text lightValuesText;

    private void Awake()
    {
        if (directionalLight == null)
            directionalLight = GetComponent<Light>();
    }

    private void OnEnable()
    {
        if (arCameraManager != null)
            arCameraManager.frameReceived += OnCameraFrameReceived;
    }

    private void OnDisable()
    {
        if (arCameraManager != null)
            arCameraManager.frameReceived -= OnCameraFrameReceived;
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        var estimate = args.lightEstimation;
        var values = new StringBuilder("Освітлення AR\n");

        if (estimate.averageBrightness.HasValue)
        {
            float brightness = estimate.averageBrightness.Value;
            values.AppendLine($"Яскравість: {brightness:F2}");
            if (directionalLight != null)
                directionalLight.intensity = brightness;
        }
        else
        {
            values.AppendLine("Яскравість: немає даних");
        }

        if (estimate.averageColorTemperature.HasValue)
        {
            float temperature = estimate.averageColorTemperature.Value;
            values.AppendLine($"Температура: {temperature:F0} K");
            if (directionalLight != null)
            {
                directionalLight.useColorTemperature = true;
                directionalLight.colorTemperature = temperature;
            }
        }
        else
        {
            values.AppendLine("Температура: немає даних");
        }

        if (estimate.colorCorrection.HasValue)
        {
            Color correction = estimate.colorCorrection.Value;
            values.AppendLine($"Корекція кольору: #{ColorUtility.ToHtmlStringRGB(correction)}");
            if (directionalLight != null)
                directionalLight.color = correction;
        }
        else
        {
            values.AppendLine("Корекція кольору: немає даних");
        }

        if (estimate.mainLightDirection.HasValue)
        {
            Vector3 direction = estimate.mainLightDirection.Value;
            values.AppendLine("Напрямок основного світла: доступний");
            if (directionalLight != null && direction.sqrMagnitude > 0.0001f)
                directionalLight.transform.rotation = Quaternion.LookRotation(direction);
        }
        else
        {
            values.AppendLine("Напрямок основного світла: немає даних");
        }

        if (estimate.ambientSphericalHarmonics.HasValue)
        {
            values.AppendLine("Ambient Spherical Harmonics: доступні");
            RenderSettings.ambientProbe = estimate.ambientSphericalHarmonics.Value;
        }
        else
        {
            values.AppendLine("Ambient Spherical Harmonics: немає даних");
        }

        if (lightValuesText != null)
            lightValuesText.text = values.ToString();
    }
}
