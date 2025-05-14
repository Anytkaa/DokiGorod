using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceScript : MonoBehaviour {

    static Rigidbody rb;
    public static Vector3 diceVelocity;
    private bool spacePressed = false;

    // Use this for initialization
    void Start () {
        rb = GetComponent<Rigidbody> ();
        rb.isKinematic = true; // Замораживаем объект при старте
    }
    
    // Размораживаем объект
    void UnfreezeObject() {
        rb.isKinematic = false;
    }

    // Update is called once per frame
    void Update () {
        diceVelocity = rb.velocity;

        if (Input.GetKeyDown (KeyCode.Mouse0) && !spacePressed) {
            spacePressed = true; // Устанавливаем флаг, что клавиша была нажата
            UnfreezeObject();
            snake.diceNumber = 0;
            float dirX = Random.Range (0, 500);
            float dirY = Random.Range (0, 500);
            float dirZ = Random.Range (0, 500);
            transform.position = new Vector3 (-200, 2, 0);
            transform.rotation = Quaternion.identity;
            rb.AddForce (transform.up * 500);
            rb.AddTorque (dirX, dirY, dirZ);
        }
    }
}