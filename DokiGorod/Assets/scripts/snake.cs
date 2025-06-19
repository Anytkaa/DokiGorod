using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[System.Serializable]
public class TurnPointInfo
{
    public string description = "Описание для удобства";
    public RotateCheck triggerObject;
    public Transform[] leftWaypoints;
    public int leftPathCost = 1;
    public Transform[] rightWaypoints;
    public int rightPathCost = 1;
    [TextArea] public string turnMessage;
    public string leftButtonText = "Налево";
    public string rightButtonText = "Направо";
}

public class snake : MonoBehaviour
{
    [Header("Объекты и UI")]
    public GameObject buttonRollDice;
    public Text movesValueText;
    public Text moneyText;

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
    public GameObject turnMessagePanel;
    public TMPro.TextMeshProUGUI turnMessageText;

    [Header("Настройки Развилок")]
    public List<TurnPointInfo> turnPoints = new List<TurnPointInfo>();
    private RotateCheck currentTurnTrigger;

    // Переменные для сохранения состояния развилки между ходами
    private Transform[] currentForkWaypoints;
    private int currentForkCost;
    private Quaternion currentForkInitialRotation;
    private int currentForkIndex;
    private bool isForkActive;
    private bool shouldReturnToInitialRotation;
    private bool needsTurnToLastWaypoint;

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

    [Header("Настройки событий 17 и 18 лет")]
    public float graduationFieldXCoordinate = 210.0f;
    public float graduationFieldTolerance = 0.5f;
    public GameObject graduationUIPanel;
    public GameObject getGraduationButton;

    public float drivingLicenseFieldXCoordinate = 220.0f;
    public float drivingLicenseFieldTolerance = 0.5f;
    public GameObject drivingLicensePanel;
    public GameObject getDrivingLicenseButton;

    private bool hasGraduation = false;
    private bool hasDrivingLicense = false;
    private bool wentRightAtSecondFork = false;

    [Header("Настройки Бонусной Клетки")]
    public float bonusFieldXCoordinate = 60.0f;
    public float bonusFieldTolerance = 1.5f;
    public GameObject bonusUIPanel;
    public TMPro.TextMeshProUGUI bonusMessageText;
    public int bonusAmount = 50;
    private bool _bonusCheckedThisTurn = false;

    [Header("Настройки Кинотеатра")]
    public string cinemaTileTag = "CinemaTile";
    public int cinemaVisitCost = 50;
    public float cinemaFieldXCoordinate = 43.21f;
    public float cinemaFieldTolerance = 1.5f;

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
    private bool _bonusAppliedThisTurn = false;

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

    [SerializeField] private GameObject hasPassportPanel;
    [SerializeField] private GameObject graduationCertificateObject;

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

        shouldReturnToInitialRotation = false;
        needsTurnToLastWaypoint = false;

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

    public void StartMoving(int steps, bool isContinuationOfEvent = false)
    {
        // Проверяем, нужно ли продолжить движение по активной развилке
        if (isForkActive && currentForkIndex < currentForkWaypoints.Length)
        {
            _bonusAppliedThisTurn = false;
            if (isMoving || waitingForTurnChoice || isMovingOnLoop)
            {
                Debug.Log("StartMoving блокирован активным процессом.");
                return;
            }

            // Подготовка переменных для внутреннего блока
            bool isStartingFromStopFieldInner = Mathf.Abs(transform.position.x - stopFieldXCoordinate) < stopFieldTolerance;
            bool isStartingFromPassportFieldInner = Mathf.Abs(transform.position.x - passportFieldXCoordinate) < passportFieldTolerance;

            hasStoppedOnStopFieldThisMove = false;
            hasAlreadyPresentedPassportOnThisStopField = false;
            _cinemaCheckedThisTurn = false;

            if (passportEventCurrentlyActive) HidePassportUIPanel();
            if (passportCheckEventActive)
            {
                HidePassportCheckPanel();
                passportCheckEventActive = false;
            }

            startedMoveFromSpecialField = isStartingFromStopFieldInner || isStartingFromPassportFieldInner || isContinuationOfEvent;

            currentDiceSteps = steps;
            if (PlayerPrefs.HasKey(DiceRollKey)) PlayerPrefs.DeleteKey(DiceRollKey);

            // Продолжаем движение по развилке
            isMovingOnLoop = true;
            UpdateUIAndButton();
            if (loopMoveCoroutine != null) StopCoroutine(loopMoveCoroutine);
            loopMoveCoroutine = StartCoroutine(ContinueForkMovement());
            return;
        }

        // Оригинальный код StartMoving
        _bonusAppliedThisTurn = false;
        if (isMoving || waitingForTurnChoice || isMovingOnLoop)
        {
            Debug.Log("StartMoving блокирован активным процессом.");
            return;
        }

        bool isStartingFromStopField = Mathf.Abs(transform.position.x - stopFieldXCoordinate) < stopFieldTolerance;
        bool isStartingFromPassportField = Mathf.Abs(transform.position.x - passportFieldXCoordinate) < passportFieldTolerance;

        hasStoppedOnStopFieldThisMove = false;
        hasAlreadyPresentedPassportOnThisStopField = false;
        _cinemaCheckedThisTurn = false;

        if (passportEventCurrentlyActive) HidePassportUIPanel();
        if (passportCheckEventActive)
        {
            HidePassportCheckPanel();
            passportCheckEventActive = false;
        }

        startedMoveFromSpecialField = isStartingFromStopField || isStartingFromPassportField || isContinuationOfEvent;

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
                    if (CheckAndHandleBonusFieldIfNeeded(startPositionOfThisStep, transform.position, true)) { }
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
            if (CheckAndHandleBonusFieldIfNeeded(startPositionOfThisStep, transform.position, true)) { }
        }
        ForceStopMovementSequence("Движение завершено нормально или по решению.");
    }

    void OnMovementFinished()
    {
        isMoving = false;
        isMovingOnLoop = false;
        SavePlayerState();

        if (hasStoppedOnStopFieldThisMove && !passportCheckEventActive)
        {
            hasStoppedOnStopFieldThisMove = false;
        }

        if (!_bonusCheckedThisTurn && currentDiceSteps == 0 &&
            Mathf.Abs(transform.position.x - bonusFieldXCoordinate) < bonusFieldTolerance)
        {
            GiveBonus();
            _bonusCheckedThisTurn = true;
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
        if (primaryMoveCoroutine != null)
        {
            StopCoroutine(primaryMoveCoroutine);
            primaryMoveCoroutine = null;
        }
        if (loopMoveCoroutine != null)
        {
            StopCoroutine(loopMoveCoroutine);
            loopMoveCoroutine = null;
        }

        // Сбрасываем состояние развилки только если остановка не вызвана нехваткой шагов на развилке
        if (!(isForkActive && currentDiceSteps <= 0))
        {
            ResetForkState();
        }

        isMoving = false;
        isMovingOnLoop = false;

        if (!hasStoppedOnStopFieldThisMove)
        {
            currentDiceSteps = 0;
        }

        startedMoveFromSpecialField = false;

        OnMovementFinished();
    }

    bool CheckAndHandleBonusFieldIfNeeded(Vector3 prevPos, Vector3 currentPos, bool isFinalCheck = false)
    {
        if (_bonusAppliedThisTurn) return false;

        bool crossedXBonus = (prevPos.x < bonusFieldXCoordinate && currentPos.x >= bonusFieldXCoordinate) ||
                            (prevPos.x > bonusFieldXCoordinate && currentPos.x <= bonusFieldXCoordinate);
        bool atXBonusCoordinate = Mathf.Abs(currentPos.x - bonusFieldXCoordinate) < bonusFieldTolerance;

        Debug.Log($"Bonus check: isFinalCheck={isFinalCheck}, atXBonus={atXBonusCoordinate}, stepsLeft={currentDiceSteps}");

        if (isFinalCheck && atXBonusCoordinate && currentDiceSteps == 0)
        {
            Debug.Log("Условия для бонуса выполнены!");
            GiveBonus();
            _bonusAppliedThisTurn = true;
            return true;
        }
        return false;
    }

    bool CheckBonusField(Vector3 position)
    {
        return Mathf.Abs(position.x - bonusFieldXCoordinate) < bonusFieldTolerance;
    }

    void GiveBonus()
    {
        money += bonusAmount;
        UpdateMoneyUI();

        if (bonusUIPanel != null)
        {
            bonusUIPanel.SetActive(true);
            if (bonusMessageText != null)
            {
                bonusMessageText.text = $"Вы получили {bonusAmount} доков!";
            }

            StartCoroutine(HideBonusPanelAfterDelay(2f));
        }
    }

    IEnumerator HideBonusPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bonusUIPanel != null)
        {
            bonusUIPanel.SetActive(false);
        }
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
        PlayerPrefs.SetInt(HasPassportKey, 1);
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
        if (PlayerEffectivelyHasPassport())
        {
            HideAllPassportCheckUI();

            if (passportSuccessPanel != null)
            {
                passportSuccessPanel.SetActive(true);
            }

            StartCoroutine(HidePanelAfterDelay(passportSuccessPanel, 1.5f, () =>
            {
                int remainingSteps = currentDiceSteps;
                passportCheckEventActive = false;
                hasAlreadyPresentedPassportOnThisStopField = true;
                UpdateUIAndButton();

                if (remainingSteps > 0)
                {
                    StartMoving(remainingSteps, true);
                }
            }));
        }
        else
        {
            if (passportFailPanel != null)
                passportFailPanel.SetActive(true);

            StartCoroutine(HidePanelAfterDelay(passportFailPanel, 2f, () => {
                StartCoroutine(MovePlayerBackThreeFieldsCoroutine(() => {
                    HidePassportCheckPanel();
                    passportCheckEventActive = false;
                    ShowPassportUIPanel();
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

        UpdateUIAndButton();

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
        currentTurnTrigger = trigger;
        stepsRemainingAfterTurn = currentDiceSteps;

        TurnPointInfo currentPoint = turnPoints.Find(p => p.triggerObject == currentTurnTrigger);

        if (currentPoint != null)
        {
            if (turnLeftButton != null && turnLeftButton.GetComponentInChildren<TMPro.TextMeshProUGUI>() != null)
            {
                turnLeftButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = currentPoint.leftButtonText;
            }
            if (turnRightButton != null && turnRightButton.GetComponentInChildren<TMPro.TextMeshProUGUI>() != null)
            {
                turnRightButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = currentPoint.rightButtonText;
            }
        }

        if (turnMessagePanel != null)
        {
            turnMessagePanel.SetActive(true);

            if (turnMessageText != null)
            {
                if (currentPoint != null && !string.IsNullOrEmpty(currentPoint.turnMessage))
                {
                    turnMessageText.text = currentPoint.turnMessage;
                }
                else
                {
                    turnMessageText.text = "Выберите направление:";
                }
            }

            StartCoroutine(ShowTurnButtonsAfterDelay(2f));
        }
        else
        {
            if (turnChoiceUI != null) turnChoiceUI.SetActive(true);
        }

        UpdateUIAndButton();
    }

    IEnumerator ShowTurnButtonsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (turnMessagePanel != null)
        {
            turnMessagePanel.SetActive(false);
        }

        if (turnChoiceUI != null)
        {
            turnChoiceUI.SetActive(true);
        }
    }

    public void HandleTurnChoice(bool turnLeft)
    {
        if (!waitingForTurnChoice || currentTurnTrigger == null) return;

        if (turnMessagePanel != null)
        {
            turnMessagePanel.SetActive(false);
        }

        TurnPointInfo currentPoint = turnPoints.Find(p => p.triggerObject == currentTurnTrigger);

        if (currentPoint == null)
        {
            Debug.LogError("Не удалось найти настройки для текущей развилки!");
            waitingForTurnChoice = false;
            if (turnChoiceUI != null) turnChoiceUI.SetActive(false);
            OnMovementFinished();
            return;
        }

        if (turnChoiceUI != null) turnChoiceUI.SetActive(false);
        waitingForTurnChoice = false;

        Transform[] targetLoopWaypoints = turnLeft ? currentPoint.leftWaypoints : currentPoint.rightWaypoints;
        int loopCost = turnLeft ? currentPoint.leftPathCost : currentPoint.rightPathCost;

        if (targetLoopWaypoints != null && targetLoopWaypoints.Length > 0)
        {
            // Сохраняем начальное вращение перед входом на развилку
            Quaternion initialRotation = transform.rotation;

            isMovingOnLoop = true;
            isForkActive = true;
            if (loopMoveCoroutine != null) StopCoroutine(loopMoveCoroutine);
            loopMoveCoroutine = StartCoroutine(MoveAlongLoopCoroutine(targetLoopWaypoints, loopCost, initialRotation));
        }
        else
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

    IEnumerator MoveAlongLoopCoroutine(Transform[] waypoints, int costOfLoop, Quaternion initialRotation)
    {
        isMovingOnLoop = true;
        isMoving = true;
        UpdateUIAndButton();

        // Сохраняем состояние развилки
        currentForkWaypoints = waypoints;
        currentForkCost = costOfLoop;
        currentForkInitialRotation = initialRotation;
        currentForkIndex = 0;
        isForkActive = true;
        shouldReturnToInitialRotation = true;
        needsTurnToLastWaypoint = false;

        // Обрабатываем все waypoints
        for (int i = 0; i < waypoints.Length; i++)
        {
            // Если ходов не осталось - сохраняем состояние и прерываемся
            if (currentDiceSteps <= 0)
            {
                // Запоминаем, что нужно сделать поворот к последнему waypoint
                if (i == waypoints.Length - 2)
                {
                    needsTurnToLastWaypoint = true;
                }

                SavePlayerState();
                isMovingOnLoop = false;
                isMoving = false;
                UpdateButtonRollDiceVisibility();
                yield break;
            }

            // Вычитаем шаг
            currentDiceSteps--;
            UpdateMovesValueUIText(currentDiceSteps);

            Transform targetWaypoint = waypoints[i];
            Vector3 startPosition = transform.position;
            Vector3 targetPosition = targetWaypoint.position;

            // Для первого waypoint: поворачиваем к нему
            if (i == 0)
            {
                yield return StartCoroutine(RotateTowardsTargetCoroutine(targetPosition));
            }

            // Двигаемся к waypoint
            float elapsedTime = 0;
            while (elapsedTime < loopMoveDurationPerWaypoint)
            {
                transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / loopMoveDurationPerWaypoint);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.position = targetPosition;

            // После достижения waypoint:
            if (i == 0 && shouldReturnToInitialRotation)
            {
                // После первого waypoint возвращаем начальное вращение
                transform.rotation = initialRotation;
            }
            else if (i == waypoints.Length - 1)
            {
                // После последнего waypoint возвращаем начальное вращение
                transform.rotation = initialRotation;
            }

            // Если это предпоследний waypoint, поворачиваем к последнему
            if (i == waypoints.Length - 2)
            {
                yield return StartCoroutine(RotateTowardsTargetCoroutine(waypoints[i + 1].position));
            }

            CheckForCinemaTile();
            if (CheckAndHandleStopFieldIfNeeded(transform.position, transform.position, true))
            {
                ForceStopMovementSequence("Остановка на поле СТОП");
                yield break;
            }
            if (CheckAndShowPassportPanelIfNeeded(transform.position, transform.position, true))
            {
                ForceStopMovementSequence("Остановка на поле паспорта");
                yield break;
            }

            currentForkIndex = i + 1;
        }

        // После прохождения всех waypoints
        currentDiceSteps -= costOfLoop;
        if (currentDiceSteps < 0) currentDiceSteps = 0;
        UpdateMovesValueUIText(currentDiceSteps);

        // Сбрасываем состояние развилки
        ResetForkState();

        isMovingOnLoop = false;
        isMoving = false;

        if (currentDiceSteps > 0)
        {
            StartMoving(currentDiceSteps);
        }
        else
        {
            OnMovementFinished();
        }
    }

    IEnumerator ContinueForkMovement()
    {
        isMovingOnLoop = true;
        isMoving = true;
        UpdateUIAndButton();

        // Если нужно повернуть к последнему waypoint
        if (needsTurnToLastWaypoint)
        {
            // Поворачиваем к последнему waypoint
            if (currentForkIndex < currentForkWaypoints.Length)
            {
                yield return StartCoroutine(RotateTowardsTargetCoroutine(
                    currentForkWaypoints[currentForkIndex].position));
            }
            needsTurnToLastWaypoint = false;
        }

        // Продолжаем с сохраненной позиции
        for (int i = currentForkIndex; i < currentForkWaypoints.Length; i++)
        {
            // Если ходов не осталось - прерываемся
            if (currentDiceSteps <= 0)
            {
                // Если мы на предпоследнем waypoint, запоминаем что нужно повернуть к последнему
                if (i == currentForkWaypoints.Length - 2)
                {
                    needsTurnToLastWaypoint = true;
                }

                SavePlayerState();
                isMovingOnLoop = false;
                isMoving = false;
                UpdateButtonRollDiceVisibility();
                yield break;
            }

            // Вычитаем шаг
            currentDiceSteps--;
            UpdateMovesValueUIText(currentDiceSteps);

            Transform targetWaypoint = currentForkWaypoints[i];
            Vector3 startPosition = transform.position;
            Vector3 targetPosition = targetWaypoint.position;

            // Если это предпоследний waypoint, поворачиваем к последнему
            if (i == currentForkWaypoints.Length - 2)
            {
                yield return StartCoroutine(RotateTowardsTargetCoroutine(
                    currentForkWaypoints[i + 1].position));
            }

            // Двигаемся к waypoint
            float elapsedTime = 0;
            while (elapsedTime < loopMoveDurationPerWaypoint)
            {
                transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / loopMoveDurationPerWaypoint);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.position = targetPosition;

            // После достижения последнего waypoint возвращаем начальное вращение
            if (i == currentForkWaypoints.Length - 1)
            {
                transform.rotation = currentForkInitialRotation;
            }

            CheckForCinemaTile();
            if (CheckAndHandleStopFieldIfNeeded(transform.position, transform.position, true))
            {
                ForceStopMovementSequence("Остановка на поле СТОП");
                yield break;
            }
            if (CheckAndShowPassportPanelIfNeeded(transform.position, transform.position, true))
            {
                ForceStopMovementSequence("Остановка на поле паспорта");
                yield break;
            }

            currentForkIndex = i + 1;
        }

        // После прохождения всех waypoints
        currentDiceSteps -= currentForkCost;
        if (currentDiceSteps < 0) currentDiceSteps = 0;
        UpdateMovesValueUIText(currentDiceSteps);

        // Сбрасываем состояние развилки
        ResetForkState();

        isMovingOnLoop = false;
        isMoving = false;

        if (currentDiceSteps > 0)
        {
            StartMoving(currentDiceSteps);
        }
        else
        {
            OnMovementFinished();
        }
    }

    private void ResetForkState()
    {
        currentForkWaypoints = null;
        currentForkCost = 0;
        currentForkInitialRotation = Quaternion.identity;
        currentForkIndex = 0;
        isForkActive = false;
        shouldReturnToInitialRotation = false;
        needsTurnToLastWaypoint = false;
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