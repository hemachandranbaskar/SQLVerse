using UnityEngine;
using TMPro;
using System.Collections;

public class DatabaseGalaxyGenerator : MonoBehaviour
{
    [SerializeField] private string jsonFileName = "SampleDatabase";
    [SerializeField] private bool useBoundarySpheres = false;
    [SerializeField] private bool useUniformMoonDistribution = true;
    [SerializeField] private float moonOrbitRadius = .5f;
    [SerializeField] private Material pulsingOrbMaterial;
    private DatabaseRoot databaseRoot;

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
    }

    private void GenerateGalaxy()
    {
        float galaxySpacing = 50f;
        //float moonOrbitSpacing = 0.5f;
        //float planetSpacing = 10f;
        int databaseIndex = 0;

        foreach (Database db in databaseRoot.Databases)
        {
            GameObject galaxy = new GameObject(db.Name);
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
                AddLabel(sun, $"Schema:\n{schema.Name}", 3f);

                int tableIndex = 0;
                float tableAngleStep = 360f / Mathf.Max(1, schema.Tables.Length);
                foreach (Table table in schema.Tables)
                {
                    GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    planet.name = table.Name;
                    planet.transform.localScale = Vector3.one * 3f; //Vector3.one * 3f;
                    float planetAngle = tableIndex * tableAngleStep * Mathf.Deg2Rad;
                    float planetDistance = 5f + tableIndex * 3f; ; // planetSpacing + tableIndex * 5f;
                    planet.transform.SetParent(schemaCluster.transform);
                    planet.transform.localPosition = new Vector3(Mathf.Cos(planetAngle) * planetDistance, 0, Mathf.Sin(planetAngle) * planetDistance);
                    planet.GetComponent<Renderer>().material.color = Color.blue;
                    planet.AddComponent<Orbiter>().orbitSpeed = 20f;
                    AddLabel(planet, table.Name, 1.5f);

                    int columnIndex = 0;
                    Vector3[] moonPositions = useUniformMoonDistribution
                        ? GenerateFibonacciSpherePoints(table.Columns.Length, moonOrbitRadius)
                        : GenerateRandomSpherePoints(table.Columns.Length, moonOrbitRadius);

                    foreach (Column column in table.Columns)
                    {
                        GameObject moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
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

                        AddLabel(moon, $"{column.Name} ({column.DataType})", 0.8f);
                        //AddLabel(moon, $"{column.Name} ({column.DataType})" + (columnIndex == 0 ? " (Primary Key)" : ""), 0.8f);

                        columnIndex++;
                    }
                    tableIndex++;
                }
                schemaIndex++;
            }
            databaseIndex++;
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