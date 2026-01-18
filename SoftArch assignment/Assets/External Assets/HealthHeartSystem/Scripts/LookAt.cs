using UnityEngine;

public class LookAt : MonoBehaviour
{
    public bool invert = false;
    // Update is called once per frame
    void FixedUpdate()
    {
        transform.LookAt(Camera.main.transform.position);
        if (invert) transform.Rotate(0f, 180f, 0f, Space.Self); // Invert on Y axis locally
    }
}
