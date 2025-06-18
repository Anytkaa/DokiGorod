using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Для стандартного UI Text
using UnityEngine.SceneManagement;

[System.Serializable] // Это нужно, чтобы видеть его в Инспекторе
public class TurnPointInfo
{
    public string description = "Описание для удобства"; // Просто чтобы не запутаться в инспекторе
    public RotateCheck triggerObject; // Ссылка на сам объект-триггер развилки
    public Transform[] leftWaypoints;
    public int leftPathCost = 1;
    public Transform[] rightWaypoints;
    public int rightPathCost = 1;
}

public class snake : MonoBehaviour
{
    [Header("Объекты и UI")]
    public GameObject buttonRollDice;
    public Text movesValueText;
    public Text moneyText; // или public TextMeshProUGUI moneyText;

    [Header("Настройки Движения")]
    public float stepDistance = 10.0f;
    public float moveDuration = 0.5f;
    public float rotateDuration = 0.3f;
    public float loopMoveDurationPerWaypoint = 0.9f;
    public float loopRotateSpeed = 5f;

    [Header("UI для Развилки")]
    public GameObject turnChoiceUI;
    public Button turnLeftButton;
    public Button turnRightButton;
    public GameObject turnMessagePanel; // Новая панель с сообщением
    public Text turnMessageText; // Текст сообщения

    [Header("Настройки Развилок")]
    public List<TurnPointInfo> turnPoints = new List<TurnPointInfo>();

    // Добавим переменную для хранения текущего триггера
    private RotateCheck currentTurnTrigger;

    [Header("Специальные Поля и UI Событий")]
    public float passportFieldXCoordinate = 80.0f;
    public float passportFieldTolerance = 0.5f;
    public GameObject passportUIPanel;
    public GameObject getPassportButtonObject;

    public float stopFieldXCoordinate = 140.0f;
    public float stopFieldTolerance = 0.5f;

    [Header("UI Проверки Паспорта на Стоп-Поле")]
    public GameObject passportCheckPanel;
    public Button presentPassportButton;
    public GameObject passportSuccessPanel;
    public GameObject passportFailPanel;
    public GameObject passportObject;
    public int passportFineAmount = 300;

    [Header("Настройки Кинотеатра")]
    public string cinemaTileTag = "CinemaTile"; // Тег для клетки "Кино"
    public int cinemaVisitCost = 50;      // Стоимость посещения кино
    public float cinemaFieldXCoordinate = 43.21f; // X координата центра клетки кино
    public float cinemaFieldTolerance = 1.5f;  // Допуск (половина ширины клетки + немного)

    public static int money = 5000;
    private int _previousMoneyForUI = -1;
    private bool _cinemaCheckedThisTurn = false;

    private bool isMoving = false;
    private bool waitingForTurnChoice = false;
    private bool isMovingOnLoop = false;
    private int stepsRemainingAfterTurn = 0;
    private Coroutine primaryMoveCoroutine;
    private Coroutine loopMoveCoroutine;
    private int currentDiceSteps = 0;

    private bool passportEventCurrentlyActive = false;
    private bool passportCheckEventActive = false;
    private bool hasStoppedOnStopFieldThisMove = false;
    private bool startedMoveFromSpecialField = false;
    private bool hasAlreadyPresentedPassportOnThisStopField = false;


    private const string PosXKey = "PlayerPositionX_Snake_DokiGorod";
    private const string PosYKey = "PlayerPositionY_Snake_DokiGorod";
    private const string PosZKey = "PlayerPositionZ_Snake_DokiGorod";
    private const string RotYKey = "PlayerRotationY_Snake_DokiGorod";
    private const string DiceRollKey = "LastDiceRoll";
    private const string HasPassportKey = "PlayerHasPassport_DokiGorod";
    private const string MoneyKey = "PlayerMoney_DokiGorod";

    private int stepsTakenInCurrentMove = 0;
    private static bool _sessionHasPassport = false;
    private static bool _sessionPassportStatusInitialized = false;

    private static bool _isFirstLaunch = true;

    [SerializeField] private GameObject hasPassportPanel; // 🟢 ДОБАВЬ ЭТУ СТРОКУ

    void Awake()
    {
        if (_isFirstLaunch)
        {
            ClearAllSavedData();
            _isFirstLaunch = false;
        }

        InitializePassportStatus();
        LoadMoney();
    }

    void Start()
    {
        gameObject.name = "Player_Snake";
        LoadPlayerState();

        if (PlayerPrefs.HasKey(DiceRollKey))
        {
            int stepsFromDice = PlayerPrefs.GetInt(DiceRollKey);
            if (stepsFromDice > 0) StartMoving(stepsFromDice);
            else { UpdateMovesValueUIText(0); UpdateButtonRollDiceVisibility(); }
        }
        else { UpdateMovesValueUIText(0); UpdateButtonRollDiceVisibility(); }

        if (passportUIPanel != null) passportUIPanel.SetActive(false);
        if (getPassportButtonObject != null)
        {
            getPassportButtonObject.SetActive(false);
            Button btn = getPassportButtonObject.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnGetPassportButtonClicked);
            }
        }
        if (turnChoiceUI != null) turnChoiceUI.SetActive(false);

        if (turnLeftButton != null) { turnLeftButton.onClick.RemoveAllListeners(); turnLeftButton.onClick.AddListener(() => HandleTurnChoice(true)); }
        if (turnRightButton != null) { turnRightButton.onClick.RemoveAllListeners(); turnRightButton.onClick.AddListener(() => HandleTurnChoice(false)); }

        if (passportCheckPanel != null) passportCheckPanel.SetActive(false);
        if (passportSuccessPanel != null) passportSuccessPanel.SetActive(false);
        if (passportFailPanel != null) passportFailPanel.SetActive(false);
        if (presentPassportButton != null)
        {
            presentPassportButton.onClick.RemoveAllListeners();
            presentPassportButton.onClick.AddListener(OnPresentPassportButtonClicked);
        }

        if (hasPassportPanel != null) hasPassportPanel.SetActive(false);
    }

    void OnApplicationQuit()
    {
        _isFirstLaunch = true;
    }

    private void ClearAllSavedData()
    {
        PlayerPrefs.DeleteKey(PosXKey);
        PlayerPrefs.DeleteKey(PosYKey);
        PlayerPrefs.DeleteKey(PosZKey);
        PlayerPrefs.DeleteKey(RotYKey);
        PlayerPrefs.DeleteKey(DiceRollKey);
        PlayerPrefs.DeleteKey(HasPassportKey);
        PlayerPrefs.DeleteKey(MoneyKey);
        PlayerPrefs.Save();
        Debug.Log("НОВЫЙ ЗАПУСК: Все сохраненные данные очищены.");
    }

    private static void InitializePassportStatus()
    {
        if (_sessionPassportStatusInitialized && Application.isPlaying) return;

        if (PlayerPrefs.HasKey(HasPassportKey))
        {
            _sessionHasPassport = (PlayerPrefs.GetInt(HasPassportKey, 0) == 1);
        }
        else
        {
            _sessionHasPassport = false;
        }
        _sessionPassportStatusInitialized = true;
    }

    private bool PlayerEffectivelyHasPassport()
    {
        return _sessionHasPassport;
    }

    void LoadMoney()
    {
        if (PlayerPrefs.HasKey(MoneyKey))
        {
            money = PlayerPrefs.GetInt(MoneyKey);
        }
        else
        {
            money = 5000;
        }
        _previousMoneyForUI = -1;
        UpdateMoneyUI();
    }

    void SaveMoney()
    {
        PlayerPrefs.SetInt(MoneyKey, money);
    }

    void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            if (money != _previousMoneyForUI)
            {
                moneyText.text = $"Доков: {money}";
                _previousMoneyForUI = money;
            }
        }
    }

    void LoadPlayerState()
    {
        if (PlayerPrefs.HasKey(PosXKey))
        {
            transform.position = new Vector3(PlayerPrefs.GetFloat(PosXKey), PlayerPrefs.GetFloat(PosYKey), PlayerPrefs.GetFloat(PosZKey));
            transform.rotation = Quaternion.Euler(0, PlayerPrefs.GetFloat(RotYKey, transform.rotation.eulerAngles.y), 0);
        }
        bool stopFieldActive = CheckAndHandleStopFieldIfNeeded(transform.position, transform.position, true);
        if (!stopFieldActive)
        {
            CheckAndShowPassportPanelIfNeeded(transform.position, transform.position, true);
        }
        UpdateButtonRollDiceVisibility();
    }

    public void SavePlayerState()
    {
        PlayerPrefs.SetFloat(PosXKey, transform.position.x);
        PlayerPrefs.SetFloat(PosYKey, transform.position.y);
        PlayerPrefs.SetFloat(PosZKey, transform.position.z);
        PlayerPrefs.SetFloat(RotYKey, transform.rotation.eulerAngles.y);
        PlayerPrefs.SetInt(HasPassportKey, _sessionHasPassport ? 1 : 0);
        SaveMoney();
        PlayerPrefs.Save();
        Debug.Log($"СОСТОЯНИЕ СОХРАНЕНО (ВНУТРИ СЕССИИ). Позиция: {transform.position}");
    }

    // ПОЛНОСТЬЮ ЗАМЕНИТЕ ВАШУ ФУНКЦИЮ STARTMOVING
    public void StartMoving(int steps, bool isContinuationOfEvent = false)
    {
        // --- ПРЕДВАРИТЕЛЬНАЯ ПРОВЕРКА И ОЧИСТКА СОСТОЯНИЯ ---

        if (isMoving || waitingForTurnChoice || isMovingOnLoop)
        {
            Debug.Log("StartMoving блокирован активным процессом.");
            return;
        }

        // Определяем, начинаем ли мы ход, стоя на ОДНОМ ИЗ специальных полей
        bool isStartingFromStopField = Mathf.Abs(transform.position.x - stopFieldXCoordinate) < stopFieldTolerance;
        // ↓↓↓ ВОТ КЛЮЧЕВОЕ ДОБАВЛЕНИЕ ↓↓↓
        bool isStartingFromPassportField = Mathf.Abs(transform.position.x - passportFieldXCoordinate) < passportFieldTolerance;

        // Сбрасываем все флаги состояния для НОВОГО хода.
        hasStoppedOnStopFieldThisMove = false;
        hasAlreadyPresentedPassportOnThisStopField = false;
        _cinemaCheckedThisTurn = false;

        if (passportEventCurrentlyActive)
        {
            HidePassportUIPanel();
        }

        if (passportCheckEventActive)
        {
            HidePassportCheckPanel();
            passportCheckEventActive = false;
        }

        // Устанавливаем флаг, который позволит "уехать" с поля без повторного срабатывания
        startedMoveFromSpecialField = isStartingFromStopField || isStartingFromPassportField || isContinuationOfEvent;

        // --- НАЧАЛО ДВИЖЕНИЯ ---

        currentDiceSteps = steps;
        if (PlayerPrefs.HasKey(DiceRollKey)) PlayerPrefs.DeleteKey(DiceRollKey);

        UpdateUIAndButton();
        if (primaryMoveCoroutine != null) StopCoroutine(primaryMoveCoroutine);
        primaryMoveCoroutine = StartCoroutine(MoveStepsCoroutine());
    }

    IEnumerator MoveStepsCoroutine()
    {
        if (isMovingOnLoop) { yield break; }
        isMoving = true;
        UpdateUIAndButton();
        bool skipSpecialFieldChecksDuringFirstSegment = startedMoveFromSpecialField;

        while (currentDiceSteps > 0 && !waitingForTurnChoice && !isMovingOnLoop && !passportCheckEventActive && !passportEventCurrentlyActive)
        {
            Vector3 startPositionOfThisStep = transform.position;
            Vector3 endPositionThisStep = startPositionOfThisStep + transform.forward * stepDistance;
            float elapsedTime = 0;

            while (elapsedTime < moveDuration)
            {
                Vector3 posBeforeLerp = transform.position;
                transform.position = Vector3.Lerp(startPositionOfThisStep, endPositionThisStep, elapsedTime / moveDuration);
                elapsedTime += Time.deltaTime;

                if (!skipSpecialFieldChecksDuringFirstSegment)
                {
                    if (CheckAndHandleStopFieldIfNeeded(posBeforeLerp, transform.position)) { ForceStopMovementSequence("Прервано полем Стоп (в середине хода)"); yield break; }
                    if (CheckAndShowPassportPanelIfNeeded(posBeforeLerp, transform.position)) { ForceStopMovementSequence("Прервано полем 14 лет (в середине хода)"); yield break; }
                }
                yield return null;
            }
            transform.position = endPositionThisStep;
            skipSpecialFieldChecksDuringFirstSegment = false;
            startedMoveFromSpecialField = false;

            CheckForCinemaTile();

            currentDiceSteps--;
            stepsTakenInCurrentMove++;
            UpdateMovesValueUIText(currentDiceSteps);

            if (CheckAndHandleStopFieldIfNeeded(startPositionOfThisStep, transform.position, true)) { ForceStopMovementSequence("Остановка на поле Стоп (в конце шага)"); yield break; }
            if (CheckAndShowPassportPanelIfNeeded(startPositionOfThisStep, transform.position, true)) { ForceStopMovementSequence("Остановка на поле 14 лет (в конце шага)"); yield break; }
        }
        ForceStopMovementSequence("Движение завершено нормально или по решению.");
    }

    void OnMovementFinished()
    {
        isMoving = false;
        isMovingOnLoop = false;
        SavePlayerState();

        // Если было стоп-поле, но паспорт проверен - сбрасываем флаг
        if (hasStoppedOnStopFieldThisMove && !passportCheckEventActive)
        {
            hasStoppedOnStopFieldThisMove = false;
        }

        if (passportCheckEventActive || passportEventCurrentlyActive)
        {
            UpdateUIAndButton();
            return;
        }

        if (CheckAndHandleStopFieldIfNeeded(transform.position, transform.position, true) ||
            CheckAndShowPassportPanelIfNeeded(transform.position, transform.position, true))
        {
            UpdateUIAndButton();
            return;
        }

        if (!_cinemaCheckedThisTurn)
        {
            CheckForCinemaTile();
        }

        if (stepsTakenInCurrentMove == 2 && currentDiceSteps == 0)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.5f);
            foreach (var collider in hitColliders)
            {
                if (collider.TryGetComponent<Vopros>(out _))
                {
                    PlayerPrefs.SetInt(DiceRollKey, 0);
                    SceneManager.LoadScene("Vopros");
                    return;
                }
            }
        }

        UpdateUIAndButton();
    }

    void CheckForCinemaTile()
    {
        if (_cinemaCheckedThisTurn)
        {
            return;
        }

        if (Mathf.Abs(transform.position.x - cinemaFieldXCoordinate) <= cinemaFieldTolerance)
        {
            Debug.Log($"Обнаружена клетка 'Кино' на координате X={transform.position.x}. Списываем деньги.");
            if (money >= cinemaVisitCost)
            {
                money -= cinemaVisitCost;
            }

            _cinemaCheckedThisTurn = true;
            UpdateMoneyUI();
        }
    }

    void ForceStopMovementSequence(string reason)
    {
        if (primaryMoveCoroutine != null) { StopCoroutine(primaryMoveCoroutine); primaryMoveCoroutine = null; }
        if (loopMoveCoroutine != null) { StopCoroutine(loopMoveCoroutine); loopMoveCoroutine = null; }

        isMoving = false;
        isMovingOnLoop = false;

        if (!hasStoppedOnStopFieldThisMove)
        {
            currentDiceSteps = 0;
        }

        startedMoveFromSpecialField = false;

        OnMovementFinished();
    }

    bool CheckAndShowPassportPanelIfNeeded(Vector3 prevPos, Vector3 currentPos, bool isFinalCheckAfterStop = false)
    {
        if (passportCheckEventActive) return false;
        if (passportEventCurrentlyActive && !isFinalCheckAfterStop) return false;

        if (PlayerEffectivelyHasPassport())
        {
            if (passportEventCurrentlyActive) HidePassportUIPanel();
            return false;
        }

        if (passportUIPanel == null) return false;

        bool crossedXPassport = (prevPos.x < passportFieldXCoordinate && currentPos.x >= passportFieldXCoordinate) ||
                                (prevPos.x > passportFieldXCoordinate && currentPos.x <= passportFieldXCoordinate);
        bool atXPassportCoordinate = Mathf.Abs(currentPos.x - passportFieldXCoordinate) < passportFieldTolerance;

        if ((isFinalCheckAfterStop && atXPassportCoordinate) || (!isFinalCheckAfterStop && (crossedXPassport || atXPassportCoordinate)))
        {
            if (!passportEventCurrentlyActive)
            {
                ShowPassportUIPanel();
            }
            return true;
        }
        return false;
    }

    void ShowPassportUIPanel()
    {
        if (passportUIPanel != null)
        {
            passportUIPanel.SetActive(true);
            if (getPassportButtonObject != null) getPassportButtonObject.SetActive(true);
            passportEventCurrentlyActive = true;
            UpdateButtonRollDiceVisibility();
        }
    }

    void HidePassportUIPanel()
    {
        if (passportUIPanel != null && passportEventCurrentlyActive)
        {
            passportUIPanel.SetActive(false);
            if (getPassportButtonObject != null) getPassportButtonObject.SetActive(false);
            passportEventCurrentlyActive = false;
            UpdateButtonRollDiceVisibility();
        }
    }

    public void OnGetPassportButtonClicked()
    {
        _sessionHasPassport = true;
         PlayerPrefs.SetInt(HasPassportKey, 1); // Сохраняем в PlayerPrefs
        PlayerPrefs.Save();
        if (passportObject != null) passportObject.SetActive(true);
        HidePassportUIPanel();
        OnMovementFinished();
    }

    bool CheckAndHandleStopFieldIfNeeded(Vector3 prevPos, Vector3 currentPos, bool isFinalCheck = false)
    {
        if (passportEventCurrentlyActive && !isFinalCheck)
            return false;
        if (passportCheckEventActive && !isFinalCheck)
            return false;

        // ✅ НЕ проверять, если уже предъявили паспорт ранее
        if (hasAlreadyPresentedPassportOnThisStopField)
            return false;

        bool crossedXStop = (prevPos.x < stopFieldXCoordinate && currentPos.x >= stopFieldXCoordinate) ||
                            (prevPos.x > stopFieldXCoordinate && currentPos.x <= stopFieldXCoordinate);
        bool atXStopCoordinate = Mathf.Abs(currentPos.x - stopFieldXCoordinate) < stopFieldTolerance;

        if ((isFinalCheck && atXStopCoordinate) || (!isFinalCheck && (crossedXStop || atXStopCoordinate)))
        {
            if (!hasStoppedOnStopFieldThisMove)
            {
                transform.position = new Vector3(stopFieldXCoordinate, currentPos.y, currentPos.z);
                hasStoppedOnStopFieldThisMove = true;
                if (!passportCheckEventActive) ShowPassportCheckPanel();
                return true;
            }
            
        }
        return false;
    }

    void ShowPassportCheckPanel()
    {
        if (passportCheckPanel != null)
        {
            passportCheckPanel.SetActive(true);
            if (presentPassportButton != null) presentPassportButton.gameObject.SetActive(true);
            passportCheckEventActive = true;
            UpdateButtonRollDiceVisibility();
        }
    }

    void HidePassportCheckPanel()
    {
        if (passportCheckPanel != null)
        {
            passportCheckPanel.SetActive(false);

            if (presentPassportButton != null)
                presentPassportButton.gameObject.SetActive(false);
        }
    }

    private void HideAllPassportCheckUI()
    {
        // Эта функция скрывает ВСЁ, что относится к проверке паспорта.
        if (passportCheckPanel != null)
        {
            passportCheckPanel.SetActive(false);
        }
        if (presentPassportButton != null)
        {
            presentPassportButton.gameObject.SetActive(false);
        }
    }

    public void OnPresentPassportButtonClicked()
    {
        // --- ВЕТКА 1: У ИГРОКА ЕСТЬ ПАСПОРТ (УСПЕХ) ---
        if (PlayerEffectivelyHasPassport())
        {
            // === НЕМЕДЛЕННЫЕ ДЕЙСТВИЯ ===

            // 1. НЕМЕДЛЕННО вызываем нашего "чистильщика". Он скроет и панель, и кнопку.
            HideAllPassportCheckUI();

            // 2. Показываем сообщение об успехе.
            if (passportSuccessPanel != null)
            {
                passportSuccessPanel.SetActive(true);
            }

            // === ОТЛОЖЕННЫЕ ДЕЙСТВИЯ ===
            StartCoroutine(HidePanelAfterDelay(passportSuccessPanel, 1.5f, () =>
            {
                int remainingSteps = currentDiceSteps;

                // Сбрасываем флаг события, чтобы игра знала, что мы больше не в нем.
                passportCheckEventActive = false;

                // Устанавливаем флаг "памяти", чтобы не остановиться на этом поле снова в рамках этого же хода.
                hasAlreadyPresentedPassportOnThisStopField = true;

                // Обновляем UI на случай, если ходов не осталось и нужно показать кнопку "Кинь кубик".
                UpdateUIAndButton();

                // Если были шаги, продолжаем движение с флагом "продолжение".
                if (remainingSteps > 0)
                {
                    StartMoving(remainingSteps, true);
                }
            }));
        }
        else // Ветка, если паспорта НЕТ (ИСПРАВЛЕННАЯ ВЕРСИЯ)
        {
            if (passportFailPanel != null)
                passportFailPanel.SetActive(true);

            StartCoroutine(HidePanelAfterDelay(passportFailPanel, 2f, () => {
                StartCoroutine(MovePlayerBackThreeFieldsCoroutine(() => {

                    // --- ПРАВИЛЬНЫЙ ПОРЯДОК СБРОСА И АКТИВАЦИИ ---

                    // 1. Сначала скрываем UI стоп-поля.
                    HidePassportCheckPanel();

                    // 2. ЯВНО и ПРИНУДИТЕЛЬНО сбрасываем флаг состояния "Проверка паспорта".
                    // Это самая важная строка, которой не хватало.
                    passportCheckEventActive = false;

                    // 3. Теперь, когда система в чистом состоянии, активируем логику получения паспорта.
                    ShowPassportUIPanel();

                    // 4. Обновляем UI, чтобы кнопка "Кинь кубик" исчезла,
                    // так как теперь активно событие получения паспорта.
                    UpdateButtonRollDiceVisibility();

                }));
            }));
        }
    }

    IEnumerator MovePlayerBackThreeFieldsCoroutine(System.Action onComplete)
    {
        isMoving = true;
        Vector3 targetPosition = transform.position - transform.forward * (stepDistance * 3);
        targetPosition.x = Mathf.Max(0, targetPosition.x);
        float elapsedTime = 0;
        Vector3 startPos = transform.position;
        float totalMoveDuration = moveDuration * 1.5f;
        while (elapsedTime < totalMoveDuration)
        {
            transform.position = Vector3.Lerp(startPos, targetPosition, elapsedTime / totalMoveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;
        isMoving = false;

        Debug.Log($"Штраф! Списано {passportFineAmount} доков за несвоевременное получение паспорта.");
        money -= passportFineAmount;
        UpdateMoneyUI();

        onComplete?.Invoke();
    }

    IEnumerator HidePanelAfterDelay(GameObject panel, float delay, System.Action onComplete = null)
    {
        yield return new WaitForSeconds(delay);
        if (panel != null) panel.SetActive(false);

        UpdateUIAndButton(); // <== Добавьте эту строку

        onComplete?.Invoke();
    }

    public void ReachedTurnPoint(RotateCheck trigger)
    {
        if (waitingForTurnChoice) return;
        if (passportEventCurrentlyActive || passportCheckEventActive) return;

        if (primaryMoveCoroutine != null) StopCoroutine(primaryMoveCoroutine);
        if (loopMoveCoroutine != null) StopCoroutine(loopMoveCoroutine);
        isMoving = false; isMovingOnLoop = false;

        waitingForTurnChoice = true;
        currentTurnTrigger = trigger; // <-- ЗАПОМИНАЕМ, КАКАЯ РАЗВИЛКА АКТИВНА
        stepsRemainingAfterTurn = currentDiceSteps;


        // Показываем сообщение перед развилкой --> тут добавила 
        if (turnMessagePanel != null)
        {
            turnMessagePanel.SetActive(true);
            
            // Установите текст сообщения (можно настроить в инспекторе или динамически)
            if (turnMessageText != null)
            {
                turnMessageText.text = "Выберите направление:";
            }
            
            // Запускаем корутину, которая через пару секунд покажет кнопки
            StartCoroutine(ShowTurnButtonsAfterDelay(2f));
        }
        else
        {
            // Если панели сообщения нет, сразу показываем кнопки
            if (turnChoiceUI != null) turnChoiceUI.SetActive(true);
        }


        UpdateUIAndButton();
    }
    
    IEnumerator ShowTurnButtonsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Скрываем панель сообщения
        if (turnMessagePanel != null)
        {
            turnMessagePanel.SetActive(false);
        }
        
        // Показываем кнопки выбора направления
        if (turnChoiceUI != null)
        {
            turnChoiceUI.SetActive(true);
        }
    }

    public void HandleTurnChoice(bool turnLeft)
    {
        if (!waitingForTurnChoice || currentTurnTrigger == null) return;

        // Скрываем панель сообщения (если вдруг еще видна) --тут тоже добавила 
        if (turnMessagePanel != null)
        {
            turnMessagePanel.SetActive(false);
        }

        // Ищем в нашем списке нужную информацию по активному триггеру
        TurnPointInfo currentPoint = turnPoints.Find(p => p.triggerObject == currentTurnTrigger);

        if (currentPoint == null)
        {
            Debug.LogError("Не удалось найти настройки для текущей развилки! Проверьте инспектор.");
            // Отменяем ожидание, чтобы не застрять
            waitingForTurnChoice = false;
            if (turnChoiceUI != null) turnChoiceUI.SetActive(false);
            OnMovementFinished();
            return;
        }

        if (turnChoiceUI != null) turnChoiceUI.SetActive(false);
        waitingForTurnChoice = false;
        currentDiceSteps = stepsRemainingAfterTurn;

        // Используем данные из найденной развилки
        Transform[] targetLoopWaypoints = turnLeft ? currentPoint.leftWaypoints : currentPoint.rightWaypoints;
        int loopCost = turnLeft ? currentPoint.leftPathCost : currentPoint.rightPathCost;

        if (targetLoopWaypoints != null && targetLoopWaypoints.Length > 0)
        {
            if (currentDiceSteps >= loopCost)
            {
                isMovingOnLoop = true;
                if (loopMoveCoroutine != null) StopCoroutine(loopMoveCoroutine);
                loopMoveCoroutine = StartCoroutine(MoveAlongLoopCoroutine(targetLoopWaypoints, loopCost));
            }
            else
            {
                currentDiceSteps = 0;
                OnMovementFinished();
            }
        }
        else // Если для этой развилки пути не заданы, делаем простой поворот
        {
            isMoving = true;
            float rotationYAmount = turnLeft ? -90f : 90f;
            if (primaryMoveCoroutine != null) StopCoroutine(primaryMoveCoroutine);
            primaryMoveCoroutine = StartCoroutine(RotateCoroutine(rotationYAmount, () =>
            {
                if (currentDiceSteps > 0)
                {
                    StartMoving(currentDiceSteps);
                }
                else
                {
                    OnMovementFinished();
                }
            }));
        }
        UpdateUIAndButton();
    }

    IEnumerator MoveAlongLoopCoroutine(Transform[] waypoints, int costOfLoop)
    {
        isMovingOnLoop = true; isMoving = true; UpdateUIAndButton();
        if (waypoints.Length > 0) yield return StartCoroutine(RotateTowardsTargetCoroutine(waypoints[0].position));

        for (int i = 0; i < waypoints.Length; i++)
        {
            Vector3 startPositionOfThisStep = transform.position;
            Vector3 endPositionThisStep = waypoints[i].position;
            float elapsedTime = 0;

            while (elapsedTime < loopMoveDurationPerWaypoint)
            {
                Vector3 posBeforeLerp = transform.position;
                transform.position = Vector3.Lerp(startPositionOfThisStep, endPositionThisStep, elapsedTime / loopMoveDurationPerWaypoint);
                if (i + 1 < waypoints.Length) RotateTowardsTargetDuringMovement(waypoints[i + 1].position);
                else RotateTowardsTargetDuringMovement(endPositionThisStep + waypoints[i].forward);
                elapsedTime += Time.deltaTime;

                if (CheckAndHandleStopFieldIfNeeded(posBeforeLerp, transform.position)) { ForceStopMovementSequence("Прервано полем СТОП (середина петли)"); yield break; }
                if (CheckAndShowPassportPanelIfNeeded(posBeforeLerp, transform.position)) { ForceStopMovementSequence("Прервано полем паспорта 14 лет (середина петли)"); yield break; }

                yield return null;
            }
            transform.position = endPositionThisStep;

            CheckForCinemaTile();
            if (CheckAndHandleStopFieldIfNeeded(transform.position, transform.position, true)) { ForceStopMovementSequence("Остановка на поле СТОП (конец точки пути петли)"); yield break; }
            if (CheckAndShowPassportPanelIfNeeded(transform.position, transform.position, true)) { ForceStopMovementSequence("Остановка на поле паспорта 14 лет (конец точки пути петли)"); yield break; }
        }

        currentDiceSteps -= costOfLoop; UpdateMovesValueUIText(currentDiceSteps);
        if (waypoints.Length > 0) transform.rotation = waypoints[waypoints.Length - 1].rotation;

        isMovingOnLoop = false; isMoving = false;

        if (currentDiceSteps > 0) { StartMoving(currentDiceSteps); }
        else OnMovementFinished();
    }

    IEnumerator RotateCoroutine(float angleY, System.Action onRotationComplete)
    {
        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0, angleY, 0);
        float elapsedTime = 0;
        while (elapsedTime < rotateDuration) { transform.rotation = Quaternion.Slerp(startRot, endRot, elapsedTime / rotateDuration); elapsedTime += Time.deltaTime; yield return null; }
        transform.rotation = endRot;
        onRotationComplete?.Invoke();
    }

    IEnumerator RotateTowardsTargetCoroutine(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction == Vector3.zero) { yield break; }
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float elapsedTime = 0;
        Quaternion startRotation = transform.rotation;
        while (elapsedTime < rotateDuration) { transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime / rotateDuration); elapsedTime += Time.deltaTime; yield return null; }
        transform.rotation = targetRotation;
    }

    void RotateTowardsTargetDuringMovement(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * loopRotateSpeed);
    }

    void UpdateUIAndButton()
    {
        UpdateMovesValueUIText(currentDiceSteps);
        UpdateButtonRollDiceVisibility();
        UpdateMoneyUI();
    }

    void UpdateMovesValueUIText(int moves)
    {
        if (movesValueText != null) movesValueText.text = moves.ToString();
    }

    void UpdateButtonRollDiceVisibility()
    {
        if (buttonRollDice != null)
        {

            bool canRoll = !(isMoving || waitingForTurnChoice || isMovingOnLoop || passportCheckEventActive) && currentDiceSteps <= 0;
            buttonRollDice.SetActive(canRoll);
        }
    }

    public bool IsCurrentlyExecutingMovement()
    {
        return isMoving || waitingForTurnChoice || isMovingOnLoop || passportCheckEventActive || passportEventCurrentlyActive;
    }



    
}