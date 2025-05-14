using UnityEngine;
using UnityEngine.SceneManagement;

public class False : MonoBehaviour
{
    public GameObject player;

    // Метод для смены сцены
    public void SwitchScene(string sceneName)
    {
        if (player != null)
        {
            // Сохраняем координаты игрока перед сменой сцены
            PlayerPrefs.SetFloat("PlayerPositionX", player.transform.position.x);
            PlayerPrefs.SetFloat("PlayerPositionY", player.transform.position.y);
            PlayerPrefs.SetFloat("PlayerPositionZ", player.transform.position.z);
        }

        SceneManager.LoadScene(sceneName);
        snake.maxon = 2;

    }
    private void OnApplicationQuit()
    {
        if (player != null)
        {
            // Сбрасываем сохраненные координаты
            PlayerPrefs.DeleteKey("PlayerPositionX");
            PlayerPrefs.DeleteKey("PlayerPositionY");
            PlayerPrefs.DeleteKey("PlayerPositionZ");
        }
    }
}
