using UnityEngine;

[RequireComponent(typeof(OVRMicrogestureEventSource))]
public class UniverseScaler : MonoBehaviour
{
    private OVRMicrogestureEventSource ovrMicrogestureEventSource;
    [SerializeField] private float scaledDownFactor = 0.1f; // Scale to 10% when toggled
    [SerializeField] private float distanceFromCamera = 1.5f; // Distance in front of camera
    [SerializeField] private float transitionDuration = 0.5f; // Transition time

    private Vector3 originalScale;
    private Vector3 originalPosition;
    private bool isScaledDown = false;
    private bool isTransitioning = false;

    void Start()
    {
        // Store original scale and position
        Debug.Log("UniverseScaler: Start() called");

        originalScale = transform.localScale;
        originalPosition = transform.position;

        Debug.Log($"Original scale: {originalScale}, position: {originalPosition}");

        ovrMicrogestureEventSource = GetComponent<OVRMicrogestureEventSource>();
        if (ovrMicrogestureEventSource == null)
        {
            Debug.LogError("OVRMicrogestureEventSource not found!");
            return;
        }
        Debug.Log("Adding gesture listener");

        ovrMicrogestureEventSource.GestureRecognizedEvent.AddListener(g =>
        {
            OnMicrogestureRecognized(g);
        });
    }


    void OnMicrogestureRecognized(OVRHand.MicrogestureType microgestureType)
    {
        Debug.Log($"Microgesture event received: {microgestureType}");
        if (microgestureType == OVRHand.MicrogestureType.ThumbTap)
        {
            OnToggleAction(microgestureType);
        }
        if (microgestureType == OVRHand.MicrogestureType.SwipeLeft)
        {
            StartCoroutine(SmoothTransition(transform.localScale, new Vector3(transform.position.x + 0.5f, transform.position.y, transform.position.z)));
        }
        if (microgestureType == OVRHand.MicrogestureType.SwipeRight)
        {
            StartCoroutine(SmoothTransition(transform.localScale, new Vector3(transform.position.x - 0.5f, transform.position.y, transform.position.z)));
        }
    }
    private void OnToggleAction(OVRHand.MicrogestureType microgestureType)
    {
        if (isTransitioning) return;

        isTransitioning = true;

        if (isScaledDown)
        {
            // Restore original scale and position
            StartCoroutine(SmoothTransition(originalScale, originalPosition));
        }
        else
        {
            // Scale down and move closer to camera
            Vector3 targetScale = originalScale * scaledDownFactor;
            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();
            Vector3 targetPosition = Camera.main.transform.position + cameraForward * distanceFromCamera;
            StartCoroutine(SmoothTransition(targetScale, targetPosition));
        }

        isScaledDown = !isScaledDown;
    }

    private System.Collections.IEnumerator SmoothTransition(Vector3 targetScale, Vector3 targetPosition)
    {
        Vector3 startScale = transform.localScale;
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        // Ensure final values are exact
        transform.localScale = targetScale;
        transform.position = targetPosition;
        isTransitioning = false;
    }
}