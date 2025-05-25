// snake.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class snake : MonoBehaviour
{
    // --- Публичные переменные для настройки в Инспекторе ---
    [Header("Объекты и UI")]
    public GameObject buttonRollDice;    // Кнопка "Кинь кубик"
    public Text movesValueText;          // UI Text для отображения количества оставшихся ходов
    // public GameObject pas14; // Если используется
    // public GameObject pas20; // Если используется

    [Header("Настройки Движения")]
    public float stepDistance = 10.0f;   // Расстояние одного шага по основной дороге
    public float moveDuration = 0.5f;    // Длительность анимации одного шага
    public float rotateDuration = 0.3f;  // Длительность анимации поворота
    public float loopMoveDurationPerWaypoint = 0.9f; // Длительность движения к одной точке петли
    public float loopRotateSpeed = 5f;   // Скорость плавного поворота на петле

    [Header("UI для Развилки")]
    public GameObject turnChoiceUI;      // Панель с кнопками выбора (лево/право)
    public Button turnLeftButton;
    public Button turnRightButton;

    [Header("Настройки Боковых Путей (Петель)")]
    public Transform[] leftLoopWaypoints;  // Массив Transform'ов точек для левой петли
    public int leftLoopCost = 3;         // Сколько шагов стоит левая петля (если фиксировано, иначе можно считать по waypoints.Length)
    public Transform[] rightLoopWaypoints; // Массив Transform'ов точек для правой петли
    public int rightLoopCost = 3;        // Сколько шагов стоит правая петля

    // --- Статические и приватные переменные ---
    public static int money = 2000;        // Статическое поле для денег

    private bool isMoving = false;             // Флаг: персонаж в процессе активного движения (основной путь или поворот)
    private bool waitingForTurnChoice = false; // Флаг: персонаж на развилке и ждет выбора игрока
    private bool isMovingOnLoop = false;       // Флаг: персонаж движется по боковой петле
    private int stepsRemainingAfterTurn = 0;   // Сколько шагов было доступно на момент выбора на развилке
    private Coroutine primaryMoveCoroutine;    // Ссылка на корутину движения по основному пути/поворота
    private Coroutine loopMoveCoroutine;       // Ссылка на корутину движения по петле
    private int currentDiceSteps = 0;          // Общее количество шагов, полученное от текущего броска кубика

    // Ключи для PlayerPrefs
    private const string PosXKey = "PlayerPositionX_Snake_DokiGorod";
    private const string PosYKey = "PlayerPositionY_Snake_DokiGorod";
    private const string PosZKey = "PlayerPositionZ_Snake_DokiGorod";
    private const string RotYKey = "PlayerRotationY_Snake_DokiGorod";
    private const string DiceRollKey = "LastDiceRoll";

    // --- Методы Жизненного Цикла Unity ---
    void Start()
    {
        gameObject.name = "Player_Snake";
        Debug.Log("Snake.cs Start() called on scene: " + SceneManager.GetActiveScene().name);

        LoadPlayerState();

        if (PlayerPrefs.HasKey(DiceRollKey))
        {
            int stepsFromDice = PlayerPrefs.GetInt(DiceRollKey);
            Debug.Log("Snake: Found dice roll result in PlayerPrefs (" + DiceRollKey + "): " + stepsFromDice);
            PlayerPrefs.DeleteKey(DiceRollKey);
            PlayerPrefs.Save();

            if (stepsFromDice > 0)
            {
                StartMoving(stepsFromDice);
            }
            else
            {
                UpdateMovesValueUIText(0);
                UpdateButtonRollDiceVisibility();
            }
        }
        else
        {
            UpdateMovesValueUIText(0);
            UpdateButtonRollDiceVisibility();
        }

        if (turnChoiceUI != null) turnChoiceUI.SetActive(false);

        if (turnLeftButton != null)
        {
            turnLeftButton.onClick.RemoveAllListeners();
            turnLeftButton.onClick.AddListener(() => HandleTurnChoice(true));
        }
        if (turnRightButton != null)
        {
            turnRightButton.onClick.RemoveAllListeners();
            turnRightButton.onClick.AddListener(() => HandleTurnChoice(false));
        }
    }

    void OnApplicationQuit()
    {
        Debug.Log("Snake: Application quitting. Clearing saved player state for next session.");
        PlayerPrefs.DeleteKey(PosXKey);
        PlayerPrefs.DeleteKey(PosYKey);
        PlayerPrefs.DeleteKey(PosZKey);
        PlayerPrefs.DeleteKey(RotYKey);
        PlayerPrefs.Save();
    }

    // --- Управление Состоянием Игрока (Сохранение/Загрузка) ---
    void LoadPlayerState()
    {
        if (PlayerPrefs.HasKey(PosXKey))
        {
            float x = PlayerPrefs.GetFloat(PosXKey);
            float y = PlayerPrefs.GetFloat(PosYKey);
            float z = PlayerPrefs.GetFloat(PosZKey);
            float savedRotationY = PlayerPrefs.GetFloat(RotYKey, transform.rotation.eulerAngles.y);
            transform.position = new Vector3(x, y, z);
            transform.rotation = Quaternion.Euler(0, savedRotationY, 0);
            Debug.Log("Snake: Player state loaded. Pos: " + transform.position + ", RotY: " + savedRotationY);
        }
        else
        {
            Debug.Log("Snake: No saved player state found. Starting at initial editor/scene position.");
        }
    }

    public void SavePlayerState()
    {
        PlayerPrefs.SetFloat(PosXKey, transform.position.x);
        PlayerPrefs.SetFloat(PosYKey, transform.position.y);
        PlayerPrefs.SetFloat(PosZKey, transform.position.z);
        PlayerPrefs.SetFloat(RotYKey, transform.rotation.eulerAngles.y);
        PlayerPrefs.Save();
        Debug.Log("Snake: Player state saved. Pos: " + transform.position + ", RotY: " + transform.rotation.eulerAngles.y);
    }

    // --- Основная Логика Движения ---
    public void StartMoving(int steps)
    {
        if (IsCurrentlyExecutingMovement())
        {
            Debug.LogWarning("Snake: Already moving/waiting. New movement for " + steps + " steps ignored.");
            return;
        }
        currentDiceSteps = steps;
        Debug.Log("Snake: StartMoving called for " + currentDiceSteps + " steps.");
        UpdateUIAndButton();

        if (primaryMoveCoroutine != null) StopCoroutine(primaryMoveCoroutine);
        primaryMoveCoroutine = StartCoroutine(MoveStepsCoroutine(currentDiceSteps));
    }

    IEnumerator MoveStepsCoroutine(int stepsToMoveInitially) // Движение по основной дороге
    {
        if (isMovingOnLoop)
        {
            Debug.Log("Snake: MoveStepsCoroutine called, but currently on a loop. Aborting.");
            yield break;
        }
        isMoving = true;
        UpdateUIAndButton();

        while (currentDiceSteps > 0 && !waitingForTurnChoice && !isMovingOnLoop)
        {
            Vector3 startPosition = transform.position;
            Vector3 endPosition = startPosition + transform.forward * stepDistance;
            float elapsedTime = 0;
            Debug.Log($"Snake: Main path step. From {startPosition} to {endPosition}. Steps left: {currentDiceSteps - 1}");

            while (elapsedTime < moveDuration)
            {
                transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / moveDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.position = endPosition;
            currentDiceSteps--;
            UpdateMovesValueUIText(currentDiceSteps);
        }

        isMoving = false;
        Debug.Log("Snake: MoveStepsCoroutine finished. WaitingForTurn: " + waitingForTurnChoice + ", OnLoop: " + isMovingOnLoop + ", StepsLeft: " + currentDiceSteps);

        if (!waitingForTurnChoice && !isMovingOnLoop) // Если не прервано развилкой или петлей
        {
            OnMovementFinished();
        }
    }

    void OnMovementFinished()
    {
        Debug.Log("Snake: All movement sequence completed. Final steps: " + currentDiceSteps);
        if (currentDiceSteps < 0) currentDiceSteps = 0;
        isMoving = false;
        isMovingOnLoop = false; // Убедимся, что все флаги движения сброшены
        waitingForTurnChoice = false;
        UpdateUIAndButton();
        SavePlayerState();
    }

    // --- Логика Развилок и Боковых Петель ---
    public void ReachedTurnPoint()
    {
        if (waitingForTurnChoice)
        {
            Debug.Log("Snake: ReachedTurnPoint, but already waiting.");
            return;
        }
        Debug.Log("Snake: Reached Turn Point. Current steps: " + currentDiceSteps);

        // Останавливаем любое текущее движение
        isMoving = false;
        isMovingOnLoop = false;
        if (primaryMoveCoroutine != null) { StopCoroutine(primaryMoveCoroutine); primaryMoveCoroutine = null; }
        if (loopMoveCoroutine != null) { StopCoroutine(loopMoveCoroutine); loopMoveCoroutine = null; }

        waitingForTurnChoice = true;
        stepsRemainingAfterTurn = currentDiceSteps; // Сохраняем шаги на момент развилки

        if (turnChoiceUI != null) turnChoiceUI.SetActive(true);
        UpdateUIAndButton();
    }

    public void HandleTurnChoice(bool turnLeft)
    {
        if (!waitingForTurnChoice)
        {
            Debug.LogWarning("Snake: HandleTurnChoice called, but not waiting.");
            return;
        }
        if (turnChoiceUI != null) turnChoiceUI.SetActive(false);
        waitingForTurnChoice = false; // Сразу сбрасываем, так как выбор сделан

        currentDiceSteps = stepsRemainingAfterTurn; // Восстанавливаем шаги для обработки
        Debug.Log($"Snake: HandleTurnChoice. Left: {turnLeft}. Steps available: {currentDiceSteps}");

        Transform[] targetLoopWaypoints = turnLeft ? leftLoopWaypoints : rightLoopWaypoints;
        int loopCost = turnLeft ? leftLoopCost : rightLoopCost; // Используем заданную стоимость петли

        if (targetLoopWaypoints != null && targetLoopWaypoints.Length > 0)
        {
            if (currentDiceSteps >= loopCost) // Проверяем, хватает ли шагов на СТОИМОСТЬ петли
            {
                Debug.Log($"Snake: Starting {(turnLeft ? "left" : "right")} loop. Cost: {loopCost}.");
                isMovingOnLoop = true;
                UpdateUIAndButton();
                if (loopMoveCoroutine != null) StopCoroutine(loopMoveCoroutine);
                loopMoveCoroutine = StartCoroutine(MoveAlongLoopCoroutine(targetLoopWaypoints, loopCost));
            }
            else
            {
                Debug.Log($"Snake: Not enough steps for {(turnLeft ? "left" : "right")} loop. Has: {currentDiceSteps}, Needs: {loopCost}. Ending turn.");
                OnMovementFinished(); // Завершаем ход, если шагов не хватает
            }
        }
        else // Если это не петля, а стандартный поворот на 90 градусов
        {
            Debug.Log("Snake: Standard turn (no loop waypoints defined or chosen).");
            isMoving = true; // Устанавливаем флаг для поворота
            UpdateUIAndButton();
            float rotationYAmount = turnLeft ? -90f : 90f;
            if (primaryMoveCoroutine != null) StopCoroutine(primaryMoveCoroutine);
            primaryMoveCoroutine = StartCoroutine(RotateCoroutine(rotationYAmount, () => {
                isMoving = false; // Поворот завершен
                if (currentDiceSteps > 0)
                {
                    Debug.Log("Snake: Continuing on main path after standard turn for " + currentDiceSteps + " steps.");
                    StartMoving(currentDiceSteps); // Передаем ОСТАВШИЕСЯ шаги
                }
                else
                {
                    OnMovementFinished();
                }
            }));
        }
    }

    IEnumerator MoveAlongLoopCoroutine(Transform[] waypoints, int costOfLoop)
    {
        isMovingOnLoop = true; // Already set, but good to be explicit
        isMoving = true; // Общий флаг движения тоже активен
        UpdateUIAndButton();

        // Поворот к первой точке петли
        if (waypoints.Length > 0)
        {
            yield return StartCoroutine(RotateTowardsTargetCoroutine(waypoints[0].position));
        }

        for (int i = 0; i < waypoints.Length; i++)
        {
            Vector3 startPosition = transform.position;
            Vector3 endPosition = waypoints[i].position;
            Debug.Log($"Snake: Loop step to waypoint {i} ({endPosition}).");

            float elapsedTime = 0;
            while (elapsedTime < loopMoveDurationPerWaypoint)
            {
                transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / loopMoveDurationPerWaypoint);
                // Плавный поворот к следующей точке петли во время движения
                if (i + 1 < waypoints.Length)
                {
                    RotateTowardsTargetDuringMovement(waypoints[i + 1].position);
                }
                else
                {
                    // Если это последняя точка, поворачиваемся в ее направлении (или в направлении выхода с петли)
                    // Предполагается, что последняя точка waypoints.rotation уже настроена правильно для выхода на дорогу
                    RotateTowardsTargetDuringMovement(endPosition + waypoints[i].forward); // Поворот по направлению waypoint'а
                }
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.position = endPosition;
            // После достижения waypoint'а, можно установить его поворот, если он важен
            // transform.rotation = waypoints[i].rotation; 
        }

        // Шаги за петлю списываются единовременно
        currentDiceSteps -= costOfLoop;
        UpdateMovesValueUIText(currentDiceSteps);
        Debug.Log($"Snake: Loop finished. Cost: {costOfLoop}. Steps remaining: {currentDiceSteps}.");

        // После петли, персонаж должен быть на последней точке waypoints.
        // Убедимся, что поворот соответствует выходу с петли (если последняя точка имеет нужный rotation)
        if (waypoints.Length > 0) transform.rotation = waypoints[waypoints.Length - 1].rotation;


        isMovingOnLoop = false;
        isMoving = false; // Завершаем общее движение

        if (currentDiceSteps > 0)
        {
            Debug.Log("Snake: Continuing on main path after loop.");
            StartMoving(currentDiceSteps); // Продолжаем с оставшимися шагами
        }
        else
        {
            OnMovementFinished();
        }
    }

    // --- Вспомогательные Корутины для Движения и Поворотов ---
    IEnumerator RotateCoroutine(float angleY, System.Action onRotationComplete)
    {
        isMoving = true; // Флаг на время поворота
        UpdateUIAndButton();
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0, angleY, 0);
        float elapsedTime = 0;
        while (elapsedTime < rotateDuration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, elapsedTime / rotateDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.rotation = endRotation;
        isMoving = false; // Поворот завершен
        onRotationComplete?.Invoke();
    }

    IEnumerator RotateTowardsTargetCoroutine(Vector3 targetPosition) // Поворот к цели перед началом движения
    {
        isMoving = true;
        UpdateUIAndButton();
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float elapsedTime = 0;
            Quaternion startRotation = transform.rotation;
            while (elapsedTime < rotateDuration)
            {
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime / rotateDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.rotation = targetRotation;
        }
        isMoving = false;
    }

    void RotateTowardsTargetDuringMovement(Vector3 targetPosition) // Плавный поворот во время движения
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * loopRotateSpeed);
        }
    }

    // --- Методы Управления UI и Состоянием ---
    void UpdateUIAndButton()
    {
        UpdateMovesValueUIText(currentDiceSteps);
        UpdateButtonRollDiceVisibility();
    }

    void UpdateMovesValueUIText(int moves)
    {
        if (movesValueText != null)
        {
            movesValueText.text = moves.ToString();
        }
    }

    void UpdateButtonRollDiceVisibility()
    {
        if (buttonRollDice != null)
        {
            buttonRollDice.SetActive(!IsCurrentlyExecutingMovement() && currentDiceSteps <= 0);
        }
    }

    public bool IsCurrentlyExecutingMovement() // Один метод для проверки всех состояний движения/ожидания
    {
        return isMoving || waitingForTurnChoice || isMovingOnLoop;
    }

    // --- Обработка Триггеров ---
    void OnTriggerEnter(Collider other)
    {
        if (IsCurrentlyExecutingMovement()) // Игнорируем триггеры во время активного движения/выбора
        {
            // Debug.Log("Snake: OnTriggerEnter for " + other.name + " ignored during movement/choice.");
            return;
        }
        Debug.Log("Snake: OnTriggerEnter with " + other.name);

        if (other.CompareTag("TurnPointTrigger")) // Убедитесь, что ваши триггеры развилок имеют этот тег
        {
            Debug.Log("Snake: Hit a TurnPointTrigger directly.");
            ReachedTurnPoint();
        }
        // else if (other.TryGetComponent(out Eat eatScript)) { /* Логика еды */ }
        else if (other.TryGetComponent(out Vopros voprosScript))
        {
            Debug.Log("Snake: Entered Vopros trigger. Saving state and loading Vopros scene.");
            SavePlayerState();
            SceneManager.LoadScene("Vopros"); // Убедитесь, что сцена "Vopros" добавлена в Build Settings
        }
    }
}