using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public class InteractionLogger : MonoBehaviour
{
    [SerializeField] private EyeGazeProvider gazeProvider;
    [SerializeField] private TraceBackSelector traceBackSelector;
    [SerializeField] private OnlineCalibrator calibrator;
    [SerializeField] private XRBaseInteractor interactor;

    [Header("Raycast")]
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    private void OnEnable()
    {
        if (interactor == null)
        {
            interactor = GetComponent<XRBaseInteractor>();
        }

        if (interactor != null)
        {
            interactor.selectEntered.AddListener(OnSelectEntered);
            interactor.activated.AddListener(OnActivated);
        }
    }

    private void OnDisable()
    {
        if (interactor != null)
        {
            interactor.selectEntered.RemoveListener(OnSelectEntered);
            interactor.activated.RemoveListener(OnActivated);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        TryLogSample(args.interactableObject?.transform);
    }

    private void OnActivated(ActivateEventArgs args)
    {
        TryLogSample(args.interactableObject?.transform);
    }

    private void TryLogSample(Transform targetTransform)
    {
        if (gazeProvider == null || calibrator == null)
        {
            return;
        }

        EyeGazeProvider.GazeRaySample rawSample = gazeProvider.CurrentSample;
        Ray rawRay = new Ray(rawSample.origin, rawSample.direction);

        Vector3 targetPoint = ResolveHitPoint(rawRay, targetTransform);

        EyeGazeProvider.GazeRaySample selectedSample = rawSample;
        float selectionAngleError = 0f;

        if (traceBackSelector != null && traceBackSelector.TrySelectStableSample(targetPoint, out EyeGazeProvider.GazeRaySample stableSample, out float angleError))
        {
            selectedSample = stableSample;
            selectionAngleError = angleError;
        }

        OnlineCalibrator.CalibrationSample sample = new OnlineCalibrator.CalibrationSample
        {
            origin = selectedSample.origin,
            rawDirection = selectedSample.direction,
            targetDirection = (targetPoint - selectedSample.origin).normalized,
            timestamp = Time.time
        };

        calibrator.AddSample(sample, selectionAngleError);
    }

    private Vector3 ResolveHitPoint(Ray gazeRay, Transform targetTransform)
    {
        if (Physics.Raycast(gazeRay, out RaycastHit hit, maxDistance, hitMask, triggerInteraction))
        {
            return hit.point;
        }

        if (targetTransform != null)
        {
            return targetTransform.position;
        }

        return gazeRay.origin + gazeRay.direction * maxDistance;
    }
}
