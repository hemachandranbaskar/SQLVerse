using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

[RequireComponent(typeof(OVRMicrogestureEventSource))]
public class MoonSelector : MonoBehaviour
{
    private OVRMicrogestureEventSource gestureSource;

    private List<GameObject> moons = new List<GameObject>();
    private int currentMoonIndex = 0;

    private Material originalMaterial;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material joinLineMaterial;
    [SerializeField] private QueryBuilder queryBuilder;

    private static List<GameObject> selectedMoons = new List<GameObject>(); // static for cross-planet memory

    private void Start()
    {
        gestureSource = GetComponent<OVRMicrogestureEventSource>();
        if (gestureSource == null)
        {
            Debug.LogError("OVRMicrogestureEventSource not found!");
            return;
        }

        gestureSource.GestureRecognizedEvent.AddListener(OnGesture);

        foreach (Transform child in transform)
        {
            if (child.CompareTag("Moon"))
                moons.Add(child.gameObject);
        }

        if (moons.Count > 0)
            HighlightMoon(currentMoonIndex);
    }

    private void OnGesture(OVRHand.MicrogestureType gesture)
    {
        if (moons.Count == 0) return;

        int prevIndex = currentMoonIndex;

        if (gesture == OVRHand.MicrogestureType.SwipeRight)
            currentMoonIndex = (currentMoonIndex + 1) % moons.Count;
        else if (gesture == OVRHand.MicrogestureType.SwipeLeft)
            currentMoonIndex = (currentMoonIndex - 1 + moons.Count) % moons.Count;
        else if (gesture == OVRHand.MicrogestureType.ThumbTap)
        {
            GameObject selectedMoon = moons[currentMoonIndex];
            Debug.Log($"Moon selected via gesture: {selectedMoon.name}");

            if (!selectedMoons.Contains(selectedMoon))
            {
                selectedMoons.Add(selectedMoon);
            }

            if (selectedMoons.Count == 2)
            {
                GameObject moon1 = selectedMoons[0];
                GameObject moon2 = selectedMoons[1];

                // Visual join line
                CreateJoinLine(moon1, moon2);

                // Call QueryBuilder
                if (queryBuilder != null)
                {
                    queryBuilder.AddJoin(moon1, moon2);
                }

                Debug.Log($"Join created between: {moon1.name} and {moon2.name}");

                selectedMoons.Clear(); // reset after join
            }
        }

        UnhighlightMoon(prevIndex);
        HighlightMoon(currentMoonIndex);
    }

    private void HighlightMoon(int index)
    {
        var r = moons[index].GetComponent<Renderer>();
        if (r != null) r.material = highlightMaterial;
    }

    private void UnhighlightMoon(int index)
    {
        var r = moons[index].GetComponent<Renderer>();
        if (r != null)
        {
            string type = GetDataTypeFromMoonName(moons[index].name);
            r.material = originalMaterial;
        }
    }

    private string GetDataTypeFromMoonName(string name)
    {
        if (name.Contains("("))
        {
            int start = name.IndexOf('(') + 1;
            int end = name.IndexOf(')');
            return name.Substring(start, end - start).ToLower();
        }
        return "string";
    }

    private Color GetColorForDataType(string type)
    {
        switch (type)
        {
            case "string": return Color.green;
            case "integer": return Color.red;
            case "float": return Color.yellow;
            case "boolean": return Color.cyan;
            default: return Color.white;
        }
    }

    private void CreateJoinLine(GameObject moon1, GameObject moon2)
    {
        GameObject lineObj = new GameObject("JoinLine");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = joinLineMaterial;
        lr.startColor = Color.yellow;
        lr.endColor = Color.yellow;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.positionCount = 2;

        StartCoroutine(UpdateLine(lr, moon1, moon2));
    }

    private System.Collections.IEnumerator UpdateLine(LineRenderer lr, GameObject moon1, GameObject moon2)
    {
        while (moon1 != null && moon2 != null && lr != null)
        {
            lr.SetPosition(0, moon1.transform.position);
            lr.SetPosition(1, moon2.transform.position);
            yield return null;
        }
    }
}
