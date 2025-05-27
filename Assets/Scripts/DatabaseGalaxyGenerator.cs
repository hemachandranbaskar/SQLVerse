using UnityEngine;
using TMPro;

public class DatabaseGalaxyGenerator : MonoBehaviour
{
    [SerializeField] private string jsonFileName = "SampleDatabase";
    private DatabaseRoot databaseRoot;
    [SerializeField] private bool useBoundarySpheres = false;

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
        float moonOrbitSpacing = 0.5f;
        float planetSpacing = 10f;
        int databaseIndex = 0;

        foreach (Database db in databaseRoot.Databases)
        {
            GameObject galaxy = new GameObject(db.Name);
            galaxy.transform.position = new Vector3(databaseIndex * galaxySpacing, 0, 0);
            AddGalaxyParticleSystem(galaxy, Color.white, 50f, 2000);
            AddLabel(galaxy, db.Name, 10f);

            int schemaIndex = 0;
            float schemaAngleStep = 360f / Mathf.Max(1, db.Schemas.Length);
            foreach (Schema schema in db.Schemas)
            {
                GameObject schemaCluster = new GameObject(schema.Name);
                schemaCluster.transform.SetParent(galaxy.transform);
                float angle = schemaIndex * schemaAngleStep * Mathf.Deg2Rad;
                float schemaDistance = 20f;
                schemaCluster.transform.localPosition = new Vector3(Mathf.Cos(angle) * schemaDistance, 0, Mathf.Sin(angle) * schemaDistance);
                AddSchemaParticleSystem(schemaCluster, GetSchemaColor(schemaIndex), 10f, 200);
                if (useBoundarySpheres)
                {
                    AddBoundarySphere(schemaCluster, 15f, GetSchemaColor(schemaIndex));
                }
                AddLabel(schemaCluster, schema.Name, 3f);

                int tableIndex = 0;
                float tableAngleStep = 360f / Mathf.Max(1, schema.Tables.Length);
                foreach (Table table in schema.Tables)
                {
                    GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    planet.name = table.Name;
                    planet.transform.localScale = Vector3.one * 5f; //Vector3.one * 3f;
                    float planetAngle = tableIndex * tableAngleStep * Mathf.Deg2Rad;
                    float planetDistance = 5f + tableIndex * 3f; ; // planetSpacing + tableIndex * 5f;
                    planet.transform.SetParent(schemaCluster.transform);
                    planet.transform.localPosition = new Vector3(Mathf.Cos(planetAngle) * planetDistance, 0, Mathf.Sin(planetAngle) * planetDistance);
                    planet.GetComponent<Renderer>().material.color = Color.blue;
                    planet.AddComponent<Orbiter>().orbitSpeed = 20f;
                    AddLabel(planet, table.Name, 1.5f);

                    int columnIndex = 0;
                    foreach (Column column in table.Columns)
                    {
                        GameObject moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        moon.name = $"{column.Name} ({column.DataType})";
                        moon.transform.localScale = Vector3.one * 1f;
                        float moonOrbitDistance = 1f + columnIndex * moonOrbitSpacing;
                        moon.transform.SetParent(planet.transform);
                        moon.transform.localPosition = new Vector3(moonOrbitDistance, 0, 0);
                        moon.GetComponent<Renderer>().material.color = GetColorForDataType(column.DataType);
                        moon.AddComponent<Orbiter>().orbitSpeed = 50f;
                        AddLabel(moon, $"{column.Name} ({column.DataType})", 0.8f);

                        columnIndex++;
                    }
                    tableIndex++;
                }
                schemaIndex++;
            }
            databaseIndex++;
        }
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