using UnityEngine;

public class Orbiter : MonoBehaviour
{
    [SerializeField] public float orbitSpeed = 10f;

    void Update()
    {
        if (transform.parent != null)
        {
            transform.RotateAround(transform.parent.position, Vector3.up, orbitSpeed * Time.deltaTime);
        }
    }
}