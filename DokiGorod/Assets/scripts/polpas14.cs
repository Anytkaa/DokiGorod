using UnityEngine;
using UnityEngine.UI;

public class polpas14 : MonoBehaviour
{
    public static bool pas14 = false;
    public GameObject buttonp14;

    public void OnButtonClick()
    {
        snake.money -= 300;
        pas14 = true;
        buttonp14.gameObject.SetActive(false);
    }
}
