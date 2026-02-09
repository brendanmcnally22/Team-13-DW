using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateP : MonoBehaviour
{
    public float rotationSpeed = 90;
    // Update is called once per frame
    void Update()
    {
        transform.rotation *= Quaternion.Euler(0, rotationSpeed*Time.deltaTime, 0);
    }
}
