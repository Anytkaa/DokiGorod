using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class eeRotate : MonoBehaviour
{
    public float rotationAmount = 0f;
    public GameObject player;
 
     void OnTriggerEnter(Collider other)
    {
        player.transform.Rotate(0, rotationAmount, 0);
    }
}
