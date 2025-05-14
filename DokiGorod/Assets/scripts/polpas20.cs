using UnityEngine;
using UnityEngine.UI;

public class polpas20 : MonoBehaviour
{
    public static bool pas20 = false;
    public GameObject buttonp20;

    public void OnButtonClick()
    {
        snake.money -= 300;
        pas20 = true;
        buttonp20.gameObject.SetActive(false);
    }
}
