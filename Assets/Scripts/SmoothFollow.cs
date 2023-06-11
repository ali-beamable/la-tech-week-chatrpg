using UnityEngine;

public class SmoothFollow : MonoBehaviour
{
    //Insert your final position & rotation here as an empty Transform
    public Transform target;
    public float rotationSpeed=0.1f;
    
    void Update ()
    {
        if(!target)
            return;
        
        target.Rotate(Vector3.down, rotationSpeed * Time.deltaTime);
    }
}