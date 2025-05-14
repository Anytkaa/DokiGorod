using UnityEngine;
using UnityEngine.UI;

public class Rotate : MonoBehaviour
{public GameObject player;
    public float rotationAmount = 45f; 
    public Button rotateButton; 
    public GameObject buttonl;
    public GameObject buttonr;
    public GameObject buttond;
    private bool hasRotated = false; 

    void Start()
    {

        rotateButton.onClick.AddListener(RotateObject);
    }


    void RotateObject()
    {

        if (!hasRotated)
        {

            player.transform.Rotate(0, rotationAmount, 0);

            buttonl.SetActive(false);
        buttonr.SetActive(false);
        snake.maxim = 1;
            snake.moveSpeed = 5;
            snake.diceNumber = RotateCheck.nummm - 1;
            hasRotated = true;
        }
    }

    public void ResetRotation()
    {
        hasRotated = false;
    }
}
