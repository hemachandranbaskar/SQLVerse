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
            AddGalaxyParticleSystem(galaxy);
            AddLabel(galaxy, db.Name, 10f);

            int schemaIndex = 0;
            foreach (Schema schema in db.Schemas)
            {
                GameObject solarSystem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                solarSystem.name = schema.Name;
                solarSystem.transform.localScale = Vector3.one * 5f;
                float solarSystemDistance = 15f + schemaIndex * 10f;
                solarSystem.transform.SetParent(galaxy.transform);
                solarSystem.transform.localPosition = new Vector3(solarSystemDistance, 0, 0);
                solarSystem.GetComponent<Renderer>().material.color = Color.yellow;
                AddLabel(solarSystem, schema.Name, 2f);

                int tableIndex = 0;
                foreach (Table table in schema.Tables)
                {
                    GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    planet.name = table.Name;
                    planet.transform.localScale = Vector3.one * 3f;
                    float planetDistance = planetSpacing + tableIndex * 5f;
                    planet.transform.SetParent(solarSystem.transform);
                    planet.transform.localPosition = new Vector3(planetDistance, 0, 0);
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

    private void AddGalaxyParticleSystem(GameObject galaxy)
    {
        ParticleSystem ps = galaxy.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startSize = 0.1f;
        main.startSpeed = 0;
        main.maxParticles = 2000;
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 100;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 50f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetColor("_EmissionColor", Color.white);
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