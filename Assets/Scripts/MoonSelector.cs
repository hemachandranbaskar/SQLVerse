using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

[RequireComponent(typeof(OVRMicrogestureEventSource))]
public class MoonSelector : MonoBehaviour
{
    private OVRMicrogestureEventSource gestureSource;
    [SerializeField] private float gazeRayDistance = 50f;

    private List<GameObject> moons = new List<GameObject>();
    private int currentMoonIndex = 0;

    private Material originalMaterial;
    [SerializeField] private Material highlightMaterial;

    private GameObject currentMoon;
    private static GameObject currentFocusedPlanet;

    private void Start()
    {
        gestureSource = GetComponent<OVRMicrogestureEventSource>();
        if (gestureSource == null)
        {
            Debug.LogError("OVRMicrogestureEventSource not found!");
            return;
        }

        gestureSource.GestureRecognizedEvent.AddListener(OnGesture);
    }

    public static void SetFocusedPlanet(GameObject planet)
    {
        currentFocusedPlanet = planet;
        Debug.Log("SetFocusedPlanet called: " + planet.name);
    }

    private void Update()
    {
        // Dynamically update the focused planet based on gaze
        UpdateFocusedPlanet();
    }

    private void UpdateFocusedPlanet()
    {
        GameObject previousFocusedPlanet = currentFocusedPlanet;

        // Cast a ray from the headset's forward direction
        Ray gazeRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(gazeRay, out RaycastHit hit, gazeRayDistance) && hit.collider.CompareTag("Planet"))
        {
            currentFocusedPlanet = hit.collider.gameObject;
        }
        else
        {
            currentFocusedPlanet = null;
        }

        // If the focused planet changed, reset the moon list and highlights
        if (currentFocusedPlanet != previousFocusedPlanet)
        {
            ResetMoonsAndHighlights();
            if (currentFocusedPlanet != null)
            {
                FetchMoonsFromPlanet(currentFocusedPlanet);
                Debug.Log($"Focused on planet: {currentFocusedPlanet.name}. Found {moons.Count} moons.");
            }
        }
    }

    private void ResetMoonsAndHighlights()
    {
        // Unhighlight the current moon
        UnhighlightMoon(currentMoonIndex);

        // Clear the moon list and reset index
        moons.Clear();
        currentMoonIndex = 0;
        currentMoon = null;
    }

    private void OnGesture(OVRHand.MicrogestureType gesture)
    {
        if (currentFocusedPlanet == null)
        {
            Debug.LogWarning("No planet is currently focused for moon selection.");
            return;
        }

        if (moons.Count == 0)
        {
            FetchMoonsFromPlanet(currentFocusedPlanet);
        }

        if (moons.Count == 0) return;

        int prevIndex = currentMoonIndex;

        if (gesture == OVRHand.MicrogestureType.SwipeRight)
        {
            currentMoonIndex = (currentMoonIndex + 1) % moons.Count;
        }
        else if (gesture == OVRHand.MicrogestureType.SwipeLeft)
        {
            currentMoonIndex = (currentMoonIndex - 1 + moons.Count) % moons.Count;
        }
        else if (gesture == OVRHand.MicrogestureType.ThumbTap)
        {
            GameObject selectedMoon = moons[currentMoonIndex];
            DatabaseGalaxyGenerator.Instance.OnMoonSelected(selectedMoon);
        }

        UnhighlightMoon(prevIndex);
        HighlightMoon(currentMoonIndex);
    }

    private void FetchMoonsFromPlanet(GameObject planet)
    {
        moons.Clear();
        foreach (Transform child in planet.transform)
        {
            if (child.CompareTag("Moon"))
            {
                moons.Add(child.gameObject);
            }
        }
        currentMoonIndex = 0;
        HighlightMoon(currentMoonIndex);
    }

    private void HighlightMoon(int index)
    {
        if (index < 0 || index >= moons.Count) return;
        Renderer r = moons[index].GetComponent<Renderer>();
        if (r != null)
        {
            originalMaterial = r.material;
            r.material = highlightMaterial;
        }
        currentMoon = moons[index];
    }

    private void UnhighlightMoon(int index)
    {
        if (index < 0 || index >= moons.Count) return;
        Renderer r = moons[index].GetComponent<Renderer>();
        if (r != null && originalMaterial != null)
        {
            r.material = originalMaterial;
        }
    }
}
