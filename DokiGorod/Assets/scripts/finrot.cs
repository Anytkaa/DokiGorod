using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class finrot : MonoBehaviour
{
public GameObject player;
 public float xpos = 0f;
     void OnTriggerEnter(Collider other)
    {
        player.transform.position = new Vector3(xpos, 1f, 0f);
        player.transform.rotation = Quaternion.Euler(0, 0, 0);
    }
}
