using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Для стандартного UI Text
using UnityEngine.SceneManagement;

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

    [Header("Настройки Боковых Путей (Петель)")]
    public Transform[] leftLoopWaypoints;
    public int leftLoopCost = 3;
    public Transform[] rightLoopWaypoints;
    public int rightLoopCost = 3;

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

    void Awake()
    {
        InitializePassportStatus();
        LoadMoney();
    }

    void Start()
    {
        gameObject.name = "Player_Snake";
        // InitializePassportStatus() и LoadMoney() уже вызваны в Awake

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
    }

    private static void InitializePassportStatus()
    {
        if (_sessionPassportStatusInitialized && Application.isPlaying) return;

        if (PlayerPrefs.HasKey(HasPassportKey))
        {
            _sessionHasPassport = (PlayerPrefs.GetInt(HasPassportKey, 0) == 1);
            Debug.Log($"InitializePassportStatus: Статус паспорта из PlayerPrefs: {_sessionHasPassport}.");
        }
        else
        {
            _sessionHasPassport = false;
            Debug.Log($"InitializePassportStatus: Ключ паспорта не найден. Статус: false.");
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
            Debug.Log($"Деньги загружены из PlayerPrefs: {money}");
        }
        else
        {
            money = 5000;
            Debug.Log($"Ключ денег не найден. Установлено начальное значение: {money}");
        }
        _previousMoneyForUI = -1;
        UpdateMoneyUI();
    }

    void SaveMoney()
    {
        PlayerPrefs.SetInt(MoneyKey, money);
        PlayerPrefs.Save();
        Debug.Log($"Деньги СОХРАНЕНЫ в PlayerPrefs: {money}.");
    }

    void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            if (money != _previousMoneyForUI)
            {
                moneyText.text = $"Монеты: {money}";
                _previousMoneyForUI = money;
                Debug.Log($"UpdateMoneyUI: moneyText ОБНОВЛЕН (значение изменилось) на '{moneyText.text}'");
            }
        }
        else
        {
            Debug.LogWarning("UpdateMoneyUI: moneyText is NULL!");
        }
    }

    void OnApplicationQuit()
    {
        Debug.Log("OnApplicationQuit: Удаление PlayerPrefs перед выходом...");
        PlayerPrefs.DeleteKey(PosXKey);
        PlayerPrefs.DeleteKey(PosYKey);
        PlayerPrefs.DeleteKey(PosZKey);
        PlayerPrefs.DeleteKey(RotYKey);
        PlayerPrefs.DeleteKey(DiceRollKey);
        PlayerPrefs.DeleteKey(HasPassportKey);
        PlayerPrefs.DeleteKey(MoneyKey);
        PlayerPrefs.Save();
        Debug.Log("OnApplicationQuit: Все указанные PlayerPrefs удалены.");
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
        SaveMoney();
        Debug.Log("PlayerState сохранен (позиция и деньги).");
    }

    public void StartMoving(int steps)
    {
        if (isMoving || waitingForTurnChoice || isMovingOnLoop || passportCheckEventActive || passportEventCurrentlyActive)
        {
            Debug.LogWarning($"Snake: StartMoving called but character is busy. Ignored.");
            return;
        }
        Debug.Log($"Snake: StartMoving initiated with {steps} steps.");

        stepsTakenInCurrentMove = 0;
        startedMoveFromSpecialField = (passportEventCurrentlyActive || hasStoppedOnStopFieldThisMove || passportCheckEventActive);
        hasStoppedOnStopFieldThisMove = false;
        _cinemaCheckedThisTurn = false; // Сбрасываем флаг проверки кино для нового хода

        if (passportEventCurrentlyActive)
        {
            HidePassportUIPanel();
        }

        currentDiceSteps = steps;
        if (PlayerPrefs.HasKey(DiceRollKey))
        {
            PlayerPrefs.DeleteKey(DiceRollKey);
            PlayerPrefs.Save();
        }
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
            bool crossedCinemaXThisStep = false;
            float initialXForStep = startPositionOfThisStep.x;

            while (elapsedTime < moveDuration)
            {
                Vector3 posBeforeLerp = transform.position;
                transform.position = Vector3.Lerp(startPositionOfThisStep, endPositionThisStep, elapsedTime / moveDuration);
                elapsedTime += Time.deltaTime;

                if (!crossedCinemaXThisStep)
                {
                    if (transform.forward.x > 0 && posBeforeLerp.x < cinemaFieldXCoordinate && transform.position.x >= cinemaFieldXCoordinate)
                    { crossedCinemaXThisStep = true; Debug.Log($"MoveStepsCoroutine: Пересекли X={cinemaFieldXCoordinate} справа (X={transform.position.x})"); }
                    else if (transform.forward.x < 0 && posBeforeLerp.x > cinemaFieldXCoordinate && transform.position.x <= cinemaFieldXCoordinate)
                    { crossedCinemaXThisStep = true; Debug.Log($"MoveStepsCoroutine: Пересекли X={cinemaFieldXCoordinate} слева (X={transform.position.x})"); }
                }

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

            bool onCinemaTileAfterStep = false;
            float finalXForStep = transform.position.x;
            if ((initialXForStep <= cinemaFieldXCoordinate + cinemaFieldTolerance && finalXForStep >= cinemaFieldXCoordinate - cinemaFieldTolerance) ||
                (initialXForStep >= cinemaFieldXCoordinate - cinemaFieldTolerance && finalXForStep <= cinemaFieldXCoordinate + cinemaFieldTolerance))
            {
                if (Mathf.Abs(finalXForStep - cinemaFieldXCoordinate) <= cinemaFieldTolerance)
                {
                    onCinemaTileAfterStep = true;
                }
            }

            if (crossedCinemaXThisStep || onCinemaTileAfterStep)
            {
                Debug.Log($"MoveStepsCoroutine: Шаг завершен, crossedCinemaX={crossedCinemaXThisStep}, onCinemaTile={onCinemaTileAfterStep}. Проверка кино для X={transform.position.x}");
                CheckForCinemaTile();
            }

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
        Debug.Log($"OnMovementFinished CALLED. Player pos: {transform.position}. isMoving: {isMoving}, passportCheckActive: {passportCheckEventActive}, passportEventCurrentlyActive: {passportEventCurrentlyActive}");
        isMoving = false;
        isMovingOnLoop = false;

        if (passportCheckEventActive)
        {
            Debug.Log($"Snake: OnMovementFinished. Событие (Проверка паспорта на СТОП) активно. Ожидание.");
            return;
        }
        else if (passportEventCurrentlyActive)
        {
            Debug.Log($"Snake: OnMovementFinished. Событие (поле 14 лет) активно. Ожидание.");
            return;
        }

        if (CheckAndHandleStopFieldIfNeeded(transform.position, transform.position, true))
        {
            Debug.Log($"Snake: OnMovementFinished. Только что остановились на поле СТОП.");
            return;
        }
        else if (CheckAndShowPassportPanelIfNeeded(transform.position, transform.position, true))
        {
            Debug.Log($"Snake: OnMovementFinished. Только что остановились на поле 14 лет.");
            return;
        }

        if (!_cinemaCheckedThisTurn) // Проверяем кино в конце хода, только если оно не было проверено в середине
        {
            Debug.Log("OnMovementFinished: Кино не проверялось в середине хода, вызываем CheckForCinemaTile() сейчас.");
            CheckForCinemaTile();
        }
        else
        {
            Debug.Log("OnMovementFinished: Кино уже было проверено/обработано в середине этого хода.");
        }


        if (stepsTakenInCurrentMove == 2 && currentDiceSteps == 0)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.5f);
            foreach (var collider in hitColliders)
            {
                if (collider.TryGetComponent<Vopros>(out _))
                {
                    Debug.Log("Игрок остановился на поле вопроса после броска 2 - активация вопроса!");
                    SavePlayerState();
                    PlayerPrefs.SetInt(DiceRollKey, 0);
                    PlayerPrefs.Save();
                    SceneManager.LoadScene("Vopros");
                    return;
                }
            }
        }

        UpdateUIAndButton();
        SavePlayerState();
        Debug.Log($"Snake: OnMovementFinished. FINALIZED. Готов к следующему ходу.");
    }

    void CheckForCinemaTile()
    {
        if (_cinemaCheckedThisTurn && currentDiceSteps > 0)
        {
            Debug.Log("CheckForCinemaTile: Уже проверяли кино на этом ходу (и это не конец хода), пропускаем.");
            return;
        }

        Debug.Log("CheckForCinemaTile: Method ENTERED.");
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1.0f); // Радиус для обнаружения тега
        Debug.Log($"CheckForCinemaTile: OverlapSphere found {hitColliders.Length} colliders at pos {transform.position}.");

        bool cinemaFoundThisCheck = false;
        foreach (var collider in hitColliders)
        {
            Debug.Log($"CheckForCinemaTile: Checking collider '{collider.gameObject.name}' with tag '{collider.tag}'");
            if (collider.CompareTag(cinemaTileTag))
            {
                cinemaFoundThisCheck = true;
                Debug.Log($"CheckForCinemaTile: TAG MATCH! Object: {collider.gameObject.name}.");
                if (money >= cinemaVisitCost)
                {
                    money -= cinemaVisitCost;
                    Debug.Log($"CheckForCinemaTile: Visited cinema! Deducted {cinemaVisitCost}. Remaining money: {money}");
                }
                else
                {
                    Debug.Log($"CheckForCinemaTile: Not enough money for cinema! Need {cinemaVisitCost}, have {money}");
                }
                UpdateMoneyUI();
                SaveMoney();
                break;
            }
        }

        if (cinemaFoundThisCheck)
        {
            _cinemaCheckedThisTurn = true;
        }
        else
        {
            Debug.Log("CheckForCinemaTile: No cinema tile found or tag mismatch at current position.");
        }
    }

    void ForceStopMovementSequence(string reason)
    {
        Debug.Log($"Snake: Принудительная остановка движения: {reason}");
        if (primaryMoveCoroutine != null) { StopCoroutine(primaryMoveCoroutine); primaryMoveCoroutine = null; }
        if (loopMoveCoroutine != null) { StopCoroutine(loopMoveCoroutine); loopMoveCoroutine = null; }

        isMoving = false;
        isMovingOnLoop = false;
        currentDiceSteps = 0;
        startedMoveFromSpecialField = false;

        OnMovementFinished();
    }

    bool CheckAndShowPassportPanelIfNeeded(Vector3 prevPos, Vector3 currentPos, bool isFinalCheckAfterStop = false)
    {
        if (passportCheckEventActive) return false;
        if (passportEventCurrentlyActive && !isFinalCheckAfterStop) return false;

        bool passportEffectivelyObtained = PlayerEffectivelyHasPassport();

        if (passportEffectivelyObtained)
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
                Debug.Log($"Snake: Поле 14 лет обнаружено. Панель показана. isFinalCheck:{isFinalCheckAfterStop}.");
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
            Debug.Log("Панель UI паспорта (14 лет) показана.");
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
            Debug.Log("Панель UI паспорта (14 лет) скрыта.");
        }
    }

    public void OnGetPassportButtonClicked()
    {
        Debug.Log("Кнопка 'Получить паспорт' нажата!");
        PlayerPrefs.SetInt(HasPassportKey, 1);
        _sessionHasPassport = true;

        if (passportObject != null) passportObject.SetActive(true);

        HidePassportUIPanel();
        OnMovementFinished();
    }

    bool CheckAndHandleStopFieldIfNeeded(Vector3 prevPos, Vector3 currentPos, bool isFinalCheck = false)
    {
        if (passportEventCurrentlyActive && !isFinalCheck) return false;
        if (passportCheckEventActive && !isFinalCheck) return false;

        bool crossedXStop = (prevPos.x < stopFieldXCoordinate && currentPos.x >= stopFieldXCoordinate) ||
                            (prevPos.x > stopFieldXCoordinate && currentPos.x <= stopFieldXCoordinate);
        bool atXStopCoordinate = Mathf.Abs(currentPos.x - stopFieldXCoordinate) < stopFieldTolerance;

        if ((isFinalCheck && atXStopCoordinate) || (!isFinalCheck && (crossedXStop || atXStopCoordinate)))
        {
            if (hasStoppedOnStopFieldThisMove && !isFinalCheck) return true;

            Debug.Log($"Snake: Поле СТОП обнаружено. isFinal:{isFinalCheck}, atX:{atXStopCoordinate}");
            transform.position = new Vector3(stopFieldXCoordinate, currentPos.y, currentPos.z);
            hasStoppedOnStopFieldThisMove = true;

            if (!passportCheckEventActive) ShowPassportCheckPanel();
            return true;
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
            Debug.Log("Панель проверки паспорта (поле СТОП) показана.");
        }
    }

    void HidePassportCheckPanel()
    {
        if (passportCheckPanel != null && passportCheckEventActive)
        {
            passportCheckPanel.SetActive(false);
            if (presentPassportButton != null) presentPassportButton.gameObject.SetActive(false);
            passportCheckEventActive = false;
            hasStoppedOnStopFieldThisMove = false;
            UpdateButtonRollDiceVisibility();
            Debug.Log("Панель проверки паспорта (поле СТОП) скрыта.");
        }
    }

    public void OnPresentPassportButtonClicked()
    {
        Debug.Log("Кнопка 'Предъявить документ' нажата!");

        bool effectivelyHasPassport = PlayerEffectivelyHasPassport();
        if (effectivelyHasPassport)
        {
            Debug.Log("Проверка паспорта: УСПЕХ!");
            if (passportSuccessPanel != null) passportSuccessPanel.SetActive(true);
            StartCoroutine(HidePanelAfterDelay(passportSuccessPanel, 1.5f, () => {
                HidePassportCheckPanel();
                OnMovementFinished();
            }));
        }
        else
        {
            Debug.Log("Проверка паспорта: ПРОВАЛ!");
            if (passportFailPanel != null) passportFailPanel.SetActive(true);
            StartCoroutine(HidePanelAfterDelay(passportFailPanel, 2f, () => {
                StartCoroutine(MovePlayerBackThreeFieldsCoroutine(() => {
                    HidePassportCheckPanel();
                    OnMovementFinished();
                }));
            }));
        }
    }

    IEnumerator MovePlayerBackThreeFieldsCoroutine(System.Action onComplete)
    {
        Debug.Log("Перемещаем игрока на 3 поля назад...");
        isMoving = true;
        Vector3 targetPosition = transform.position - transform.forward * (stepDistance * 3);
        targetPosition.x = Mathf.Max(0, targetPosition.x);

        float elapsedTime = 0;
        Vector3 startPos = transform.position;
        float totalMoveDuration = moveDuration * 1.5f;
        bool tempStartedMoveFromSpecialField = startedMoveFromSpecialField;
        startedMoveFromSpecialField = true;

        while (elapsedTime < totalMoveDuration)
        {
            transform.position = Vector3.Lerp(startPos, targetPosition, elapsedTime / totalMoveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;

        startedMoveFromSpecialField = tempStartedMoveFromSpecialField;
        isMoving = false;
        onComplete?.Invoke();
    }

    IEnumerator HidePanelAfterDelay(GameObject panel, float delay, System.Action onComplete = null)
    {
        yield return new WaitForSeconds(delay);
        if (panel != null) panel.SetActive(false);
        onComplete?.Invoke();
    }

    public void ReachedTurnPoint()
    {
        Debug.Log($"ReachedTurnPoint ВЫЗВАН. waitChoice: {waitingForTurnChoice}, isMoving: {isMoving}, passportActive: {passportEventCurrentlyActive}, passportCheck: {passportCheckEventActive}");
        if (waitingForTurnChoice) return;
        if (passportEventCurrentlyActive || passportCheckEventActive)
        {
            Debug.Log("ReachedTurnPoint: Активна панель спец. события, выбор пути отложен.");
            return;
        }

        if (primaryMoveCoroutine != null) StopCoroutine(primaryMoveCoroutine);
        if (loopMoveCoroutine != null) StopCoroutine(loopMoveCoroutine);
        isMoving = false; isMovingOnLoop = false;

        waitingForTurnChoice = true;
        stepsRemainingAfterTurn = currentDiceSteps;
        if (turnChoiceUI != null) turnChoiceUI.SetActive(true);
        UpdateUIAndButton();
    }

    public void HandleTurnChoice(bool turnLeft)
    {
        if (!waitingForTurnChoice) return;
        if (turnChoiceUI != null) turnChoiceUI.SetActive(false);
        waitingForTurnChoice = false;
        currentDiceSteps = stepsRemainingAfterTurn;

        startedMoveFromSpecialField = false;
        HidePassportUIPanel();
        hasStoppedOnStopFieldThisMove = false;
        HidePassportCheckPanel();

        Transform[] targetLoopWaypoints = turnLeft ? leftLoopWaypoints : rightLoopWaypoints;
        int loopCost = turnLeft ? leftLoopCost : rightLoopCost;

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
        else
        {
            isMoving = true;
            float rotationYAmount = turnLeft ? -90f : 90f;
            if (primaryMoveCoroutine != null) StopCoroutine(primaryMoveCoroutine);
            primaryMoveCoroutine = StartCoroutine(RotateCoroutine(rotationYAmount, () => {
                if (currentDiceSteps > 0)
                {
                    startedMoveFromSpecialField = false;
                    if (CheckAndHandleStopFieldIfNeeded(transform.position, transform.position, true)) { OnMovementFinished(); return; }
                    if (CheckAndShowPassportPanelIfNeeded(transform.position, transform.position, true)) { OnMovementFinished(); return; }

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
        bool skipSpecialFieldChecksDuringFirstSegment = startedMoveFromSpecialField;
        bool localCinemaCheckedThisLoop = false; // Локальный флаг для петли

        for (int i = 0; i < waypoints.Length; i++)
        {
            Vector3 startPositionOfThisStep = transform.position;
            Vector3 endPositionThisStep = waypoints[i].position;
            float elapsedTime = 0;
            bool crossedCinemaXThisSegment = false;
            float initialXForSegment = startPositionOfThisStep.x;


            while (elapsedTime < loopMoveDurationPerWaypoint)
            {
                Vector3 posBeforeLerp = transform.position;
                transform.position = Vector3.Lerp(startPositionOfThisStep, endPositionThisStep, elapsedTime / loopMoveDurationPerWaypoint);
                if (i + 1 < waypoints.Length) RotateTowardsTargetDuringMovement(waypoints[i + 1].position);
                else RotateTowardsTargetDuringMovement(endPositionThisStep + waypoints[i].forward);
                elapsedTime += Time.deltaTime;

                if (!crossedCinemaXThisSegment && !_cinemaCheckedThisTurn) // Проверяем пересечение кино, если еще не было на этом ходу
                {
                    if (transform.forward.x > 0 && posBeforeLerp.x < cinemaFieldXCoordinate && transform.position.x >= cinemaFieldXCoordinate)
                    { crossedCinemaXThisSegment = true; Debug.Log($"MoveAlongLoop: Пересекли X={cinemaFieldXCoordinate} справа (X={transform.position.x})"); }
                    else if (transform.forward.x < 0 && posBeforeLerp.x > cinemaFieldXCoordinate && transform.position.x <= cinemaFieldXCoordinate)
                    { crossedCinemaXThisSegment = true; Debug.Log($"MoveAlongLoop: Пересекли X={cinemaFieldXCoordinate} слева (X={transform.position.x})"); }
                }

                if (!skipSpecialFieldChecksDuringFirstSegment)
                {
                    if (CheckAndHandleStopFieldIfNeeded(posBeforeLerp, transform.position)) { ForceStopMovementSequence("Прервано полем СТОП (середина петли)"); yield break; }
                    if (CheckAndShowPassportPanelIfNeeded(posBeforeLerp, transform.position)) { ForceStopMovementSequence("Прервано полем паспорта 14 лет (середина петли)"); yield break; }
                }
                yield return null;
            }
            transform.position = endPositionThisStep;
            skipSpecialFieldChecksDuringFirstSegment = false;
            startedMoveFromSpecialField = false;

            bool onCinemaTileAfterSegment = false;
            float finalXForSegment = transform.position.x;
            if ((initialXForSegment <= cinemaFieldXCoordinate + cinemaFieldTolerance && finalXForSegment >= cinemaFieldXCoordinate - cinemaFieldTolerance) ||
                (initialXForSegment >= cinemaFieldXCoordinate - cinemaFieldTolerance && finalXForSegment <= cinemaFieldXCoordinate + cinemaFieldTolerance))
            {
                if (Mathf.Abs(finalXForSegment - cinemaFieldXCoordinate) <= cinemaFieldTolerance)
                {
                    onCinemaTileAfterSegment = true;
                }
            }

            if ((crossedCinemaXThisSegment || onCinemaTileAfterSegment) && !_cinemaCheckedThisTurn)
            {
                Debug.Log($"MoveAlongLoop: Сегмент завершен, crossedCinemaX={crossedCinemaXThisSegment}, onCinemaTile={onCinemaTileAfterSegment}. Проверка кино для X={transform.position.x}");
                CheckForCinemaTile();
                if (_cinemaCheckedThisTurn) localCinemaCheckedThisLoop = true; // Отмечаем, если кино было найдено на этом ходу
            }


            if (CheckAndHandleStopFieldIfNeeded(startPositionOfThisStep, transform.position, true)) { ForceStopMovementSequence("Остановка на поле СТОП (конец точки пути петли)"); yield break; }
            if (CheckAndShowPassportPanelIfNeeded(startPositionOfThisStep, transform.position, true)) { ForceStopMovementSequence("Остановка на поле паспорта 14 лет (конец точки пути петли)"); yield break; }
        }

        currentDiceSteps -= costOfLoop; UpdateMovesValueUIText(currentDiceSteps);
        if (waypoints.Length > 0) transform.rotation = waypoints[waypoints.Length - 1].rotation;

        isMovingOnLoop = false; isMoving = false;
        startedMoveFromSpecialField = false;

        if (CheckAndHandleStopFieldIfNeeded(transform.position, transform.position, true)) { OnMovementFinished(); yield break; }
        if (CheckAndShowPassportPanelIfNeeded(transform.position, transform.position, true)) { OnMovementFinished(); yield break; }

        if (currentDiceSteps > 0) { StartMoving(currentDiceSteps); }
        else OnMovementFinished(); // Если это был конец хода, OnMovementFinished вызовет CheckForCinemaTile если нужно
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

    void UpdateMovesValueUIText(int moves) { if (movesValueText != null) movesValueText.text = moves.ToString(); }

    void UpdateButtonRollDiceVisibility()
    {
        if (buttonRollDice != null)
        {
            bool canRoll = !(isMoving || waitingForTurnChoice || isMovingOnLoop || passportCheckEventActive || passportEventCurrentlyActive) && currentDiceSteps <= 0;
            buttonRollDice.SetActive(canRoll);
        }
    }

    public bool IsCurrentlyExecutingMovement()
    {
        return isMoving || waitingForTurnChoice || isMovingOnLoop || passportCheckEventActive || passportEventCurrentlyActive;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("TurnPointTrigger"))
        {
            if ((isMoving || isMovingOnLoop) && !waitingForTurnChoice && !passportEventCurrentlyActive && !passportCheckEventActive)
            {
                ReachedTurnPoint();
            }
            return;
        }
        // Логика Vopros перенесена в OnMovementFinished
    }
}