using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TraceBackSelector : MonoBehaviour
{
    [SerializeField] private EyeGazeProvider gazeProvider;
    [SerializeField] private float bufferSeconds = 1.0f;
    [SerializeField] private float stableWindowSeconds = 0.25f;
    [SerializeField] private int minSamplesInWindow = 3;

    private readonly List<EyeGazeProvider.GazeRaySample> buffer = new List<EyeGazeProvider.GazeRaySample>();

    public int BufferedSampleCount => buffer.Count;

    private void OnEnable()
    {
        if (gazeProvider != null)
        {
            gazeProvider.GazeSampled += OnGazeSampled;
        }
    }

    private void OnDisable()
    {
        if (gazeProvider != null)
        {
            gazeProvider.GazeSampled -= OnGazeSampled;
        }
    }

    private void OnValidate()
    {
        bufferSeconds = Mathf.Max(0.1f, bufferSeconds);
        stableWindowSeconds = Mathf.Clamp(stableWindowSeconds, 0.05f, bufferSeconds);
        minSamplesInWindow = Mathf.Max(2, minSamplesInWindow);
    }

    private void OnGazeSampled(EyeGazeProvider.GazeRaySample sample)
    {
        buffer.Add(sample);
        TrimBuffer(sample.timestamp);
    }

    private void TrimBuffer(float now)
    {
        float cutoff = now - bufferSeconds;
        int removeCount = 0;
        for (int i = 0; i < buffer.Count; i++)
        {
            if (buffer[i].timestamp >= cutoff)
            {
                break;
            }
            removeCount++;
        }

        if (removeCount > 0)
        {
            buffer.RemoveRange(0, removeCount);
        }
    }

    public bool TrySelectStableSample(Vector3 targetPoint, out EyeGazeProvider.GazeRaySample selected, out float angleError)
    {
        selected = default;
        angleError = 0f;

        if (buffer.Count == 0)
        {
            return false;
        }

        float bestVariance = float.MaxValue;
        float bestTargetAngle = float.MaxValue;
        bool found = false;

        for (int i = 0; i < buffer.Count; i++)
        {
            float startTime = buffer[i].timestamp;
            float endTime = startTime + stableWindowSeconds;

            List<EyeGazeProvider.GazeRaySample> window = GetWindowSamples(startTime, endTime);
            if (window.Count < minSamplesInWindow)
            {
                continue;
            }

            Vector3 averageOrigin = Vector3.zero;
            Vector3 averageDirection = Vector3.zero;

            foreach (EyeGazeProvider.GazeRaySample sample in window)
            {
                averageOrigin += sample.origin;
                averageDirection += sample.direction;
            }

            averageOrigin /= window.Count;
            averageDirection.Normalize();

            float variance = 0f;
            foreach (EyeGazeProvider.GazeRaySample sample in window)
            {
                variance += Vector3.Angle(sample.direction, averageDirection);
            }

            variance /= window.Count;

            Vector3 targetDirection = (targetPoint - averageOrigin).normalized;
            float targetAngle = Vector3.Angle(averageDirection, targetDirection);

            if (variance < bestVariance || (Mathf.Approximately(variance, bestVariance) && targetAngle < bestTargetAngle))
            {
                bestVariance = variance;
                bestTargetAngle = targetAngle;
                selected = new EyeGazeProvider.GazeRaySample
                {
                    origin = averageOrigin,
                    direction = averageDirection,
                    timestamp = (startTime + endTime) * 0.5f,
                    isTracked = true
                };
                angleError = targetAngle;
                found = true;
            }
        }

        return found;
    }

    private List<EyeGazeProvider.GazeRaySample> GetWindowSamples(float startTime, float endTime)
    {
        List<EyeGazeProvider.GazeRaySample> window = new List<EyeGazeProvider.GazeRaySample>();
        for (int i = 0; i < buffer.Count; i++)
        {
            EyeGazeProvider.GazeRaySample sample = buffer[i];
            if (sample.timestamp < startTime)
            {
                continue;
            }
            if (sample.timestamp > endTime)
            {
                break;
            }
            window.Add(sample);
        }
        return window;
    }
}
