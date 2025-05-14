using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public GameObject player;

    public void SwitchScene(string sceneName)
    {
        if (player != null)
        {   PlayerPrefs.SetFloat("PlayerRotationY", player.transform.rotation.y);
            PlayerPrefs.SetFloat("PlayerPositionX", player.transform.position.x);
            PlayerPrefs.SetFloat("PlayerPositionY", player.transform.position.y);
            PlayerPrefs.SetFloat("PlayerPositionZ", player.transform.position.z);
        }

        SceneManager.LoadScene(sceneName);

    }
    private void OnApplicationQuit()
    {
        if (player != null)
        {   PlayerPrefs.DeleteKey("PlayerRotationY");
            PlayerPrefs.DeleteKey("PlayerPositionX");
            PlayerPrefs.DeleteKey("PlayerPositionY");
            PlayerPrefs.DeleteKey("PlayerPositionZ");
        }
    }
}
