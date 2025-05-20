// SceneSwitcher.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public GameObject player;

    [Header("UI Главного Меню")]
    public GameObject quitConfirmationPanel; // Панель "Выйти из игры?" в главном меню
    public GameObject backgroundDimmerPanel;   // Панель для затемнения фона в главном меню
    public string mainMenuSceneName = "Menu"; // Имя сцены главного меню

    [Header("UI Игровой Сцены (Пауза)")]
    public GameObject pauseMenuPanelInGame;   // Панель паузы с кнопками "Продолжить", "Выйти в меню", "Выйти из игры"
    public string gameSceneName = "MainScene";  // Имя вашей основной игровой сцены
    // public GameObject gameSceneDimmerPanel; // Опционально: отдельный затемнитель для игровой сцены, если нужен другой стиль

    private bool isGamePaused = false; // Флаг для отслеживания состояния паузы в игровой сцене

    void Start()
    {
        // Скрываем панели при запуске соответствующей сцены
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);
            if (backgroundDimmerPanel != null) backgroundDimmerPanel.SetActive(false);
        }
        else if (SceneManager.GetActiveScene().name == gameSceneName)
        {
            if (pauseMenuPanelInGame != null) pauseMenuPanelInGame.SetActive(false);
            // if (gameSceneDimmerPanel != null) gameSceneDimmerPanel.SetActive(false); // Если используете отдельный затемнитель
            Time.timeScale = 1f; // Убедимся, что время идет нормально при старте игровой сцены
            isGamePaused = false;
        }
    }

    void Update()
    {
        // Логика для Главного Меню
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleQuitConfirmationPanelMainMenu();
            }
        }
        // Логика для Игровой Сцены (MainScene)
        else if (SceneManager.GetActiveScene().name == gameSceneName)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePauseMenuInGame();
            }
        }
    }

    // --- Методы для UI Главного Меню ---
    public void ToggleQuitConfirmationPanelMainMenu()
    {
        if (quitConfirmationPanel == null || backgroundDimmerPanel == null) return;
        bool isActive = quitConfirmationPanel.activeSelf;
        quitConfirmationPanel.SetActive(!isActive);
        backgroundDimmerPanel.SetActive(!isActive);
    }

    public void ConfirmQuitGameFromMainMenu() // Переименовал для ясности
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void CancelQuitGameFromMainMenu() // Переименовал для ясности
    {
        if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);
        if (backgroundDimmerPanel != null) backgroundDimmerPanel.SetActive(false);
    }

    // --- Методы для UI Игровой Сцены (Пауза) ---
    public void TogglePauseMenuInGame()
    {
        if (pauseMenuPanelInGame == null)
        {
            Debug.LogWarning("Панель меню паузы не назначена в SceneSwitcher.");
            return;
        }

        isGamePaused = !isGamePaused;
        pauseMenuPanelInGame.SetActive(isGamePaused);
        Time.timeScale = isGamePaused ? 0f : 1f; // Ставим игру на паузу или снимаем

        // Опционально: включаем/выключаем затемнитель для игровой сцены
        // if (gameSceneDimmerPanel != null)
        // {
        //     gameSceneDimmerPanel.SetActive(isGamePaused);
        // }
    }

    // Кнопка "Продолжить" в меню паузы
    public void ResumeGame()
    {
        if (pauseMenuPanelInGame != null)
        {
            pauseMenuPanelInGame.SetActive(false);
        }
        // if (gameSceneDimmerPanel != null) gameSceneDimmerPanel.SetActive(false); // Если используете затемнитель
        Time.timeScale = 1f;
        isGamePaused = false;
    }

    // Кнопка "Выйти в меню" в меню паузы
    public void ReturnToMainMenuFromGame()
    {
        ResumeGame(); // Снимаем с паузы и убираем UI перед сменой сцены
        SwitchScene(mainMenuSceneName); // Используем существующий метод SwitchScene
    }

    // Кнопка "Выйти из игры" в меню паузы
    public void QuitGameFromGameScene()
    {
        Debug.Log("Выход из игры из игровой сцены...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // --- Общие методы ---
    public void SwitchScene(string sceneName)
    {
        // Восстанавливаем время, если оно было на паузе
        Time.timeScale = 1f;
        isGamePaused = false; // На всякий случай

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
            // Логика очистки PlayerPrefs (решите, нужна ли она вам)
        }
    }

    public void OpenLink(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        Application.OpenURL(url);
    }
}