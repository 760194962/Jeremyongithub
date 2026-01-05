using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class OnlineCalibrator : MonoBehaviour
{
    public struct CalibrationSample
    {
        public Vector3 origin;
        public Vector3 rawDirection;
        public Vector3 targetDirection;
        public float timestamp;
    }

    [Header("Calibration")]
    [SerializeField] private int samplesPerUpdate = 8;
    [SerializeField] private int maxQueuedSamples = 10;
    [SerializeField] private float updateSmoothing = 0.6f;

    [Header("Error Tracking")]
    [SerializeField] private int errorWindow = 20;

    private readonly List<CalibrationSample> pendingSamples = new List<CalibrationSample>();
    private readonly Queue<float> errorQueue = new Queue<float>();

    private Quaternion correctionRotation = Quaternion.identity;

    public Quaternion CorrectionRotation => correctionRotation;
    public float MeanAngleErrorDegrees { get; private set; }
    public string Mode => pendingSamples.Count >= samplesPerUpdate ? "Calibrating" : "Collecting";

    private void OnValidate()
    {
        samplesPerUpdate = Mathf.Clamp(samplesPerUpdate, 4, 30);
        maxQueuedSamples = Mathf.Max(samplesPerUpdate, maxQueuedSamples);
        updateSmoothing = Mathf.Clamp01(updateSmoothing);
        errorWindow = Mathf.Clamp(errorWindow, 5, 50);
    }

    public void AddSample(CalibrationSample sample, float selectionAngleError)
    {
        if (sample.rawDirection == Vector3.zero || sample.targetDirection == Vector3.zero)
        {
            return;
        }

        pendingSamples.Add(sample);
        while (pendingSamples.Count > maxQueuedSamples)
        {
            pendingSamples.RemoveAt(0);
        }

        EnqueueError(selectionAngleError);

        if (pendingSamples.Count >= samplesPerUpdate)
        {
            UpdateCalibration();
        }
    }

    public Vector3 GetCalibratedDirection(Vector3 rawDirection)
    {
        if (rawDirection == Vector3.zero)
        {
            return rawDirection;
        }

        return correctionRotation * rawDirection.normalized;
    }

    private void UpdateCalibration()
    {
        Quaternion averageRotation = Quaternion.identity;
        for (int i = 0; i < pendingSamples.Count; i++)
        {
            CalibrationSample sample = pendingSamples[i];
            Quaternion delta = Quaternion.FromToRotation(sample.rawDirection, sample.targetDirection);
            float t = 1f / (i + 1);
            averageRotation = Quaternion.Slerp(averageRotation, delta, t);
        }

        correctionRotation = Quaternion.Slerp(correctionRotation, averageRotation, updateSmoothing);
        pendingSamples.Clear();
    }

    private void EnqueueError(float angleError)
    {
        errorQueue.Enqueue(angleError);
        while (errorQueue.Count > errorWindow)
        {
            errorQueue.Dequeue();
        }

        float sum = 0f;
        foreach (float error in errorQueue)
        {
            sum += error;
        }

        MeanAngleErrorDegrees = errorQueue.Count > 0 ? sum / errorQueue.Count : 0f;
    }
}
