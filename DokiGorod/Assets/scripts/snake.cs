using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class snake : MonoBehaviour
{
    [Header("Объекты и UI")]
    public GameObject buttonRollDice;
    public Text movesValueText;

    [Header("Настройки Движения")]
    public float stepDistance = 10.0f;
    public float moveDuration = 0.5f;
    public float rotateDuration = 0.3f;
    public float loopMoveDurationPerWaypoint = 0.9f; // Убедитесь, что это значение не слишком маленькое
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
    public float passportFieldXCoordinate = 80.0f; // Координата X для поля "14 лет"
    public float passportFieldTolerance = 0.5f; // Допуск для определения поля
    public GameObject passportUIPanel; // Панель "Получите паспорт"
    public GameObject getPassportButtonObject; // Кнопка "Получить паспорт" на поле 14 лет

    public float stopFieldXCoordinate = 140.0f; // Координата X для поля "Стоп"
    public float stopFieldTolerance = 0.5f; // Допуск для определения поля

    [Header("UI Проверки Паспорта на Стоп-Поле")]
    public GameObject passportCheckPanel; // Панель с сообщением "Проверка паспорта"
    public Button presentPassportButton; // Кнопка "Предъявить документ"
    public GameObject passportSuccessPanel; // Панель "Молодец"
    public GameObject passportFailPanel; // Панель "Не молодец"

    public static int money = 5000;

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
    private const string HasPassportKey = "PlayerHasPassport";

    private int stepsTakenInCurrentMove = 0;

    // --- НОВАЯ СТАТИЧЕСКАЯ ПЕРЕМЕННАЯ ДЛЯ ОТСЛЕЖИВАНИЯ ПАСПОРТА В ТЕКУЩЕЙ СЕССИИ ---
    private static bool _sessionObtainedPassport = false;
    // --- КОНЕЦ НОВОЙ ПЕРЕМЕННОЙ ---

    void Start()
    {
        gameObject.name = "Player_Snake";

        // --- ДЛЯ ТЕСТИРОВАНИЯ: СБРОС СОСТОЯНИЯ ПАСПОРТА ПРИ КАЖДОМ ЗАПУСКЕ ---
        // PlayerPrefs.DeleteKey(HasPassportKey); // Оставьте закомментированным для сохранения паспорта
        // PlayerPrefs.Save();
        // --- КОНЕЦ ТЕСТИРОВАНИЯ ---

        // Если игра только что запущена (не перезагрузка скрипта в редакторе),
        // и _sessionObtainedPassport еще false, проверяем PlayerPrefs.
        // Это нужно, чтобы подхватить паспорт из предыдущей сессии, если он был сохранен
        // и если строки удаления выше закомментированы.
        if (!_sessionObtainedPassport && PlayerPrefs.GetInt(HasPassportKey, 0) == 1)
        {
            _sessionObtainedPassport = true;
            Debug.Log("Start(): Passport loaded from PlayerPrefs into session flag.");
        }


        LoadPlayerState();

        if (PlayerPrefs.HasKey(DiceRollKey))
        {
            int stepsFromDice = PlayerPrefs.GetInt(DiceRollKey);
            PlayerPrefs.DeleteKey(DiceRollKey); PlayerPrefs.Save();
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

    void OnApplicationQuit()
    {
        PlayerPrefs.DeleteKey(PosXKey); PlayerPrefs.DeleteKey(PosYKey); PlayerPrefs.DeleteKey(PosZKey); PlayerPrefs.DeleteKey(RotYKey);
        // PlayerPrefs.DeleteKey(HasPassportKey); 
        PlayerPrefs.Save();
    }

    void LoadPlayerState()
    {
        if (PlayerPrefs.HasKey(PosXKey))
        {
            transform.position = new Vector3(PlayerPrefs.GetFloat(PosXKey), PlayerPrefs.GetFloat(PosYKey), PlayerPrefs.GetFloat(PosZKey));
            transform.rotation = Quaternion.Euler(0, PlayerPrefs.GetFloat(RotYKey, transform.rotation.eulerAngles.y), 0);
        }
        if (!CheckAndHandleStopFieldIfNeeded(transform.position, transform.position, true))
        {
            CheckAndShowPassportPanelIfNeeded(transform.position, transform.position, true);
        }
        UpdateButtonRollDiceVisibility();
    }

    public void SavePlayerState()
    {
        PlayerPrefs.SetFloat(PosXKey, transform.position.x); PlayerPrefs.SetFloat(PosYKey, transform.position.y);
        PlayerPrefs.SetFloat(PosZKey, transform.position.z); PlayerPrefs.SetFloat(RotYKey, transform.rotation.eulerAngles.y);
        PlayerPrefs.Save();
    }

    public void StartMoving(int steps)
    {
        if (isMoving || waitingForTurnChoice || isMovingOnLoop || passportCheckEventActive)
        {
            Debug.LogWarning($"Snake: StartMoving called but character is busy or passport check active. Ignored.");
            return;
        }
        Debug.Log($"Snake: StartMoving initiated with {steps} steps.");

        stepsTakenInCurrentMove = 0;
        startedMoveFromSpecialField = (passportEventCurrentlyActive || hasStoppedOnStopFieldThisMove);
        hasStoppedOnStopFieldThisMove = false;

        currentDiceSteps = steps;
        UpdateUIAndButton();
        if (primaryMoveCoroutine != null) StopCoroutine(primaryMoveCoroutine);
        primaryMoveCoroutine = StartCoroutine(MoveStepsCoroutine());
    }

    IEnumerator MoveStepsCoroutine()
    {
        if (isMovingOnLoop) { Debug.LogWarning("Snake: MoveStepsCoroutine called while isMovingOnLoop. Aborting."); yield break; }
        isMoving = true;
        UpdateUIAndButton();
        bool skipSpecialFieldChecksDuringFirstSegment = startedMoveFromSpecialField;

        while (currentDiceSteps > 0 && !waitingForTurnChoice && !isMovingOnLoop && !passportCheckEventActive)
        {
            Vector3 startPositionOfThisStep = transform.position;
            Vector3 endPositionThisStep = startPositionOfThisStep + transform.forward * stepDistance;
            float elapsedTime = 0;
            Debug.Log($"Snake: MoveStepsCoroutine - Начинаем шаг {stepsTakenInCurrentMove + 1}. Пропускать проверки на первом сегменте: {skipSpecialFieldChecksDuringFirstSegment}. Текущая позиция: {transform.position.x}");
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
            currentDiceSteps--;
            stepsTakenInCurrentMove++;
            UpdateMovesValueUIText(currentDiceSteps);
            Debug.Log($"Snake: MoveStepsCoroutine - Шаг {stepsTakenInCurrentMove} завершен. Осталось шагов: {currentDiceSteps}. Текущая позиция: {transform.position.x}");
            if (CheckAndHandleStopFieldIfNeeded(startPositionOfThisStep, transform.position, true)) { ForceStopMovementSequence("Остановка на поле Стоп (в конце шага)"); yield break; }
            if (CheckAndShowPassportPanelIfNeeded(startPositionOfThisStep, transform.position, true)) { ForceStopMovementSequence("Остановка на поле 14 лет (в конце шага)"); yield break; }
        }
        ForceStopMovementSequence("Движение завершено нормально или по решению.");
    }

    void OnMovementFinished()
    {
        isMoving = false;
        isMovingOnLoop = false;
        waitingForTurnChoice = false;
        startedMoveFromSpecialField = false;

        if (passportCheckEventActive)
        {
            UpdateButtonRollDiceVisibility();
            SavePlayerState();
            Debug.Log($"Snake: OnMovementFinished. Специальное событие (Проверка паспорта на поле СТОП) активно. Ожидание действия игрока.");
            return;
        }
        else if (passportEventCurrentlyActive)
        {
            ShowPassportUIPanel();
            UpdateButtonRollDiceVisibility();
            SavePlayerState();
            Debug.Log($"Snake: OnMovementFinished. Специальное событие (поле 14 лет) активно. Ожидание действия игрока (получить паспорт или бросить кубик).");
            return;
        }

        if (CheckAndHandleStopFieldIfNeeded(transform.position, transform.position, true))
        {
            Debug.Log($"Snake: OnMovementFinished. Только что приземлились на поле СТОП. Проверка паспорта активирована.");
            SavePlayerState();
            return;
        }
        else if (CheckAndShowPassportPanelIfNeeded(transform.position, transform.position, true))
        {
            Debug.Log($"Snake: OnMovementFinished. Только что приземлились на поле 14 лет. Панель получения паспорта активирована.");
            SavePlayerState();
            return;
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
                    SceneManager.LoadScene("Vopros");
                    return;
                }
            }
        }
        UpdateUIAndButton();
        SavePlayerState();
        Debug.Log($"Snake: OnMovementFinished. Все проверки пройдены, специальных полей не встречено. Готов к следующему ходу.");
    }

    void ForceStopMovementSequence(string reason)
    {
        Debug.Log($"Snake: Принудительная остановка последовательности движения из-за: {reason}");
        if (primaryMoveCoroutine != null) { StopCoroutine(primaryMoveCoroutine); primaryMoveCoroutine = null; }
        if (loopMoveCoroutine != null) { StopCoroutine(loopMoveCoroutine); loopMoveCoroutine = null; }
        isMoving = false;
        isMovingOnLoop = false;
        waitingForTurnChoice = false;
        currentDiceSteps = 0;
        OnMovementFinished();
    }

    bool CheckAndShowPassportPanelIfNeeded(Vector3 prevPos, Vector3 currentPos, bool isFinalCheckAfterStop = false)
    {
        if (passportCheckEventActive) return false;

        // Используем _sessionObtainedPassport ИЛИ PlayerPrefs для определения наличия паспорта
        bool passportEffectivelyObtained = _sessionObtainedPassport || (PlayerPrefs.GetInt(HasPassportKey, 0) == 1);
        Debug.Log($"CheckAndShowPassportPanelIfNeeded: Эффективный статус паспорта: {passportEffectivelyObtained} (Session: {_sessionObtainedPassport}, Prefs: {PlayerPrefs.GetInt(HasPassportKey, 0) == 1})");

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
            ShowPassportUIPanel();
            Debug.Log($"Snake: Поле 14 лет обнаружено. Требуется остановка. isFinalCheck:{isFinalCheckAfterStop}, passportEventCurrentlyActive:{passportEventCurrentlyActive}. Эффективный статус паспорта: {passportEffectivelyObtained}");
            return true;
        }
        else
        {
            if (passportEventCurrentlyActive) HidePassportUIPanel();
            return false;
        }
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
        if (passportUIPanel != null)
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
        PlayerPrefs.Save();
        _sessionObtainedPassport = true; // --- УСТАНАВЛИВАЕМ ФЛАГ СЕССИИ ---
        Debug.Log($"Статус паспорта после установки: PlayerPrefs: {(PlayerPrefs.GetInt(HasPassportKey, 0) == 1)}, SessionFlag: {_sessionObtainedPassport}");
        HidePassportUIPanel();
        OnMovementFinished();
    }

    bool CheckAndHandleStopFieldIfNeeded(Vector3 prevPos, Vector3 currentPos, bool isFinalCheck = false)
    {
        if (passportCheckEventActive) return true;
        if (passportEventCurrentlyActive && !isFinalCheck) return false;

        bool crossedXStop = (prevPos.x < stopFieldXCoordinate && currentPos.x >= stopFieldXCoordinate) ||
                            (prevPos.x > stopFieldXCoordinate && currentPos.x <= stopFieldXCoordinate);
        bool atXStopCoordinate = Mathf.Abs(currentPos.x - stopFieldXCoordinate) < stopFieldTolerance;

        if ((isFinalCheck && atXStopCoordinate) || (!isFinalCheck && (crossedXStop || atXStopCoordinate)))
        {
            if (hasStoppedOnStopFieldThisMove && !isFinalCheck) return true;
            Debug.Log($"Snake: Поле СТОП обнаружено. isFinal:{isFinalCheck}, crossed:{crossedXStop}, atX:{atXStopCoordinate}, hasStoppedOnStopFieldThisMove:{hasStoppedOnStopFieldThisMove}");
            transform.position = new Vector3(stopFieldXCoordinate, currentPos.y, currentPos.z);
            hasStoppedOnStopFieldThisMove = true;
            ShowPassportCheckPanel();
            return true;
        }
        else
        {
            if (passportCheckEventActive && Mathf.Abs(currentPos.x - stopFieldXCoordinate) >= stopFieldTolerance)
            {
                HidePassportCheckPanel();
            }
            return false;
        }
    }

    void ShowPassportCheckPanel()
    {
        if (passportCheckPanel != null && !passportCheckEventActive)
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
        if (passportCheckPanel != null)
        {
            passportCheckPanel.SetActive(false);
            if (presentPassportButton != null) presentPassportButton.gameObject.SetActive(false);
            passportCheckEventActive = false;
            UpdateButtonRollDiceVisibility();
            Debug.Log("Панель проверки паспорта (поле СТОП) скрыта.");
        }
    }

    public void OnPresentPassportButtonClicked()
    {
        Debug.Log("Кнопка 'Предъявить документ' нажата!");
        HidePassportCheckPanel();

        bool hasPassportPlayerPrefs = (PlayerPrefs.GetInt(HasPassportKey, 0) == 1);
        // --- ПРОВЕРЯЕМ ОБА ИСТОЧНИКА: PLAYERPREFS И ФЛАГ СЕССИИ ---
        bool effectivelyHasPassport = hasPassportPlayerPrefs || _sessionObtainedPassport;

        Debug.Log($"Статус паспорта: PlayerPrefs: {hasPassportPlayerPrefs}, SessionFlag: {_sessionObtainedPassport}. Эффективный статус: {effectivelyHasPassport}");

        if (effectivelyHasPassport)
        {
            Debug.Log("Проверка паспорта: УСПЕХ! Паспорт есть (эффективный).");
            if (passportSuccessPanel != null) passportSuccessPanel.SetActive(true);
            StartCoroutine(HidePanelAfterDelay(passportSuccessPanel, 1.5f, () => {
                OnMovementFinished();
            }));
        }
        else
        {
            Debug.Log("Проверка паспорта: ПРОВАЛ! Паспорта нет (эффективный).");
            if (passportFailPanel != null) passportFailPanel.SetActive(true);
            StartCoroutine(HidePanelAfterDelay(passportFailPanel, 2f, () => {
                StartCoroutine(MovePlayerBackThreeFieldsCoroutine(() => {
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
        float totalMoveDuration = moveDuration * 3;
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
        Debug.Log($"ReachedTurnPoint ВЫЗВАН. waitChoice: {waitingForTurnChoice}, isMoving: {isMoving}, onLoop: {isMovingOnLoop}, passportActive (14yo): {passportEventCurrentlyActive}, passportCheckActive (Stop): {passportCheckEventActive}");
        if (waitingForTurnChoice) { Debug.Log("ReachedTurnPoint: Возврат, так как уже ждем выбора."); return; }
        if (passportEventCurrentlyActive || passportCheckEventActive)
        {
            Debug.Log("ReachedTurnPoint: Возврат, так как активна панель специального события. Игрок должен сначала решить ее.");
            return;
        }
        Debug.Log("ReachedTurnPoint: Обработка точки поворота. Остановка текущего движения, если есть.");
        if (primaryMoveCoroutine != null) { StopCoroutine(primaryMoveCoroutine); primaryMoveCoroutine = null; }
        if (loopMoveCoroutine != null) { StopCoroutine(loopMoveCoroutine); loopMoveCoroutine = null; }
        isMoving = false; isMovingOnLoop = false;

        waitingForTurnChoice = true;
        stepsRemainingAfterTurn = currentDiceSteps;
        if (turnChoiceUI != null) turnChoiceUI.SetActive(true); else Debug.LogWarning("ReachedTurnPoint: turnChoiceUI не назначен!");
        UpdateUIAndButton();
    }

    public void HandleTurnChoice(bool turnLeft)
    {
        if (!waitingForTurnChoice) { Debug.LogWarning("HandleTurnChoice: Не ждем выбора."); return; }
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
                isMovingOnLoop = true; UpdateUIAndButton();
                if (loopMoveCoroutine != null) StopCoroutine(loopMoveCoroutine);
                loopMoveCoroutine = StartCoroutine(MoveAlongLoopCoroutine(targetLoopWaypoints, loopCost));
            }
            else
            {
                Debug.Log($"HandleTurnChoice: Недостаточно шагов для петли ({currentDiceSteps}/{loopCost}). Завершение хода.");
                OnMovementFinished();
            }
        }
        else
        {
            isMoving = true; UpdateUIAndButton();
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
    }

    IEnumerator MoveAlongLoopCoroutine(Transform[] waypoints, int costOfLoop)
    {
        isMovingOnLoop = true; isMoving = true; UpdateUIAndButton();
        if (waypoints.Length > 0) yield return StartCoroutine(RotateTowardsTargetCoroutine(waypoints[0].position));
        bool skipSpecialFieldChecksDuringFirstSegment = startedMoveFromSpecialField;

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
    }
    void UpdateMovesValueUIText(int moves) { if (movesValueText != null) movesValueText.text = moves.ToString(); }

    void UpdateButtonRollDiceVisibility()
    {
        if (buttonRollDice != null)
        {
            bool canRoll = !(isMoving || waitingForTurnChoice || isMovingOnLoop || passportCheckEventActive) && currentDiceSteps <= 0;
            buttonRollDice.SetActive(canRoll);
            Debug.Log($"UpdateButtonRollDiceVisibility: isMoving:{isMoving}, waiting:{waitingForTurnChoice}, onLoop:{isMovingOnLoop}, passportCheckActive:{passportCheckEventActive}, currentDiceSteps:{currentDiceSteps}. CanRoll: {canRoll}");
        }
    }

    public bool IsCurrentlyExecutingMovement()
    {
        return isMoving || waitingForTurnChoice || isMovingOnLoop || passportCheckEventActive;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"OnTriggerEnter с {other.name}. Шаги: {stepsTakenInCurrentMove}, isMoving: {isMoving}, DiceSteps: {currentDiceSteps}");
        if (other.CompareTag("TurnPointTrigger"))
        {
            if (isMoving || isMovingOnLoop)
            {
                ReachedTurnPoint();
            }
            return;
        }
        if (other.TryGetComponent<Vopros>(out _))
        {
            Debug.Log("Обнаружено поле Vopros через OnTriggerEnter.");
            if ((!isMoving || currentDiceSteps == 1) && stepsTakenInCurrentMove == 1)
            {
                Debug.Log("Условия для вопроса выполнены (через OnTriggerEnter) - активация вопроса!");
                SavePlayerState();
                SceneManager.LoadScene("Vopros");
                return;
            }
            return;
        }
    }
}