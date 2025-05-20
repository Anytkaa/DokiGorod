// SceneSwitcher.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public GameObject player;

    [Header("UI �������� ����")]
    public GameObject quitConfirmationPanel; // ������ "����� �� ����?" � ������� ����
    public GameObject backgroundDimmerPanel;   // ������ ��� ���������� ���� � ������� ����
    public string mainMenuSceneName = "Menu"; // ��� ����� �������� ����

    [Header("UI ������� ����� (�����)")]
    public GameObject pauseMenuPanelInGame;   // ������ ����� � �������� "����������", "����� � ����", "����� �� ����"
    public string gameSceneName = "MainScene";  // ��� ����� �������� ������� �����
    // public GameObject gameSceneDimmerPanel; // �����������: ��������� ����������� ��� ������� �����, ���� ����� ������ �����

    private bool isGamePaused = false; // ���� ��� ������������ ��������� ����� � ������� �����

    void Start()
    {
        // �������� ������ ��� ������� ��������������� �����
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);
            if (backgroundDimmerPanel != null) backgroundDimmerPanel.SetActive(false);
        }
        else if (SceneManager.GetActiveScene().name == gameSceneName)
        {
            if (pauseMenuPanelInGame != null) pauseMenuPanelInGame.SetActive(false);
            // if (gameSceneDimmerPanel != null) gameSceneDimmerPanel.SetActive(false); // ���� ����������� ��������� �����������
            Time.timeScale = 1f; // ��������, ��� ����� ���� ��������� ��� ������ ������� �����
            isGamePaused = false;
        }
    }

    void Update()
    {
        // ������ ��� �������� ����
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleQuitConfirmationPanelMainMenu();
            }
        }
        // ������ ��� ������� ����� (MainScene)
        else if (SceneManager.GetActiveScene().name == gameSceneName)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePauseMenuInGame();
            }
        }
    }

    // --- ������ ��� UI �������� ���� ---
    public void ToggleQuitConfirmationPanelMainMenu()
    {
        if (quitConfirmationPanel == null || backgroundDimmerPanel == null) return;
        bool isActive = quitConfirmationPanel.activeSelf;
        quitConfirmationPanel.SetActive(!isActive);
        backgroundDimmerPanel.SetActive(!isActive);
    }

    public void ConfirmQuitGameFromMainMenu() // ������������ ��� �������
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void CancelQuitGameFromMainMenu() // ������������ ��� �������
    {
        if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);
        if (backgroundDimmerPanel != null) backgroundDimmerPanel.SetActive(false);
    }

    // --- ������ ��� UI ������� ����� (�����) ---
    public void TogglePauseMenuInGame()
    {
        if (pauseMenuPanelInGame == null)
        {
            Debug.LogWarning("������ ���� ����� �� ��������� � SceneSwitcher.");
            return;
        }

        isGamePaused = !isGamePaused;
        pauseMenuPanelInGame.SetActive(isGamePaused);
        Time.timeScale = isGamePaused ? 0f : 1f; // ������ ���� �� ����� ��� �������

        // �����������: ��������/��������� ����������� ��� ������� �����
        // if (gameSceneDimmerPanel != null)
        // {
        //     gameSceneDimmerPanel.SetActive(isGamePaused);
        // }
    }

    // ������ "����������" � ���� �����
    public void ResumeGame()
    {
        if (pauseMenuPanelInGame != null)
        {
            pauseMenuPanelInGame.SetActive(false);
        }
        // if (gameSceneDimmerPanel != null) gameSceneDimmerPanel.SetActive(false); // ���� ����������� �����������
        Time.timeScale = 1f;
        isGamePaused = false;
    }

    // ������ "����� � ����" � ���� �����
    public void ReturnToMainMenuFromGame()
    {
        ResumeGame(); // ������� � ����� � ������� UI ����� ������ �����
        SwitchScene(mainMenuSceneName); // ���������� ������������ ����� SwitchScene
    }

    // ������ "����� �� ����" � ���� �����
    public void QuitGameFromGameScene()
    {
        Debug.Log("����� �� ���� �� ������� �����...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // --- ����� ������ ---
    public void SwitchScene(string sceneName)
    {
        // ��������������� �����, ���� ��� ���� �� �����
        Time.timeScale = 1f;
        isGamePaused = false; // �� ������ ������

        if (player != null && SceneManager.GetActiveScene().name == gameSceneName)
        {
            PlayerPrefs.SetFloat("PlayerRotationY", player.transform.rotation.eulerAngles.y);
            PlayerPrefs.SetFloat("PlayerPositionX", player.transform.position.x);
            PlayerPrefs.SetFloat("PlayerPositionY", player.transform.position.y);
            PlayerPrefs.SetFloat("PlayerPositionZ", player.transform.position.z);
            PlayerPrefs.Save();
            Debug.Log("Player data saved from game scene.");
        }
        SceneManager.LoadScene(sceneName);
    }

    private void OnApplicationQuit()
    {
        if (player != null)
        {
            // ������ ������� PlayerPrefs (������, ����� �� ��� ���)
        }
    }

    public void OpenLink(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        Application.OpenURL(url);
    }
}