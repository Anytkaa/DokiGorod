using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pas14 : MonoBehaviour
{
    public GameObject buttonp14;
    void OnTriggerEnter(Collider other)
    {
        if (polpas14.pas14 == false) {
        buttonp14.SetActive(true);
        }
    }
}
