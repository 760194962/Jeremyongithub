using UnityEngine;

[DisallowMultipleComponent]
public class DebugVisualizer : MonoBehaviour
{
    [SerializeField] private EyeGazeProvider gazeProvider;
    [SerializeField] private OnlineCalibrator calibrator;

    [Header("Ray Display")]
    [SerializeField] private float rayLength = 5f;
    [SerializeField] private Color rawRayColor = new Color(1f, 0.4f, 0.4f, 0.9f);
    [SerializeField] private Color calibratedRayColor = new Color(0.4f, 0.9f, 1f, 0.9f);
    [SerializeField] private bool drawInSceneView = true;

    [Header("HUD")]
    [SerializeField] private bool showHud = true;
    [SerializeField] private Vector2 hudOffset = new Vector2(20f, 20f);

    private void Update()
    {
        if (!drawInSceneView || gazeProvider == null)
        {
            return;
        }

        EyeGazeProvider.GazeRaySample sample = gazeProvider.CurrentSample;
        Vector3 rawDirection = sample.direction.normalized;

        Debug.DrawRay(sample.origin, rawDirection * rayLength, rawRayColor);

        if (calibrator != null)
        {
            Vector3 calibratedDirection = calibrator.GetCalibratedDirection(rawDirection);
            Debug.DrawRay(sample.origin, calibratedDirection * rayLength, calibratedRayColor);
        }
    }

    private void OnGUI()
    {
        if (!showHud || calibrator == null)
        {
            return;
        }

        GUI.Label(new Rect(hudOffset.x, hudOffset.y, 400f, 60f),
            $"Gaze Calibration\nMode: {calibrator.Mode}\nMean Error: {calibrator.MeanAngleErrorDegrees:F1}°");
    }
}
