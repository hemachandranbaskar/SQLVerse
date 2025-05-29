using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Text;

public class QueryBuilder : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI queryTextDisplay;
    private List<GameObject> selectedPlanets = new List<GameObject>();
    private Dictionary<GameObject, List<GameObject>> joinPairs = new Dictionary<GameObject, List<GameObject>>();

    public void OnPlanetGrabbed(GameObject planetClone)
    {
        if (!selectedPlanets.Contains(planetClone))
        {
            selectedPlanets.Add(planetClone);
            Debug.Log($"QueryBuilder: {planetClone.name} grabbed, added to query.");
            UpdateQueryDisplay();
        }
    }

    public void OnPlanetReleased(GameObject planetClone)
    {
        if (selectedPlanets.Contains(planetClone))
        {
            selectedPlanets.Remove(planetClone);
            Debug.Log($"QueryBuilder: {planetClone.name} released, removed from query.");
            Destroy(planetClone); // Clean up the clone
            UpdateQueryDisplay();
        }
    }

    public void OnPlanetSnapped(GameObject planetClone)
    {
        if (!selectedPlanets.Contains(planetClone))
        {
            selectedPlanets.Add(planetClone);
            Debug.Log($"QueryBuilder: {planetClone.name} snapped to platform.");
            UpdateQueryDisplay();
        }
    }

    public void OnPlanetUnsnapped(GameObject planetClone)
    {
        if (selectedPlanets.Contains(planetClone))
        {
            selectedPlanets.Remove(planetClone);
            Debug.Log($"QueryBuilder: {planetClone.name} unsnapped from platform.");
            Destroy(planetClone); // Clean up the clone
            UpdateQueryDisplay();
        }
    }

    public void AddJoin(GameObject moon1, GameObject moon2)
    {
        // Ensure the parent tables of both moons are selected
        GameObject table1 = moon1.transform.parent.gameObject;
        GameObject table2 = moon2.transform.parent.gameObject;
        GameObject table1Clone = selectedPlanets.Find(p => p.name == table1.name + "_Clone");
        GameObject table2Clone = selectedPlanets.Find(p => p.name == table2.name + "_Clone");

        if (table1Clone == null || table2Clone == null)
        {
            Debug.LogWarning("QueryBuilder: Cannot create join: One or both tables are not selected.");
            return;
        }

        if (!joinPairs.ContainsKey(moon1))
        {
            joinPairs[moon1] = new List<GameObject>();
        }
        if (!joinPairs[moon1].Contains(moon2))
        {
            joinPairs[moon1].Add(moon2);
            Debug.Log($"QueryBuilder: Join added between {moon1.name} and {moon2.name}");
            UpdateQueryDisplay();
        }
    }

    public void UpdateQueryDisplay()
    {
        if (queryTextDisplay == null)
        {
            Debug.LogWarning("QueryBuilder: Query Text Display is not assigned in QueryBuilder!");
            return;
        }

        string sqlQuery = GenerateSQLQuery();
        queryTextDisplay.text = sqlQuery;
        Debug.Log($"QueryBuilder: Generated SQL Query: {sqlQuery}");
    }

    private string GenerateSQLQuery()
    {
        if (selectedPlanets.Count == 0)
        {
            return "SELECT * FROM ..."; // Default placeholder when no tables are selected
        }

        StringBuilder sqlQuery = new StringBuilder();
        sqlQuery.Append("SELECT ");

        // Collect columns from selected planets
        List<string> selectedColumns = new List<string>();
        foreach (GameObject planet in selectedPlanets)
        {
            string tableName = planet.name.Replace("_Clone", "");
            foreach (Transform child in planet.transform)
            {
                if (child.CompareTag("Moon"))
                {
                    string columnName = ExtractColumnName(child.name);
                    selectedColumns.Add($"{tableName}.{columnName}");
                }
            }
        }

        if (selectedColumns.Count > 0)
        {
            sqlQuery.Append(string.Join(", ", selectedColumns));
        }
        else
        {
            sqlQuery.Append("*"); // Default to all columns if none are explicitly selected
        }

        sqlQuery.Append(" FROM ");

        // Collect tables and their schemas
        List<string> selectedTables = new List<string>();
        Dictionary<string, string> tableToSchema = new Dictionary<string, string>();
        foreach (GameObject planet in selectedPlanets)
        {
            string tableName = planet.name.Replace("_Clone", "");
            string schemaName = "RetailStore";// planet.transform.parent.name; // Schema is the parent of the planet
            selectedTables.Add(tableName);
            tableToSchema[tableName] = schemaName;
        }

        // Start with the first table
        string firstTable = selectedTables[0];
        sqlQuery.Append($"{tableToSchema[firstTable]}.{firstTable}");

        // Add JOIN clauses
        foreach (var pair in joinPairs)
        {
            GameObject moon1 = pair.Key;
            string table1 = moon1.transform.parent.name;
            string schema1 = moon1.transform.parent.parent.name;
            string column1 = ExtractColumnName(moon1.name);

            // Skip if table1 is not selected
            if (!selectedTables.Contains(table1))
            {
                continue;
            }

            foreach (GameObject moon2 in pair.Value)
            {
                string table2 = moon2.transform.parent.name;
                string schema2 = moon2.transform.parent.parent.name;
                string column2 = ExtractColumnName(moon2.name);

                // Skip if table2 is not selected
                if (!selectedTables.Contains(table2))
                {
                    continue;
                }

                sqlQuery.Append($" JOIN {schema2}.{table2} ON {schema1}.{table1}.{column1} = {schema2}.{table2}.{column2}");
            }
        }

        return sqlQuery.ToString();
    }

    private string ExtractColumnName(string moonName)
    {
        // Extracts the column name from a moon's name (e.g., "id (integer) (Primary Key)" -> "id")
        int spaceIndex = moonName.IndexOf(' ');
        if (spaceIndex > 0)
        {
            return moonName.Substring(0, spaceIndex);
        }
        return moonName;
    }
}