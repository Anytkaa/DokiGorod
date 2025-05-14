using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewDiceCheckZoneScript : MonoBehaviour
{
    Vector3 diceVelocity;
    private bool hasProcessedTrigger = false;
    // Update is called once per frame
    void FixedUpdate()
    {
        diceVelocity = DiceScript.diceVelocity;
    }

    void OnTriggerStay(Collider col)
    {
         if (!hasProcessedTrigger)
    {
        if (diceVelocity == Vector3.zero && snake.maxon == 1)
        {
            switch (col.gameObject.name)
            {
                case "Side1":
                    snake.diceNumber = 6;
                    break;
                case "Side2":
                    snake.diceNumber = 5;
                    break;
                case "Side3":
                    snake.diceNumber = 4;
                    break;
                case "Side4":
                    snake.diceNumber = 3;
                    break;
                case "Side5":
                    snake.diceNumber = 2;
                    break;
                case "Side6":
                    snake.diceNumber = 1;
                    break;
            }
            hasProcessedTrigger = true;
            StartCoroutine(WaitAndSwitchScene(1f, "SampleScene"));
        }
        else if (diceVelocity == Vector3.zero && snake.maxon == 2) {
            
            snake.diceNumber = 0;
            snake.maxon = 1;
            hasProcessedTrigger = true;
            StartCoroutine(WaitAndSwitchScene(1f, "SampleScene"));
        }
        }
    }

    IEnumerator WaitAndSwitchScene(float waitTime, string sceneName)
    {
        yield return new WaitForSeconds(waitTime);
        SceneManager.LoadScene(sceneName);
    }
}
