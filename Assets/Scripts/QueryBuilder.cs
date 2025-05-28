using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class QueryBuilder : MonoBehaviour
{
    private List<GameObject> selectedPlanets = new List<GameObject>();
    private Dictionary<GameObject, List<GameObject>> joinPairs = new Dictionary<GameObject, List<GameObject>>();

    public void OnPlanetGrabbed(GameObject planetClone)
    {
        if (!selectedPlanets.Contains(planetClone))
        {
            selectedPlanets.Add(planetClone);
            Debug.Log($"{planetClone.name} grabbed, added to query.");
        }
    }

    public void OnPlanetReleased(GameObject planet)
    {
        GameObject originalPlanet = planet.transform.parent.Find(planet.name.Replace("_Clone", ""))?.gameObject;
        if (originalPlanet != null && selectedPlanets.Contains(originalPlanet))
        {
            selectedPlanets.Remove(originalPlanet);
            Debug.Log($"{originalPlanet.name} released, removed from query.");
            Destroy(planet); // Clean up clone
        }
    }

    public void UpdateQueryDisplay()
    {
        Debug.Log("UpdateQueryDisplay called");

        StringBuilder sqlQuery = new StringBuilder();
        sqlQuery.Append("SELECT ");

        List<string> selectedColumns = new List<string>();
        foreach (GameObject planet in selectedPlanets)
        {
            foreach (Transform child in planet.transform)
            {
                if (child.CompareTag("Moon"))
                {
                    string columnName = child.name.Replace(" (Primary Key)", "").Split(' ')[0];
                    selectedColumns.Add($"{planet.name.Replace("_Clone", "")}.{columnName}");
                }
            }
        }
        sqlQuery.Append(string.Join(", ", selectedColumns.ToArray()));
        sqlQuery.Append(" FROM ");

        List<string> selectedTables = new List<string>();
        foreach (GameObject planet in selectedPlanets)
        {
            selectedTables.Add(planet.name.Replace("_Clone", ""));
        }
        sqlQuery.Append(string.Join(", ", selectedTables.ToArray()));

        bool hasJoins = false;
        foreach (var pair in joinPairs)
        {
            foreach (GameObject joinedMoon in pair.Value)
            {
                string table1 = pair.Key.transform.parent.name.Replace("_Clone", "");
                string column1 = pair.Key.name.Replace(" (Primary Key)", "").Split(' ')[0];
                string table2 = joinedMoon.transform.parent.name.Replace("_Clone", "");
                string column2 = joinedMoon.name.Replace(" (Primary Key)", "").Split(' ')[0];
                if (!hasJoins)
                {
                    sqlQuery.Append(" JOIN ");
                    hasJoins = true;
                }
                else
                {
                    sqlQuery.Append(" JOIN ");
                }
                sqlQuery.Append($"{table2} ON {table1}.{column1} = {table2}.{column2}");
            }
        }

        Debug.Log(sqlQuery.ToString());
    }

    // Method to handle moon joins (to be expanded)
    public void OnMoonJoined(GameObject moon1, GameObject moon2)
    {
        if (!joinPairs.ContainsKey(moon1))
        {
            joinPairs[moon1] = new List<GameObject>();
        }
        if (!joinPairs[moon1].Contains(moon2))
        {
            joinPairs[moon1].Add(moon2);
            UpdateQueryDisplay();
        }
    }
}