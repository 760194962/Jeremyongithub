using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
public class EyeGazeProvider : MonoBehaviour
{
    [Serializable]
    public struct GazeRaySample
    {
        public Vector3 origin;
        public Vector3 direction;
        public float timestamp;
        public bool isTracked;
    }

    [Header("Fallback")]
    [SerializeField] private bool fallbackToHmdForward = true;
    [SerializeField] private XRNode fallbackHmdNode = XRNode.Head;

    [Header("Gaze Settings")]
    [SerializeField] private bool requireEyeTracking = false;

    private readonly List<InputDevice> eyeDevices = new List<InputDevice>();
    private InputDevice hmdDevice;

    public event Action<GazeRaySample> GazeSampled;

    public GazeRaySample CurrentSample { get; private set; }

    private void OnEnable()
    {
        RefreshDevices();
        InputDevices.deviceConnected += OnDeviceConnected;
        InputDevices.deviceDisconnected += OnDeviceDisconnected;
    }

    private void OnDisable()
    {
        InputDevices.deviceConnected -= OnDeviceConnected;
        InputDevices.deviceDisconnected -= OnDeviceDisconnected;
    }

    private void OnDeviceConnected(InputDevice device)
    {
        RefreshDevices();
    }

    private void OnDeviceDisconnected(InputDevice device)
    {
        RefreshDevices();
    }

    private void RefreshDevices()
    {
        eyeDevices.Clear();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.EyeTracking | InputDeviceCharacteristics.HeadMounted,
            eyeDevices);
        hmdDevice = InputDevices.GetDeviceAtXRNode(fallbackHmdNode);
    }

    private void Update()
    {
        bool isTracked = TryGetEyeGazePose(out Vector3 origin, out Quaternion rotation);
        if (!isTracked && fallbackToHmdForward)
        {
            isTracked = TryGetHmdPose(out origin, out rotation);
        }

        Vector3 direction = rotation * Vector3.forward;
        CurrentSample = new GazeRaySample
        {
            origin = origin,
            direction = direction,
            timestamp = Time.time,
            isTracked = isTracked
        };

        GazeSampled?.Invoke(CurrentSample);
    }

    private bool TryGetEyeGazePose(out Vector3 origin, out Quaternion rotation)
    {
        origin = Vector3.zero;
        rotation = Quaternion.identity;

        if (eyeDevices.Count == 0)
        {
            return false;
        }

        bool foundPose = false;
        bool tracked = false;

        foreach (InputDevice device in eyeDevices)
        {
            bool hasPosition = device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position);
            bool hasRotation = device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion deviceRotation);
            bool hasTracked = device.TryGetFeatureValue(CommonUsages.isTracked, out bool isTrackedValue);

            if (!hasPosition || !hasRotation)
            {
                continue;
            }

            origin = position;
            rotation = deviceRotation;
            tracked = hasTracked && isTrackedValue;
            foundPose = true;
            break;
        }

        if (!foundPose)
        {
            return false;
        }

        return !requireEyeTracking || tracked;
    }

    private bool TryGetHmdPose(out Vector3 origin, out Quaternion rotation)
    {
        origin = Vector3.zero;
        rotation = Quaternion.identity;

        if (!hmdDevice.isValid)
        {
            hmdDevice = InputDevices.GetDeviceAtXRNode(fallbackHmdNode);
        }

        bool hasPosition = hmdDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position);
        bool hasRotation = hmdDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion deviceRotation);

        if (!hasRotation)
        {
            return false;
        }

        origin = hasPosition ? position : Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        rotation = deviceRotation;
        return true;
    }
}
