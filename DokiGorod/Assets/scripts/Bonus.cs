using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bonus : MonoBehaviour
{
   
 
     void OnTriggerEnter(Collider other)
    {
        if (snake.diceNumber == 0) {
            snake.money += 200;
        }
    }
}
