using UnityEngine;

public class rotate : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    void Update()
    {
        transform.Rotate(Vector3.up, 90f * Time.deltaTime);
    }
}
