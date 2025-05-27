// snake.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// ЗАГЛУШКА - ЗАМЕНИТЕ НА ВАШ КЛАСС ИЛИ УДАЛITE ССЫЛКИ
/*
public static class polpas14 
{
    public static bool pas14 = false; 
}
*/

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
    public float passportFieldXCoordinate = 80.0f;
    public float passportFieldTolerance = 0.5f;
    public GameObject passportUIPanel;
    public GameObject getPassportButtonObject; 

    public float stopFieldXCoordinate = 140.0f;
    public float stopFieldTolerance = 0.5f; 
    private bool hasStoppedOnStopFieldThisMove = false; 

    public static int money = 5000;

    private bool isMoving = false;
    private bool waitingForTurnChoice = false;
    private bool isMovingOnLoop = false;
    private int stepsRemainingAfterTurn = 0;
    private Coroutine primaryMoveCoroutine;
    private Coroutine loopMoveCoroutine;
    private int currentDiceSteps = 0;

    private bool passportEventCurrentlyActive = false;
    private bool startedMoveFromSpecialField = false; // Флаг: текущее движение началось со спец. поля

    private const string PosXKey = "PlayerPositionX_Snake_DokiGorod";
    private const string PosYKey = "PlayerPositionY_Snake_DokiGorod";
    private const string PosZKey = "PlayerPositionZ_Snake_DokiGorod";
    private const string RotYKey = "PlayerRotationY_Snake_DokiGorod";
    private const string DiceRollKey = "LastDiceRoll";

    void Start()
    {
        gameObject.name = "Player_Snake";
        LoadPlayerState(); // Включает начальную проверку на спец. поля

        if (PlayerPrefs.HasKey(DiceRollKey))
        {
            int stepsFromDice = PlayerPrefs.GetInt(DiceRollKey);
            PlayerPrefs.DeleteKey(DiceRollKey); PlayerPrefs.Save();
            if (stepsFromDice > 0) StartMoving(stepsFromDice);
            else { UpdateMovesValueUIText(0); UpdateButtonRollDiceVisibility(); }
        }
        else { UpdateMovesValueUIText(0); UpdateButtonRollDiceVisibility(); }
        
        if (passportUIPanel != null) passportUIPanel.SetActive(false);
        if (getPassportButtonObject != null) getPassportButtonObject.SetActive(false);
        if (turnChoiceUI != null) turnChoiceUI.SetActive(false);

        if (turnLeftButton != null) { turnLeftButton.onClick.RemoveAllListeners(); turnLeftButton.onClick.AddListener(() => HandleTurnChoice(true)); }
        if (turnRightButton != null) { turnRightButton.onClick.RemoveAllListeners(); turnRightButton.onClick.AddListener(() => HandleTurnChoice(false)); }
    }

    void OnApplicationQuit()
    {
        PlayerPrefs.DeleteKey(PosXKey); PlayerPrefs.DeleteKey(PosYKey); PlayerPrefs.DeleteKey(PosZKey); PlayerPrefs.DeleteKey(RotYKey);
        PlayerPrefs.Save();
    }

    void LoadPlayerState()
    {
        if (PlayerPrefs.HasKey(PosXKey))
        {
            transform.position = new Vector3(PlayerPrefs.GetFloat(PosXKey), PlayerPrefs.GetFloat(PosYKey), PlayerPrefs.GetFloat(PosZKey));
            transform.rotation = Quaternion.Euler(0, PlayerPrefs.GetFloat(RotYKey, transform.rotation.eulerAngles.y), 0);
        }
        // При загрузке проверяем, не стоим ли мы уже на спец. поле
        // Передаем prevPos = currentPos, так как мы не двигались
        if (!CheckAndHandleStopFieldIfNeeded(transform.position, transform.position, true)) 
        {
            CheckAndShowPassportPanelIfNeeded(transform.position, transform.position, true); 
        }
    }

    public void SavePlayerState()
    {
        PlayerPrefs.SetFloat(PosXKey, transform.position.x); PlayerPrefs.SetFloat(PosYKey, transform.position.y);
        PlayerPrefs.SetFloat(PosZKey, transform.position.z); PlayerPrefs.SetFloat(RotYKey, transform.rotation.eulerAngles.y);
        PlayerPrefs.Save();
    }

    public void StartMoving(int steps)
    {
        if (isMoving || waitingForTurnChoice || isMovingOnLoop) 
        { 
            Debug.LogWarning($"Snake: StartMoving called but character is busy. Ignored."); 
            return; 
        }
        Debug.Log($"Snake: StartMoving initiated with {steps} steps.");

        stepsTaken = 0;

        // Проверяем, начинаем ли мы движение с одного из специальных полей
        startedMoveFromSpecialField = (passportEventCurrentlyActive || hasStoppedOnStopFieldThisMove);

        if (passportEventCurrentlyActive) HidePassportUIPanel();
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
        stepsTaken = 0; // Сбрасываем счетчик при начале движения
        UpdateUIAndButton();

        bool firstMicroStepTakenInSequence = false; // Отслеживаем самый первый микро-шаг всей последовательности

        while (currentDiceSteps > 0 && !waitingForTurnChoice && !isMovingOnLoop)
        {
            Vector3 startPositionOfThisStep = transform.position; 
            Vector3 endPositionThisStep = startPositionOfThisStep + transform.forward * stepDistance;
            float elapsedTime = 0;

            while (elapsedTime < moveDuration)
            {
                Vector3 posBeforeLerp = transform.position;
                transform.position = Vector3.Lerp(startPositionOfThisStep, endPositionThisStep, elapsedTime / moveDuration);
                elapsedTime += Time.deltaTime;

                if (startedMoveFromSpecialField && !firstMicroStepTakenInSequence)
                {
                    // Если мы начали со спец. поля, пропускаем проверку на первом микро-шаге
                }
                else
                {
                    if (CheckAndHandleStopFieldIfNeeded(posBeforeLerp, transform.position)) { isMoving = false; OnMovementFinished(); yield break; }
                    if (CheckAndShowPassportPanelIfNeeded(posBeforeLerp, transform.position)) { isMoving = false; OnMovementFinished(); yield break; }
                }
                firstMicroStepTakenInSequence = true; // Отмечаем, что микро-шаг сделан
                yield return null;
            }
            transform.position = endPositionThisStep; 
            startedMoveFromSpecialField = false; // После первого полного шага мы точно ушли со стартового поля

            currentDiceSteps--;
            stepsTaken++; // Увеличиваем счетчик шагов
            UpdateMovesValueUIText(currentDiceSteps);

            if (CheckAndHandleStopFieldIfNeeded(startPositionOfThisStep, transform.position, true)) { isMoving = false; OnMovementFinished(); yield break; }
            if (CheckAndShowPassportPanelIfNeeded(startPositionOfThisStep, transform.position, true)) { isMoving = false; OnMovementFinished(); yield break; }
        }
        isMoving = false; 
        OnMovementFinished(); // Вызываем всегда после завершения цикла или прерывания
    }
    
    void OnMovementFinished()
    {
    isMoving = false; 
    isMovingOnLoop = false; 
    waitingForTurnChoice = false; 
    startedMoveFromSpecialField = false; // Сбрасываем флаг

    // Финальные проверки, если не были прерваны ранее
    if (!hasStoppedOnStopFieldThisMove && !passportEventCurrentlyActive)
    {
        if (!CheckAndHandleStopFieldIfNeeded(transform.position, transform.position, true)) 
        {
            CheckAndShowPassportPanelIfNeeded(transform.position, transform.position, true); 
        }
    }

    // Проверка на активацию вопроса после остановки (если выпало 2)
    if (stepsTaken == 2 && currentDiceSteps == 0)
    {
        // Проверяем все коллайдеры в небольшом радиусе вокруг игрока
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.5f);
        foreach (var collider in hitColliders)
        {
            if (collider.TryGetComponent<Vopros>(out _))
            {
                Debug.Log("Player stopped on question field after rolling 2 - activating question!");
                SavePlayerState();
                SceneManager.LoadScene("Vopros");
                return; // Выходим, чтобы не выполнять остальной код
            }
        }
    }

    UpdateUIAndButton(); 
    SavePlayerState();
    Debug.Log($"Snake: OnMovementFinished. Steps: {currentDiceSteps}. PassportActive: {passportEventCurrentlyActive}, StoppedOnStop: {hasStoppedOnStopFieldThisMove}");
    }
    
    // --- Логика Паспортного Поля ---
    bool CheckAndShowPassportPanelIfNeeded(Vector3 prevPos, Vector3 currentPos, bool isFinalCheckAfterStop = false)
    {
        if (hasStoppedOnStopFieldThisMove && !isFinalCheckAfterStop) return false; 

        bool passportAlreadyObtained = false; // ЗАГЛУШКА
        if (passportAlreadyObtained || passportUIPanel == null)
        {
            if (passportEventCurrentlyActive) HidePassportUIPanel();
            return false; 
        }

        // Если мы только что начали движение со спец. поля, не показываем панель сразу же
        if (startedMoveFromSpecialField && !isFinalCheckAfterStop && Vector3.Distance(prevPos, currentPos) < 0.1f) 
        {
             return false;
        }

        bool isOnPassportField = Mathf.Abs(currentPos.x - passportFieldXCoordinate) < passportFieldTolerance;
        if (isOnPassportField)
        {
            if (!passportEventCurrentlyActive)
            {
                ShowPassportUIPanel();
                // Останавливаем движение только если это не финальная проверка ПОСЛЕ ПОЛНОЙ ОСТАНОВКИ
                // и если мы не стоим на месте (т.е. это не вызов из LoadPlayerState)
                if (!isFinalCheckAfterStop || (isFinalCheckAfterStop && Vector3.Distance(prevPos, currentPos) > 0.01f) ) {
                    ForceStopMovementSequence("passport field");
                }
                return true;
            }
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
        if (passportUIPanel != null && !passportEventCurrentlyActive)
        {
            passportUIPanel.SetActive(true);
            if (getPassportButtonObject != null) getPassportButtonObject.SetActive(true);
            passportEventCurrentlyActive = true;
        }
    }

    void HidePassportUIPanel()
    {
        if (passportUIPanel != null && passportEventCurrentlyActive)
        {
            passportUIPanel.SetActive(false);
            if (getPassportButtonObject != null) getPassportButtonObject.SetActive(false);
            passportEventCurrentlyActive = false;
        }
    }

    // --- Логика Поля СТОП ---
    bool CheckAndHandleStopFieldIfNeeded(Vector3 prevPos, Vector3 currentPos, bool isFinalCheck = false)
    {
        if (hasStoppedOnStopFieldThisMove && isFinalCheck && Mathf.Abs(currentPos.x - stopFieldXCoordinate) < stopFieldTolerance) return true; 
        if (passportEventCurrentlyActive && !isFinalCheck) return false; 

        // Если мы только что начали движение со спец. поля, не останавливаемся сразу же
        if (startedMoveFromSpecialField && !isFinalCheck && Vector3.Distance(prevPos, currentPos) < 0.1f)
        {
            return false;
        }

        bool crossedXStop = (prevPos.x < stopFieldXCoordinate && currentPos.x >= stopFieldXCoordinate) ||
                            (prevPos.x > stopFieldXCoordinate && currentPos.x <= stopFieldXCoordinate);
        bool atXStopCoordinate = Mathf.Abs(currentPos.x - stopFieldXCoordinate) < stopFieldTolerance;

        if ((isFinalCheck && atXStopCoordinate) || (!isFinalCheck && (crossedXStop || atXStopCoordinate))) {
            if (!hasStoppedOnStopFieldThisMove || (isFinalCheck && atXStopCoordinate && !hasStoppedOnStopFieldThisMove) ) {
                Debug.Log($"Snake: STOP Field. isFinal:{isFinalCheck}, crossed:{crossedXStop}, atX:{atXStopCoordinate}, alreadyStopped:{hasStoppedOnStopFieldThisMove}");
                transform.position = new Vector3(stopFieldXCoordinate, currentPos.y, currentPos.z);
                ForceStopMovementSequence("STOP field");
                hasStoppedOnStopFieldThisMove = true;
            }
            return true;
        }
        return false;
    }

    void ForceStopMovementSequence(string reason)
    {
        Debug.Log($"Snake: Forcing stop of movement sequence due to: {reason}");
        if (primaryMoveCoroutine != null) { StopCoroutine(primaryMoveCoroutine); primaryMoveCoroutine = null; }
        if (loopMoveCoroutine != null) { StopCoroutine(loopMoveCoroutine); loopMoveCoroutine = null; }
        isMoving = false;
        isMovingOnLoop = false;
        waitingForTurnChoice = false;
        currentDiceSteps = 0;
    }

    // --- Логика Развилок и Петель ---
    public void ReachedTurnPoint()
    {
        // ... (остается код из предыдущего вашего сообщения, который исправлял развилки) ...
        // Важно: Ensure that the conditions here are correct and do not conflict
        // with the new 'startedMoveFromSpecialField' logic if a turn point is on a special field.
        // For now, assuming turn points are not ON the exact X-coordinate of special fields.
        Debug.Log($"ReachedTurnPoint CALLED. waitChoice: {waitingForTurnChoice}, isMoving: {isMoving}, onLoop: {isMovingOnLoop}, passportActive: {passportEventCurrentlyActive}, stopFieldActive: {hasStoppedOnStopFieldThisMove}");

        if (waitingForTurnChoice) { Debug.Log("ReachedTurnPoint: Returned because already waitingForTurnChoice."); return; }
        if (passportEventCurrentlyActive) { Debug.Log("ReachedTurnPoint: Returned because passport panel is active."); return; }
        if (hasStoppedOnStopFieldThisMove) { Debug.Log("ReachedTurnPoint: Returned because stopped on STOP field."); return; }

        Debug.Log("ReachedTurnPoint: Processing turn point. Stopping current movement if any.");
        if (primaryMoveCoroutine != null) { StopCoroutine(primaryMoveCoroutine); primaryMoveCoroutine = null; }
        if (loopMoveCoroutine != null) { StopCoroutine(loopMoveCoroutine); loopMoveCoroutine = null; }
        isMoving = false; isMovingOnLoop = false;
        
        waitingForTurnChoice = true;
        stepsRemainingAfterTurn = currentDiceSteps;
        if (turnChoiceUI != null) turnChoiceUI.SetActive(true); else Debug.LogWarning("ReachedTurnPoint: turnChoiceUI is not assigned!");
        UpdateUIAndButton();
    }

    public void HandleTurnChoice(bool turnLeft)
    {
        if (!waitingForTurnChoice) { Debug.LogWarning("HandleTurnChoice: Not waiting for choice."); return; }
        if (turnChoiceUI != null) turnChoiceUI.SetActive(false);
        waitingForTurnChoice = false; 
        currentDiceSteps = stepsRemainingAfterTurn; 
        
        // Если начинаем движение с развилки, мы точно не "только что ушли со спец. поля" в том же смысле
        startedMoveFromSpecialField = false; 
        if (passportEventCurrentlyActive) HidePassportUIPanel();
        hasStoppedOnStopFieldThisMove = false; 

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
            else { Debug.Log($"HandleTurnChoice: Not enough steps for loop ({currentDiceSteps}/{loopCost}). Ending turn."); OnMovementFinished(); }
        }
        else 
        {
            isMoving = true; UpdateUIAndButton(); 
            float rotationYAmount = turnLeft ? -90f : 90f;
            if (primaryMoveCoroutine != null) StopCoroutine(primaryMoveCoroutine);
            primaryMoveCoroutine = StartCoroutine(RotateCoroutine(rotationYAmount, () => {
                // isMoving сбрасывается внутри RotateCoroutine
                if (currentDiceSteps > 0)
                {
                    // После поворота, мы "начинаем" движение, так что startedMoveFromSpecialField должно быть false
                    startedMoveFromSpecialField = false; 
                    if (CheckAndHandleStopFieldIfNeeded(transform.position, transform.position, true)) { OnMovementFinished(); return; }
                    if (CheckAndShowPassportPanelIfNeeded(transform.position, transform.position, true)) { OnMovementFinished(); return; } 
                    StartMoving(currentDiceSteps); 
                }
                else OnMovementFinished();
            }));
        }
    }

    IEnumerator MoveAlongLoopCoroutine(Transform[] waypoints, int costOfLoop)
    {
        isMovingOnLoop = true; isMoving = true; UpdateUIAndButton();
        if (waypoints.Length > 0) yield return StartCoroutine(RotateTowardsTargetCoroutine(waypoints[0].position));
        
        bool firstMicroStepTakenInLoop = false;

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

                if (startedMoveFromSpecialField && !firstMicroStepTakenInLoop) {
                    // Пропускаем проверку на первом микро-шаге, если начали с поля
                } else {
                    if (CheckAndHandleStopFieldIfNeeded(posBeforeLerp, transform.position)) { isMovingOnLoop = false; isMoving = false; OnMovementFinished(); yield break; }
                    if (CheckAndShowPassportPanelIfNeeded(posBeforeLerp, transform.position)) { isMovingOnLoop = false; isMoving = false; OnMovementFinished(); yield break; }
                }
                firstMicroStepTakenInLoop = true;
                yield return null;
            }
            transform.position = endPositionThisStep; 
            startedMoveFromSpecialField = false; // После первого полного шага в петле

            if (CheckAndHandleStopFieldIfNeeded(startPositionOfThisStep, transform.position, true)) { isMovingOnLoop = false; isMoving = false; OnMovementFinished(); yield break; }
            if (CheckAndShowPassportPanelIfNeeded(startPositionOfThisStep, transform.position, true)) { isMovingOnLoop = false; isMoving = false; OnMovementFinished(); yield break; }
        }
        currentDiceSteps -= costOfLoop; UpdateMovesValueUIText(currentDiceSteps);
        if (waypoints.Length > 0) transform.rotation = waypoints[waypoints.Length - 1].rotation; 
        
        isMovingOnLoop = false; isMoving = false;
        startedMoveFromSpecialField = false; // В конце петли

        if (CheckAndHandleStopFieldIfNeeded(transform.position, transform.position, true)) { OnMovementFinished(); yield break; }
        if (CheckAndShowPassportPanelIfNeeded(transform.position, transform.position, true)) { OnMovementFinished(); yield break; }

        if (currentDiceSteps > 0) { StartMoving(currentDiceSteps); }
        else OnMovementFinished();
    }

    // --- Вспомогательные корутины и методы ---
    IEnumerator RotateCoroutine(float angleY, System.Action onRotationComplete)
    {
        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0, angleY, 0);
        float elapsedTime = 0;
        while (elapsedTime < rotateDuration) { transform.rotation = Quaternion.Slerp(startRot, endRot, elapsedTime / rotateDuration); elapsedTime += Time.deltaTime; yield return null; }
        transform.rotation = endRot;
        isMoving = false; // Сбрасываем флаг ЗДЕСЬ
        onRotationComplete?.Invoke();
    }

    IEnumerator RotateTowardsTargetCoroutine(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction == Vector3.zero) { isMoving = false; yield break; }
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float elapsedTime = 0;
        Quaternion startRotation = transform.rotation;
        while (elapsedTime < rotateDuration) { transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime / rotateDuration); elapsedTime += Time.deltaTime; yield return null; }
        transform.rotation = targetRotation;
        isMoving = false; // Сбрасываем флаг ЗДЕСЬ
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
    
    void UpdateButtonRollDiceVisibility() {
        if (buttonRollDice != null) {
            bool canRoll = !(isMoving || waitingForTurnChoice || isMovingOnLoop) && currentDiceSteps <= 0;
            buttonRollDice.SetActive(canRoll);
        }
    }

    public bool IsCurrentlyExecutingMovement() { 
        return isMoving || waitingForTurnChoice || isMovingOnLoop;
    }

    private int stepsTaken = 0; // Счетчик шагов в текущем ходе

    // Модифицируем метод OnTriggerEnter:
    void OnTriggerEnter(Collider other)
{
    Debug.Log($"OnTriggerEnter with {other.name}. Steps: {stepsTaken}, isMoving: {isMoving}, DiceSteps: {currentDiceSteps}");

    // Обработка поворота имеет приоритет
    if (other.CompareTag("TurnPointTrigger")) 
    {
        ReachedTurnPoint();
        return;
    }

    // Для вопроса проверяем только компонент Vopros
    if (other.TryGetComponent<Vopros>(out _))
    {
        Debug.Log("Detected Vopros field");
        
        // Modified conditions:
        // 1. Either we're not moving OR we're on the last step
        // 2. Total steps in this move were exactly 2
        if ((!isMoving || currentDiceSteps == 1) && stepsTaken == 1) 
        {
            Debug.Log("Question conditions met - activating question!");
            SavePlayerState();
            SceneManager.LoadScene("Vopros");
        }
        return;
    }
}
}