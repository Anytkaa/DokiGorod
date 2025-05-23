// snake.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class snake : MonoBehaviour
{
    // --- Существующие переменные, адаптированные или оставленные ---
    public GameObject pas14; // Если все еще используется для чего-то конкретного
    public GameObject pas20; // Если все еще используется для чего-то конкретного
    public GameObject buttonRollDice; // Кнопка "Кинь кубик"
    public static int money = 2000;  // Статическое поле для денег, будет сбрасываться при перезапуске игры

    [Header("Настройки Движения")]
    public float stepDistance = 10.0f; // Расстояние одного шага
    public float moveDuration = 0.5f;  // Длительность анимации одного шага
    public float rotateDuration = 0.3f; // Длительность анимации поворота

    [Header("UI для Развилки")]
    public GameObject turnChoiceUI;    // Панель с кнопками выбора (лево/право)
    public Button turnLeftButton;
    public Button turnRightButton;

    [Header("UI Отображения")]
    public Text movesValueText;        // UI Text для отображения количества оставшихся ходов

    // --- Приватные переменные для управления состоянием ---
    private bool isMoving = false;             // Флаг: персонаж в процессе пошагового движения
    private bool waitingForTurnChoice = false; // Флаг: персонаж на развилке и ждет выбора игрока
    private int stepsRemainingAfterTurn = 0;   // Сколько шагов останется после выбора на развилке
    private Coroutine moveCoroutine;           // Ссылка на текущую корутину движения
    private int currentDiceSteps = 0;          // Общее количество шагов, полученное от текущего броска кубика

    // Ключи для PlayerPrefs (лучше вынести в статический класс или константы, если используются много где)
    private const string PosXKey = "PlayerPositionX_Snake_DokiGorod";
    private const string PosYKey = "PlayerPositionY_Snake_DokiGorod";
    private const string PosZKey = "PlayerPositionZ_Snake_DokiGorod";
    private const string RotYKey = "PlayerRotationY_Snake_DokiGorod";
    private const string DiceRollKey = "LastDiceRoll"; // Убедитесь, что этот ключ совпадает с ключом в GameController

    void Start()
    {
        gameObject.name = "Player_Snake"; // Хорошая практика для идентификации в логах/иерархии
        Debug.Log("Snake.cs Start() called on scene: " + SceneManager.GetActiveScene().name);

        LoadPlayerState(); // Загрузка сохраненной позиции/поворота при старте

        // Проверяем, есть ли результат броска кубика из другой сцены или предыдущего состояния
        if (PlayerPrefs.HasKey(DiceRollKey))
        {
            int stepsFromDice = PlayerPrefs.GetInt(DiceRollKey);
            Debug.Log("Snake: Found dice roll result in PlayerPrefs (" + DiceRollKey + "): " + stepsFromDice);
            PlayerPrefs.DeleteKey(DiceRollKey); // Удаляем ключ, чтобы не использовать его повторно
            PlayerPrefs.Save();

            if (stepsFromDice > 0)
            {
                UpdateMovesValueUIText(stepsFromDice);
                Debug.Log("Snake: Calling StartMoving with " + stepsFromDice + " steps from Start().");
                StartMoving(stepsFromDice);
            }
            else
            {
                UpdateMovesValueUIText(0); // Отобразить 0, если шагов нет
                Debug.Log("Snake: Steps from dice is 0 or less, not moving from initial dice roll.");
                UpdateButtonRollDiceVisibility(); // Показать кнопку броска, если не двигаемся
            }
        }
        else
        {
            Debug.Log("Snake: No dice roll result found in PlayerPrefs with key: " + DiceRollKey);
            UpdateMovesValueUIText(0); // Если нет данных о броске, ходов 0
            UpdateButtonRollDiceVisibility(); // Показать кнопку броска
        }

        if (turnChoiceUI != null) turnChoiceUI.SetActive(false); // Скрываем UI выбора при старте

        // Настройка слушателей кнопок выбора на развилке
        if (turnLeftButton != null)
        {
            turnLeftButton.onClick.RemoveAllListeners();
            turnLeftButton.onClick.AddListener(() => HandleTurnChoice(true)); // true для поворота налево
        }
        if (turnRightButton != null)
        {
            turnRightButton.onClick.RemoveAllListeners();
            turnRightButton.onClick.AddListener(() => HandleTurnChoice(false)); // false для поворота направо
        }

        // Пример логики для pas14/pas20, если она нужна (убедитесь, что классы polpas14/polpas20 существуют)
        // if (polpas14.pas14 == true && pas14 != null) pas14.SetActive(true);
        // if (polpas20.pas20 == true && pas20 != null) pas20.SetActive(true);
    }

    void LoadPlayerState()
    {
        if (PlayerPrefs.HasKey(PosXKey)) // Проверяем по одному ключу, предполагая, что если есть один, есть все
        {
            float x = PlayerPrefs.GetFloat(PosXKey);
            float y = PlayerPrefs.GetFloat(PosYKey);
            float z = PlayerPrefs.GetFloat(PosZKey);
            float savedRotationY = PlayerPrefs.GetFloat(RotYKey, transform.rotation.eulerAngles.y); // Используем текущий как дефолт

            transform.position = new Vector3(x, y, z);
            transform.rotation = Quaternion.Euler(0, savedRotationY, 0);
            Debug.Log("Snake: Player state loaded. Position: " + transform.position + ", RotationY: " + savedRotationY);
        }
        else
        {
            Debug.Log("Snake: No saved player state found. Starting at initial editor/scene position.");
            // Персонаж начнет с позиции, установленной в редакторе для этого объекта
        }
    }

    public void SavePlayerState()
    {
        PlayerPrefs.SetFloat(PosXKey, transform.position.x);
        PlayerPrefs.SetFloat(PosYKey, transform.position.y);
        PlayerPrefs.SetFloat(PosZKey, transform.position.z);
        PlayerPrefs.SetFloat(RotYKey, transform.rotation.eulerAngles.y);
        PlayerPrefs.Save(); // Важно сохранить изменения
        Debug.Log("Snake: Player state saved. Position: " + transform.position + ", RotationY: " + transform.rotation.eulerAngles.y);
    }

    void OnApplicationQuit()
    {
        Debug.Log("Snake: Application quitting. Clearing saved player state for next session.");
        PlayerPrefs.DeleteKey(PosXKey);
        PlayerPrefs.DeleteKey(PosYKey);
        PlayerPrefs.DeleteKey(PosZKey);
        PlayerPrefs.DeleteKey(RotYKey);
        // Если есть другие PlayerPrefs, которые нужно сбрасывать при выходе (например, очки, специфичные для сессии предметы),
        // удалите их здесь.
        // PlayerPrefs.DeleteKey("PlayerScore_DokiGorod");
        PlayerPrefs.Save(); // Сохраняем удаления
    }

    public void StartMoving(int steps)
    {
        if (isMoving || waitingForTurnChoice)
        {
            Debug.LogWarning("Snake: Already moving or waiting for turn choice. New movement for " + steps + " steps ignored.");
            return;
        }
        currentDiceSteps = steps;
        UpdateMovesValueUIText(currentDiceSteps); // Обновляем UI с общим количеством шагов
        Debug.Log("Snake: StartMoving called for " + currentDiceSteps + " steps.");
        UpdateButtonRollDiceVisibility(); // Скрыть кнопку броска кубика

        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveStepsCoroutine(currentDiceSteps));
    }

    IEnumerator MoveStepsCoroutine(int stepsToMoveInitially) // stepsToMoveInitially - это то, что выпало на кубике
    {
        isMoving = true;
        Debug.Log("Snake: MoveStepsCoroutine started. Initial steps for this sequence: " + stepsToMoveInitially + ". Current total dice steps remaining: " + currentDiceSteps);

        // Мы будем уменьшать currentDiceSteps на каждом шаге. Цикл продолжается, пока currentDiceSteps > 0
        // и мы не ждем выбора на развилке.
        while (currentDiceSteps > 0 && !waitingForTurnChoice)
        {
            Vector3 startPosition = transform.position;
            Vector3 endPosition = startPosition + transform.forward * stepDistance;
            float elapsedTime = 0;

            Debug.Log("Snake: Moving one step from " + startPosition + " to " + endPosition + ". Dice steps remaining: " + (currentDiceSteps - 1));

            while (elapsedTime < moveDuration)
            {
                transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / moveDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.position = endPosition; // Гарантируем точное конечное положение

            currentDiceSteps--; // Уменьшаем общее количество оставшихся шагов от кубика
            UpdateMovesValueUIText(currentDiceSteps); // Обновляем UI

            // Debug.Log("Snake: Step completed. Dice steps remaining: " + currentDiceSteps);

            // Здесь можно добавить проверку на триггеры событий на клетке ПОСЛЕ шага,
            // если это предпочтительнее OnTriggerEnter (например, для точного определения клетки).
            // CheckForCellEvent();

            // Небольшая пауза между шагами, если нужна для визуального восприятия
            // yield return new WaitForSeconds(0.1f); 
        }

        isMoving = false; // Завершили пошаговое движение (либо все шаги, либо развилка)
        Debug.Log("Snake: MoveStepsCoroutine finished. isMoving: " + isMoving + ", waitingForTurnChoice: " + waitingForTurnChoice + ", currentDiceSteps: " + currentDiceSteps);

        if (!waitingForTurnChoice) // Если НЕ на развилке, значит, все шаги этой последовательности сделаны
        {
            OnMovementFinished();
        }
        // Если waitingForTurnChoice is true, то ReachedTurnPoint уже должен был взять управление
        // и после выбора пользователя будет вызван HandleTurnChoice, который возобновит движение, если есть шаги.
    }

    void OnMovementFinished()
    {
        Debug.Log("Snake: All movement from current dice roll sequence completed (or no steps left after turn).");
        // currentDiceSteps должен быть 0, если не было развилок или если шаги закончились после развилки.
        if (currentDiceSteps < 0) currentDiceSteps = 0; // На всякий случай
        UpdateMovesValueUIText(currentDiceSteps); // Убедимся, что UI показывает 0
        UpdateButtonRollDiceVisibility(); // Показать кнопку броска кубика
        SavePlayerState(); // Сохраняем позицию игрока после завершения всех ходов
    }

    public void ReachedTurnPoint() // Вызывается внешним триггером на клетке-развилке
    {
        if (waitingForTurnChoice)
        {
            Debug.Log("Snake: ReachedTurnPoint called, but already waiting for turn choice. Ignored.");
            return;
        }

        // Показываем UI выбора, только если персонаж был в движении ИЛИ у него есть шаги
        if (isMoving || (currentDiceSteps > 0 && !waitingForTurnChoice))
        {
            Debug.Log("Snake: Reached Turn Point. CurrentDiceSteps: " + currentDiceSteps);
            waitingForTurnChoice = true;
            isMoving = false; // Важно остановить флаг активного пошагового движения

            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null; // Обнуляем ссылку на корутину
                Debug.Log("Snake: MoveCoroutine stopped by ReachedTurnPoint.");
            }

            stepsRemainingAfterTurn = currentDiceSteps; // Сохраняем ОСТАВШИЕСЯ шаги (которые включают текущий шаг, если он не был "сделан" до развилки)

            if (turnChoiceUI != null)
            {
                turnChoiceUI.SetActive(true);
                Debug.Log("Snake: TurnChoiceUI activated.");
            }
            UpdateButtonRollDiceVisibility(); // Кнопка "Кинь кубик" должна быть неактивна
        }
        else
        {
            Debug.LogWarning("Snake: Reached Turn Point but not in a state to show turn UI (isMoving=" + isMoving + ", waitingForTurnChoice=" + waitingForTurnChoice + ", currentDiceSteps=" + currentDiceSteps + ").");
        }
    }

    public void HandleTurnChoice(bool turnLeft) // true - налево, false - направо
    {
        if (!waitingForTurnChoice)
        {
            Debug.LogWarning("Snake: HandleTurnChoice called, but not waiting for a choice.");
            return;
        }
        if (turnChoiceUI != null) turnChoiceUI.SetActive(false); // Скрываем UI выбора

        Debug.Log("Snake: HandleTurnChoice. Turn Left: " + turnLeft + ". Steps to continue with: " + stepsRemainingAfterTurn);

        float rotationYAmount = turnLeft ? -90f : 90f;
        StartCoroutine(RotateCoroutine(rotationYAmount, () => {
            waitingForTurnChoice = false; // Сбрасываем флаг ожидания ПОСЛЕ поворота

            if (stepsRemainingAfterTurn > 0)
            {
                // Важно: currentDiceSteps уже был уменьшен на 1 в MoveStepsCoroutine, если развилка была "на" клетке
                // или остался неизменным, если развилка была "перед" клеткой.
                // Мы используем stepsRemainingAfterTurn как источник шагов для продолжения.
                currentDiceSteps = stepsRemainingAfterTurn;
                UpdateMovesValueUIText(currentDiceSteps);
                Debug.Log("Snake: Continuing movement for " + currentDiceSteps + " steps after turn.");
                StartMoving(currentDiceSteps); // Перезапускаем движение с оставшимися шагами
            }
            else
            {
                Debug.Log("Snake: No steps remaining after turn choice.");
                OnMovementFinished(); // Если шагов не осталось (например, 0), завершаем ход
            }
            stepsRemainingAfterTurn = 0; // Сбрасываем после использования
        }));
    }

    IEnumerator RotateCoroutine(float angleY, System.Action onRotationComplete)
    {
        isMoving = true; // Блокируем другие действия на время поворота
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0, angleY, 0);
        float elapsedTime = 0;

        while (elapsedTime < rotateDuration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, elapsedTime / rotateDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.rotation = endRotation; // Гарантируем точный конечный поворот
        isMoving = false; // Разблокируем после поворота (но StartMoving снова поставит isMoving=true если есть шаги)
        onRotationComplete?.Invoke(); // Вызываем коллбэк после завершения поворота
    }

    void UpdateButtonRollDiceVisibility()
    {
        if (buttonRollDice != null)
        {
            // Кнопка активна, если мы не двигаемся, не ждем выбора И все шаги от кубика сделаны
            bool canRoll = !isMoving && !waitingForTurnChoice && currentDiceSteps <= 0;
            buttonRollDice.SetActive(canRoll);
            // Debug.Log("Snake: ButtonRollDice visibility updated to: " + canRoll + " (isMoving: " + isMoving + ", waitingForTurnChoice: " + waitingForTurnChoice + ", currentDiceSteps: " + currentDiceSteps + ")");
        }
    }

    public bool IsMoving() // Для GameController, чтобы знать, можно ли бросать кубик
    {
        return isMoving || waitingForTurnChoice;
    }

    void UpdateMovesValueUIText(int moves)
    {
        if (movesValueText != null)
        {
            movesValueText.text = moves.ToString();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Важно: Логика OnTriggerEnter может быть сложной в пошаговой игре,
        // так как триггер может сработать между шагами или при неточном позиционировании.
        // Рассмотрите возможность проверки событий на клетке в конце каждого шага в MoveStepsCoroutine.

        // Предотвращаем срабатывание триггеров, если мы уже обрабатываем движение или выбор
        if (isMoving || waitingForTurnChoice)
        {
            // Можно залогировать, если нужно отследить, какие триггеры игнорируются
            // Debug.Log("Snake: OnTriggerEnter for " + other.name + " ignored while moving or waiting for turn choice.");
            return;
        }

        Debug.Log("Snake: OnTriggerEnter with " + other.name);

        if (other.CompareTag("TurnPointTrigger")) // Пример тега для триггера развилки
        {
            // Если триггер развилки не имеет своего скрипта, а просто запускает логику здесь
            Debug.Log("Snake: Hit a TurnPointTrigger directly.");
            ReachedTurnPoint();
        }
        // Если у вас есть скрипт на самом триггере развилки, который вызывает ReachedTurnPoint(),
        // то этот блок CompareTag может быть не нужен.

        else if (other.TryGetComponent(out Eat eatScript)) // Пример для клетки "Еда"
        {
            Debug.Log("Snake: Entered Eat trigger.");
            // Логика для Eat
            // eatScript.Consume();
            // Destroy(other.gameObject);
        }
        else if (other.TryGetComponent(out Vopros voprosScript)) // Пример для клетки "Вопрос"
        {
            Debug.Log("Snake: Entered Vopros trigger. Saving state and loading Vopros scene.");
            SavePlayerState(); // Сохраняем состояние перед сменой сцены
            SceneManager.LoadScene("Vopros"); // Убедитесь, что сцена "Vopros" добавлена в Build Settings
        }
        // Добавьте другие else if для других типов триггеров
    }
}