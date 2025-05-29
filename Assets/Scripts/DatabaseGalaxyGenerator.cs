using UnityEngine;
using TMPro;
using System.Collections;
using Oculus.Interaction;
using System.Collections.Generic;

public class DatabaseGalaxyGenerator : MonoBehaviour
{
    public static DatabaseGalaxyGenerator Instance { get; private set; }

    [SerializeField] private string jsonFileName = "SampleDatabase";
    [SerializeField] private bool useBoundarySpheres = false;
    [SerializeField] private bool useUniformMoonDistribution = true;
    [SerializeField] private float moonOrbitRadius = 0.5f;
    [SerializeField] private Material pulsingOrbMaterial;
    [SerializeField] private GameObject planetInteractablePrefab;
    [SerializeField] private GameObject moonInteractablePrefab;
    [SerializeField] private QueryBuilder queryBuilder;
    [SerializeField] private Material joinLineMaterial;

    // Testing
    [SerializeField] private GameObject orderPlanet;
    [SerializeField] private GameObject userPlanet;
    [SerializeField] private GameObject orderPlanetUserIdMoon;
    [SerializeField] private GameObject userPlanetUserIdMoon;
    private List<GameObject> selectedMoons = new List<GameObject>();
    private List<(GameObject moon1, GameObject moon2)> joins = new List<(GameObject, GameObject)>();
    private Dictionary<GameObject, Vector3> originalMoonScales = new Dictionary<GameObject, Vector3>();

    private DatabaseRoot databaseRoot;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        TextAsset jsonTextAsset = Resources.Load<TextAsset>(jsonFileName);
        if (jsonTextAsset == null)
        {
            Debug.LogError("JSON file not found in Resources!");
            return;
        }

        databaseRoot = JsonUtility.FromJson<DatabaseRoot>(jsonTextAsset.text);
        if (databaseRoot != null)
        {
            GenerateGalaxy();
        }

        SubscribeToPlanetGrabAndRelease(orderPlanet);
        SubscribeToPlanetGrabAndRelease(userPlanet);
        SubscribeToMoonSelection(orderPlanetUserIdMoon);
        SubscribeToMoonSelection(userPlanetUserIdMoon);
    }

    private void SubscribeToMoonSelection(GameObject moon)
    {
        //var moonCollider = moon.GetComponent<SphereCollider>();
        //if (moonCollider == null)
        //{
        //    moonCollider = moon.AddComponent<SphereCollider>();
        //}
        //var moonRigidbody = moon.GetComponent<Rigidbody>();
        //if (moonRigidbody == null)
        //{
        //    moonRigidbody = moon.AddComponent<Rigidbody>();
        //    moonRigidbody.useGravity = false;
        //    moonRigidbody.isKinematic = false;
        //}
        //var moonDistanceGrab = moon.AddComponent<DistanceGrabInteractable>();
        //moonDistanceGrab.InjectRigidbody(moonRigidbody);

        // Subscribe to moon grab events
        var moonDistanceGrab = moon.GetComponentInChildren<DistanceGrabInteractable>();
        if (moonDistanceGrab != null)
        {
            moonDistanceGrab.WhenPointerEventRaised -= (evt) => OnMoonSelected(evt, moon); // Unsubscribe to avoid duplicates
            moonDistanceGrab.WhenPointerEventRaised += (evt) => OnMoonSelected(evt, moon);
        }
        else
        {
            Debug.LogError($"DistanceGrabInteractable component not found on moon {moon.name}. Ensure the prefab is correctly configured.");
        }
    }

    private void SubscribeToPlanetGrabAndRelease(GameObject planet)
    {
        var planetDistanceGrab = planet.GetComponentInChildren<DistanceGrabInteractable>();
        if (planetDistanceGrab != null)
        {
            planetDistanceGrab.WhenPointerEventRaised += (PointerEvent evt) =>
            {
                if (evt.Type == PointerEventType.Select)
                {
                    OnPlanetGrabbed(planet);
                }
                else if (evt.Type == PointerEventType.Unselect)
                {
                    OnPlanetReleased(planet);
                }
            };
        }
        else
        {
            Debug.LogError($"DistanceGrabInteractable component not found on planet {planet.name}. Ensure the prefab is correctly configured.");
        }
    }


    //void Update()
    //{
    //    UpdateQueryDisplay();
    //}

    private void GenerateGalaxy()
    {
        float galaxySpacing = 50f;
        //float moonOrbitSpacing = 0.5f;
        //float planetSpacing = 10f;
        int databaseIndex = 0;

        foreach (Database db in databaseRoot.Databases)
        {
            GameObject galaxy = new GameObject(db.Name);
            galaxy.transform.parent = transform;
            galaxy.transform.position = new Vector3(databaseIndex * galaxySpacing, 0, 0);
            AddGalaxyParticleSystem(galaxy, Color.white, 50f, 2000);
            AddLabel(galaxy, $"Database:\n{db.Name}", 10f);

            int schemaIndex = 0;
            float schemaAngleStep = 360f / Mathf.Max(1, db.Schemas.Length);
            foreach (Schema schema in db.Schemas)
            {
                GameObject schemaCluster = new GameObject(schema.Name);
                GameObject sun = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                schemaCluster.transform.SetParent(galaxy.transform);
                float angle = schemaIndex * schemaAngleStep * Mathf.Deg2Rad;
                float schemaDistance = 20f;
                schemaCluster.transform.localPosition = new Vector3(Mathf.Cos(angle) * schemaDistance, 0, Mathf.Sin(angle) * schemaDistance);

                sun.name = schema.Name;
                sun.transform.localScale = Vector3.one * 5f;
                sun.transform.SetParent(galaxy.transform);
                sun.transform.localPosition = new Vector3(Mathf.Cos(angle) * schemaDistance, 0, Mathf.Sin(angle) * schemaDistance);
                //sun.GetComponent<Renderer>().material.color = Color.yellow;
                sun.GetComponent<Renderer>().material = pulsingOrbMaterial;

                AddSchemaParticleSystem(schemaCluster, GetSchemaColor(schemaIndex), 10f, 200);
                if (useBoundarySpheres)
                {
                    AddBoundarySphere(schemaCluster, 15f, GetSchemaColor(schemaIndex));
                }
                AddLabel(sun, $"Schema:\n{schema.Name}", 5f);

                int tableIndex = 0;
                float tableAngleStep = 360f / Mathf.Max(1, schema.Tables.Length);
                foreach (Table table in schema.Tables)
                {
                    GameObject planet = Instantiate(planetInteractablePrefab, Vector3.zero, Quaternion.identity); //GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    planet.name = table.Name;
                    planet.transform.localScale = Vector3.one * 3f; //Vector3.one * 3f;
                    float planetAngle = tableIndex * tableAngleStep * Mathf.Deg2Rad;
                    float planetDistance = 5f + tableIndex * 3f; ; // planetSpacing + tableIndex * 5f;
                    planet.transform.SetParent(schemaCluster.transform);
                    planet.transform.localPosition = new Vector3(Mathf.Cos(planetAngle) * planetDistance, 0, Mathf.Sin(planetAngle) * planetDistance);
                    planet.GetComponent<Renderer>().material.color = Color.blue;
                    planet.AddComponent<Orbiter>().orbitSpeed = 20f;
                    AddLabel(planet, table.Name, 3f);
                    planet.tag = "Planet";

                    //planet.AddComponent<RayInteractable>();
                    //var selector = planet.AddComponent<SelectorUnityEventWrapper>();
                    //selector.WhenSelected.AddListener(() =>
                    //{
                    //    MoonSelector.SetFocusedPlanet(planet);
                    //    Debug.Log("Focused planet set to: " + planet.name);
                    //});

                    var distanceGrab = planet.GetComponentInChildren<DistanceGrabInteractable>();
                    if (distanceGrab != null)
                    {
                        // Subscribe to WhenPointerEventRaised to detect select/unselect
                        distanceGrab.WhenPointerEventRaised += (PointerEvent evt) =>
                        {
                            if (evt.Type == PointerEventType.Select)
                            {
                                OnPlanetGrabbed(planet);
                            }
                            else if (evt.Type == PointerEventType.Unselect)
                            {
                                OnPlanetReleased(planet);
                            }
                        };
                    }
                    else
                    {
                        Debug.LogError($"DistanceGrabInteractable component not found on planet {planet.name}. Ensure the prefab is correctly configured.");
                    }

                    int columnIndex = 0;
                    Vector3[] moonPositions = useUniformMoonDistribution
                        ? GenerateFibonacciSpherePoints(table.Columns.Length, moonOrbitRadius)
                        : GenerateRandomSpherePoints(table.Columns.Length, moonOrbitRadius);

                    foreach (Column column in table.Columns)
                    {
                        GameObject moon = Instantiate(moonInteractablePrefab, Vector3.zero, Quaternion.identity); //GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        moon.name = $"{column.Name} ({column.DataType})";
                        moon.transform.localScale = Vector3.one * 1f;
                        moon.transform.SetParent(planet.transform);
                        moon.transform.localPosition = moonPositions[columnIndex];
                        var renderer = moon.GetComponent<Renderer>();
                        renderer.material.color = GetColorForDataType(column.DataType);
                        moon.AddComponent<Orbiter>().orbitSpeed = 5f;
                        if (column.IsPrimaryKey)
                        {
                            //SetGlowingMaterial(moon, GetColorForDataType(column.DataType));
                            renderer.material = pulsingOrbMaterial;
                            StartCoroutine(PulseOrb(moon));
                            moon.name += " (Primary Key)";
                        }

                        originalMoonScales[moon] = moon.transform.localScale;

                        //******************* UNCOMMENT LATER ************************
                        /*var moonCollider = moon.GetComponent<SphereCollider>();
                        if (moonCollider == null)
                        {
                            moonCollider = moon.AddComponent<SphereCollider>();
                        }
                        var moonRigidbody = moon.GetComponent<Rigidbody>();
                        if (moonRigidbody == null)
                        {
                            moonRigidbody = moon.AddComponent<Rigidbody>();
                            moonRigidbody.useGravity = false;
                            moonRigidbody.isKinematic = false;
                        }
                        var moonDistanceGrab = moon.AddComponent<DistanceGrabInteractable>();
                        moonDistanceGrab.InjectRigidbody(moonRigidbody);

                        // Subscribe to moon grab events
                        moonDistanceGrab.WhenPointerEventRaised += (PointerEvent evt) =>
                        {
                            if (evt.Type == PointerEventType.Select)
                            {
                                OnMoonGrabbed(moon);
                            }
                            else if (evt.Type == PointerEventType.Unselect)
                            {
                                OnMoonReleased(moon);
                            }
                        };*/

                        AddLabel(moon, $"{column.Name} ({column.DataType})", 2f);
                        //AddLabel(moon, $"{column.Name} ({column.DataType})" + (columnIndex == 0 ? " (Primary Key)" : ""), 0.8f);
                        moon.tag = "Moon";

                        var moonDistanceGrab = moon.GetComponentInChildren<DistanceGrabInteractable>();
                        if (moonDistanceGrab != null)
                        {
                            // Subscribe to WhenPointerEventRaised to detect select/unselect
                            moonDistanceGrab.WhenPointerEventRaised -= (evt) => OnMoonSelected(evt, moon); // Unsubscribe to avoid duplicates
                            moonDistanceGrab.WhenPointerEventRaised += (evt) => OnMoonSelected(evt, moon);
                        }
                        else
                        {
                            Debug.LogError($"DistanceGrabInteractable component not found on moon {moon.name}. Ensure the prefab is correctly configured.");
                        }

                        columnIndex++;
                    }
                    tableIndex++;
                }
                schemaIndex++;
            }
            databaseIndex++;
        }
    }

    /*private void OnPlanetGrabbed(GameObject planet)
    {
        if (queryBuilder != null)
        {
            GameObject clone = Instantiate(planet, planet.transform.position, planet.transform.rotation);
            //clone.transform.localScale *= 0.2f; // Reduce size to 20%
            clone.name = $"{planet.name}_Clone";

            // Scale only the planet's mesh, not its children (moons)
            Transform planetMesh = clone.transform.Find("Sphere"); // Adjust if your planetInteractablePrefab has a different child name
            if (planetMesh != null)
            {
                planetMesh.localScale *= 0.2f;
            }
            else
            {
                clone.transform.localScale *= 0.2f; // Fallback to scaling the entire object
            }

            // Disable Orbiter on the clone to prevent unwanted orbiting
            var cloneOrbiter = clone.GetComponent<Orbiter>();
            if (cloneOrbiter != null) cloneOrbiter.enabled = false;

            // Ensure the clone has a Rigidbody and DistanceGrabInteractable
            var cloneRigidbody = clone.GetComponent<Rigidbody>();
            if (cloneRigidbody == null)
            {
                cloneRigidbody = clone.AddComponent<Rigidbody>();
                cloneRigidbody.useGravity = false;
                cloneRigidbody.isKinematic = false;
                cloneRigidbody.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement
            }

            var cloneDistanceGrab = clone.GetComponentInChildren<DistanceGrabInteractable>();
            if (cloneDistanceGrab == null)
            {
                cloneDistanceGrab = clone.AddComponent<DistanceGrabInteractable>();
                cloneDistanceGrab.InjectRigidbody(cloneRigidbody);

            }
            cloneDistanceGrab.enabled = true;

            // Temporarily disable interaction on the original planet
            var originalDistanceGrab = planet.GetComponentInChildren<DistanceGrabInteractable>();
            if (originalDistanceGrab != null)
            {
                originalDistanceGrab.enabled = false;
                // Re-enable after a delay to allow re-grabbing
                StartCoroutine(ReEnableGrab(planet, 1f));
            }

            // Maintain original planet's orbit
            var originalOrbiter = planet.GetComponent<Orbiter>();
            if (originalOrbiter != null) originalOrbiter.enabled = true;

            //// Disable original interaction and reset position
            //planet.GetComponentInChildren<DistanceGrabInteractable>().enabled = false;
            //planet.GetComponent<Orbiter>().enabled = true;
            //planet.transform.position = planet.transform.parent.position;

            // Notify QueryBuilder
            queryBuilder.OnPlanetGrabbed(clone);
            //UpdateQueryDisplay();
        }
    }*/

    private void OnPlanetGrabbed(GameObject planet)
    {
        if (queryBuilder != null)
        {
            GameObject clone = Instantiate(planet, planet.transform.position, planet.transform.rotation);
            clone.name = $"{planet.name}_Clone";

            // Scale only the planet's mesh, not its children (moons)
            Transform planetMesh = clone.transform.Find("Sphere");
            if (planetMesh != null)
            {
                planetMesh.localScale *= 0.2f;
            }
            else
            {
                clone.transform.localScale *= 0.2f;
            }

            // Disable Orbiter on the clone to prevent unwanted orbiting
            var cloneOrbiter = clone.GetComponent<Orbiter>();
            if (cloneOrbiter != null) cloneOrbiter.enabled = false;

            // Ensure the clone has a Rigidbody and DistanceGrabInteractable
            var cloneRigidbody = clone.GetComponent<Rigidbody>();
            if (cloneRigidbody == null)
            {
                cloneRigidbody = clone.AddComponent<Rigidbody>();
                cloneRigidbody.useGravity = false;
                cloneRigidbody.isKinematic = false;
                cloneRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            }

            var cloneDistanceGrab = clone.GetComponentInChildren<DistanceGrabInteractable>();
            if (cloneDistanceGrab == null)
            {
                cloneDistanceGrab = clone.AddComponent<DistanceGrabInteractable>();
                cloneDistanceGrab.InjectRigidbody(cloneRigidbody);
            }
            cloneDistanceGrab.enabled = true;

            // Set up interactions for cloned moons
            SetupClonedMoonInteractions(clone);

            // Temporarily disable interaction on the original planet
            var originalDistanceGrab = planet.GetComponentInChildren<DistanceGrabInteractable>();
            if (originalDistanceGrab != null)
            {
                originalDistanceGrab.enabled = false;
                StartCoroutine(ReEnableGrab(planet, 1f));
            }

            // Maintain original planet's orbit
            var originalOrbiter = planet.GetComponent<Orbiter>();
            if (originalOrbiter != null) originalOrbiter.enabled = true;

            // Notify QueryBuilder
            queryBuilder.OnPlanetGrabbed(clone);
        }
    }

    private void SetupClonedMoonInteractions(GameObject clonedPlanet)
    {
        // Find all moons (children with "Moon" tag) in the cloned planet
        foreach (Transform child in clonedPlanet.transform)
        {
            if (child.CompareTag("Moon"))
            {
                GameObject clonedMoon = child.gameObject;

                // Disable orbiter on cloned moons to prevent interference
                var moonOrbiter = clonedMoon.GetComponent<Orbiter>();
                if (moonOrbiter != null) moonOrbiter.enabled = false;

                // Set up fresh interaction for this cloned moon
                var moonDistanceGrab = clonedMoon.GetComponentInChildren<DistanceGrabInteractable>();
                if (moonDistanceGrab != null)
                {
                    moonDistanceGrab.WhenPointerEventRaised -= (evt) => OnMoonSelected(evt, clonedMoon); // Unsubscribe to avoid duplicates
                    moonDistanceGrab.WhenPointerEventRaised += (evt) => OnMoonSelected(evt, clonedMoon); ;
                }
            }
        }
    }

    private void OnPlanetReleased(GameObject planet)
    {
        if (queryBuilder != null)
        {
            queryBuilder.OnPlanetReleased(planet);
            //UpdateQueryDisplay();
            // Clean up clone (assumed handled by QueryBuilder or destroyed elsewhere)
        }
        // Re-enable the original planet's grab interaction
        string originalPlanetName = planet.name.Replace("_Clone", "");
        GameObject originalPlanet = GameObject.Find(originalPlanetName);
        if (originalPlanet != null)
        {
            var distanceGrab = originalPlanet.GetComponentInChildren<DistanceGrabInteractable>();
            if (distanceGrab != null)
            {
                distanceGrab.enabled = true;
            }
        }
    }

    private IEnumerator ReEnableGrab(GameObject planet, float delay)
    {
        yield return new WaitForSeconds(delay);
        var distanceGrab = planet.GetComponentInChildren<DistanceGrabInteractable>();
        if (distanceGrab != null)
        {
            distanceGrab.enabled = true;
        }
    }

    public void OnMoonSelected(GameObject moon)
    {
        OnMoonSelected(new PointerEvent(0, PointerEventType.Select, Pose.identity, null), moon);
    }
    private void OnMoonSelected(PointerEvent evt, GameObject moon)
    {
        if (evt.Type != PointerEventType.Select) return;

        if (originalMoonScales.ContainsKey(moon))
        {
            moon.transform.localScale = originalMoonScales[moon] * 1.2f; // Increase scale by 20%
        }
        if (!selectedMoons.Contains(moon))
        {
            selectedMoons.Add(moon);
            Debug.Log($"Moon selected: {moon.name}. Total selected: {selectedMoons.Count}");

            // If we have two moons selected, create a join
            if (selectedMoons.Count == 2)
            {
                GameObject moon1 = selectedMoons[0];
                GameObject moon2 = selectedMoons[1];
                if (moon1.transform.parent == moon2.transform.parent)
                {
                    Debug.LogWarning("Cannot join two columns from the same table.");
                    return;
                }
                else
                {
                    CreateJoin(moon1, moon2);
                }
                selectedMoons.Clear(); // Reset selection

                // Reset highlights
                if (originalMoonScales.ContainsKey(moon1))
                {
                    moon1.transform.localScale = originalMoonScales[moon1];
                }
                if (originalMoonScales.ContainsKey(moon2))
                {
                    moon2.transform.localScale = originalMoonScales[moon2];
                }
            }
        }
    }

    private void SetGlowingMaterial(GameObject moon, Color baseColor)
    {
        var renderer = moon.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetFloat("_Surface", 1);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        material.SetColor("_BaseColor", baseColor);
        material.SetColor("_EmissionColor", baseColor * 2f);
        material.EnableKeyword("_EMISSION");
        renderer.material = material;
    }

    private void CreateJoin(GameObject moon1, GameObject moon2)
    {
        // Store the join relationship
        joins.Add((moon1, moon2));

        // Draw the visual bridge
        CreateJoinLine(moon1, moon2);

        // Update QueryBuilder with the join
        if (queryBuilder != null)
        {
            queryBuilder.AddJoin(moon1, moon2);
            //UpdateQueryDisplay();
        }

        Debug.Log($"Join created between {moon1.name} and {moon2.name}");
    }

    private void CreateJoinLine(GameObject moon1, GameObject moon2)
    {
        GameObject lineObj = new GameObject("JoinLine");
        LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.material = joinLineMaterial;
        lineRenderer.startColor = Color.yellow;
        lineRenderer.endColor = Color.yellow;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, moon1.transform.position);
        lineRenderer.SetPosition(1, moon2.transform.position);
        StartCoroutine(UpdateJoinLine(lineObj, moon1, moon2));
    }

    private IEnumerator UpdateJoinLine(GameObject lineObj, GameObject moon1, GameObject moon2)
    {
        LineRenderer lineRenderer = lineObj.GetComponent<LineRenderer>();
        while (lineObj != null && moon1 != null && moon2 != null) //true
        {
            lineRenderer.SetPosition(0, moon1.transform.position);
            lineRenderer.SetPosition(1, moon2.transform.position);
            yield return null;
        }
    }

    //private void UpdateQueryDisplay()
    //{
    //    if (queryBuilder != null)
    //    {
    //        queryBuilder.UpdateQueryDisplay();
    //    }
    //}

    private IEnumerator PulseOrb(GameObject moon)
    {
        var renderer = moon.GetComponent<Renderer>();
        float pulseSpeed = 1f;
        float minIntensity = 1f;
        float maxIntensity = 2f;

        while (true)
        {
            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
            renderer.material.SetColor("_EmissionColor", renderer.material.GetColor("_BaseColor") * intensity);
            yield return null;
        }
    }

    private Vector3[] GenerateRandomSpherePoints(int count, float radius)
    {
        Vector3[] points = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            float theta = Random.Range(0f, 2f * Mathf.PI);
            float phi = Mathf.Acos(Random.Range(-1f, 1f));
            float x = radius * Mathf.Sin(phi) * Mathf.Cos(theta);
            float y = radius * Mathf.Sin(phi) * Mathf.Sin(theta);
            float z = radius * Mathf.Cos(phi);
            points[i] = new Vector3(x, y, z);
        }
        return points;
    }

    private Vector3[] GenerateFibonacciSpherePoints(int count, float radius)
    {
        Vector3[] points = new Vector3[count];
        float offset = 2f / count;
        float increment = Mathf.PI * (3f - Mathf.Sqrt(5f)); // Golden angle
        for (int i = 0; i < count; i++)
        {
            float y = ((i * offset) - 1) + (offset / 2f);
            float r = Mathf.Sqrt(1 - y * y);
            float phi = i * increment;
            float x = Mathf.Cos(phi) * r;
            float z = Mathf.Sin(phi) * r;
            points[i] = new Vector3(x, y, z) * radius;
        }
        return points;
    }

    private void AddGalaxyParticleSystem(GameObject galaxy, Color color, float radius, int maxParticles)
    {
        ParticleSystem ps = galaxy.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startSize = 0.1f;
        main.startSpeed = 0;
        main.maxParticles = maxParticles;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = maxParticles / 10f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        Material material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        material.SetFloat("_Surface", 1);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        material.SetColor("_BaseColor", color);
        renderer.material = material;
    }

    private void AddSchemaParticleSystem(GameObject schema, Color color, float radius, int maxParticles)
    {
        ParticleSystem ps = schema.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSpeed = 0;
        main.maxParticles = maxParticles;
        main.startColor = new Color(color.r, color.g, color.b, 0.3f); // Semi-transparent
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = maxParticles / 10f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        Material material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        material.SetFloat("_Surface", 1);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        material.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 0.3f));
        renderer.material = material;
    }

    private void AddBoundarySphere(GameObject schema, float radius, Color color)
    {
        GameObject boundary = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        boundary.name = $"{schema.name}_Boundary";
        boundary.transform.SetParent(schema.transform);
        boundary.transform.localPosition = Vector3.zero;
        boundary.transform.localScale = Vector3.one * radius * 2f;
        var renderer = boundary.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetFloat("_Surface", 1);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        material.color = new Color(color.r, color.g, color.b, 0.1f);
        renderer.material = material;
    }

    private Color GetSchemaColor(int index)
    {
        Color[] colors = { new Color(0.5f, 0.2f, 1f), Color.cyan, Color.magenta, Color.yellow };
        return colors[index % colors.Length];
    }

    private Color GetColorForDataType(string dataType)
    {
        switch (dataType.ToLower())
        {
            case "string": return Color.green;
            case "integer": return Color.red;
            case "float": return Color.yellow;
            case "boolean": return Color.cyan;
            default: return Color.white;
        }
    }

    private void AddLabel(GameObject obj, string text, float fontSize)
    {
        GameObject labelObj = new GameObject($"{obj.name}_Label");
        labelObj.transform.SetParent(obj.transform);
        TextMeshPro textMesh = labelObj.AddComponent<TextMeshPro>();
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.transform.localPosition = new Vector3(0, 0.6f, 0);
        textMesh.color = Color.white;
    }
}