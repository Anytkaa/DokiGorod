using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pas20 : MonoBehaviour
{
    public GameObject buttonp20;
    void OnTriggerEnter(Collider other)
    {
        if (polpas20.pas20 == false) {
        buttonp20.SetActive(true);
        }
    }
}
