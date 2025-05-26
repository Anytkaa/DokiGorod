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
    public GameObject buttonRollDice;
    public Text movesValueText;

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

    [Header("Специальные Поля")]
    public float passportFieldXCoordinate = 80.0f; // Координата X для остановки
    public float passportFieldTolerance = 0.5f;   // Допуск для координаты X (чтобы не проскочить)
    public GameObject passportUIPanel; // ССЫЛКА НА ПАНЕЛЬ ПАСПОРТА (НАЗНАЧИТЬ В ИНСПЕКТОРЕ)

    // --- Статические и приватные переменные ---
    public static int money = 2000;

    private bool isMoving = false;
    private bool waitingForTurnChoice = false;
    private bool isMovingOnLoop = false;
    private int stepsRemainingAfterTurn = 0;
    private Coroutine primaryMoveCoroutine;
    private Coroutine loopMoveCoroutine;
    private int currentDiceSteps = 0;
    private bool hasStoppedForPassport = false; // Флаг, чтобы остановиться только один раз до получения паспорта

    private const string PosXKey = "PlayerPositionX_Snake_DokiGorod";
    private const string PosYKey = "PlayerPositionY_Snake_DokiGorod";
    private const string PosZKey = "PlayerPositionZ_Snake_DokiGorod";
    private const string RotYKey = "PlayerRotationY_Snake_DokiGorod";
    private const string DiceRollKey = "LastDiceRoll";

    void Start()
    {
        gameObject.name = "Player_Snake";
        Debug.Log("Snake.cs Start() called on scene: " + SceneManager.GetActiveScene().name);
        LoadPlayerState();

        if (!polpas14.pas14)
        {
            hasStoppedForPassport = false;
        }
        else
        {
            hasStoppedForPassport = true;
        }

        if (PlayerPrefs.HasKey(DiceRollKey))
        {
            int stepsFromDice = PlayerPrefs.GetInt(DiceRollKey);
            Debug.Log("Snake: Found dice roll result in PlayerPrefs (" + DiceRollKey + "): " + stepsFromDice);
            PlayerPrefs.DeleteKey(DiceRollKey); PlayerPrefs.Save();
            if (stepsFromDice > 0) StartMoving(stepsFromDice);
            else { UpdateMovesValueUIText(0); UpdateButtonRollDiceVisibility(); }
        }
        else { UpdateMovesValueUIText(0); UpdateButtonRollDiceVisibility(); }

        if (turnChoiceUI != null) turnChoiceUI.SetActive(false);
        if (turnLeftButton != null) { turnLeftButton.onClick.RemoveAllListeners(); turnLeftButton.onClick.AddListener(() => HandleTurnChoice(true)); }
        if (turnRightButton != null) { turnRightButton.onClick.RemoveAllListeners(); turnRightButton.onClick.AddListener(() => HandleTurnChoice(false)); }
    }

    void OnApplicationQuit()
    {
        Debug.Log("Snake: Application quitting. Clearing saved player state.");
        PlayerPrefs.DeleteKey(PosXKey); PlayerPrefs.DeleteKey(PosYKey); PlayerPrefs.DeleteKey(PosZKey); PlayerPrefs.DeleteKey(RotYKey);
        PlayerPrefs.Save();
    }

    void LoadPlayerState()
    {
        if (PlayerPrefs.HasKey(PosXKey))
        {
            transform.position = new Vector3(PlayerPrefs.GetFloat(PosXKey), PlayerPrefs.GetFloat(PosYKey), PlayerPrefs.GetFloat(PosZKey));
            transform.rotation = Quaternion.Euler(0, PlayerPrefs.GetFloat(RotYKey, transform.rotation.eulerAngles.y), 0);
            Debug.Log("Snake: Player state loaded. Pos: " + transform.position + ", RotY: " + transform.rotation.eulerAngles.y);
        }
        else Debug.Log("Snake: No saved player state found.");
    }

    public void SavePlayerState()
    {
        PlayerPrefs.SetFloat(PosXKey, transform.position.x); PlayerPrefs.SetFloat(PosYKey, transform.position.y);
        PlayerPrefs.SetFloat(PosZKey, transform.position.z); PlayerPrefs.SetFloat(RotYKey, transform.rotation.eulerAngles.y);
        PlayerPrefs.Save();
        Debug.Log("Snake: Player state saved. Pos: " + transform.position + ", RotY: " + transform.rotation.eulerAngles.y);
    }

    public void StartMoving(int steps)
    {
        if (IsCurrentlyExecutingMovement()) { Debug.LogWarning("Snake: Already moving/waiting. Movement ignored."); return; }
        currentDiceSteps = steps;
        Debug.Log("Snake: StartMoving called for " + currentDiceSteps + " steps.");
        UpdateUIAndButton();
        if (primaryMoveCoroutine != null) StopCoroutine(primaryMoveCoroutine);
        primaryMoveCoroutine = StartCoroutine(MoveStepsCoroutine());
    }

    IEnumerator MoveStepsCoroutine()
    {
        if (isMovingOnLoop) { Debug.Log("Snake: MoveStepsCoroutine called, but on a loop. Aborting."); yield break; }
        isMoving = true;
        UpdateUIAndButton();

        while (currentDiceSteps > 0 && !waitingForTurnChoice && !isMovingOnLoop)
        {
            Vector3 startPosition = transform.position;
            Vector3 endPosition = startPosition + transform.forward * stepDistance;
            float elapsedTime = 0;
            Debug.Log($"Snake: Main path step. From {startPosition} to {endPosition}. Steps left: {currentDiceSteps - 1}");
            Vector3 previousPosition = startPosition;

            while (elapsedTime < moveDuration)
            {
                previousPosition = transform.position;
                transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / moveDuration);
                elapsedTime += Time.deltaTime;
                if (CheckAndHandlePassportStopByCoordinate(previousPosition, transform.position)) yield break;
                yield return null;
            }
            previousPosition = transform.position;
            transform.position = endPosition;
            if (CheckAndHandlePassportStopByCoordinate(previousPosition, transform.position)) yield break;

            currentDiceSteps--;
            UpdateMovesValueUIText(currentDiceSteps);
        }
        isMoving = false;
        Debug.Log("Snake: MoveStepsCoroutine finished. Waiting: " + waitingForTurnChoice + ", OnLoop: " + isMovingOnLoop + ", StepsLeft: " + currentDiceSteps);
        if (!waitingForTurnChoice && !isMovingOnLoop) OnMovementFinished();
    }

    void OnMovementFinished()
    {
        Debug.Log("Snake: All movement sequence completed. Final steps: " + currentDiceSteps);
        if (currentDiceSteps < 0) currentDiceSteps = 0;
        if (!hasStoppedForPassport && !polpas14.pas14 && Mathf.Abs(transform.position.x - passportFieldXCoordinate) < passportFieldTolerance)
        {
            Debug.Log("Snake: Final check - landed on passport coordinate. Handling stop.");
            HandleSpecialFieldStop();
            return;
        }
        isMoving = false; isMovingOnLoop = false; waitingForTurnChoice = false;
        UpdateUIAndButton();
        SavePlayerState();
    }

    bool CheckAndHandlePassportStopByCoordinate(Vector3 prevPos, Vector3 currentPos)
    {
        if (!polpas14.pas14 && !hasStoppedForPassport)
        {
            bool crossedX = (prevPos.x < passportFieldXCoordinate && currentPos.x >= passportFieldXCoordinate) ||
                            (prevPos.x > passportFieldXCoordinate && currentPos.x <= passportFieldXCoordinate);
            bool veryCloseToX = Mathf.Abs(currentPos.x - passportFieldXCoordinate) < passportFieldTolerance;

            if (crossedX || veryCloseToX)
            {
                Debug.Log($"Snake: Reached/Crossed Passport X Coordinate ({passportFieldXCoordinate}). Current X: {currentPos.x}. Stopping.");
                transform.position = new Vector3(passportFieldXCoordinate, currentPos.y, currentPos.z);
                HandleSpecialFieldStop();
                return true;
            }
        }
        return false;
    }

    void HandleSpecialFieldStop()
    {
        if (primaryMoveCoroutine != null) { StopCoroutine(primaryMoveCoroutine); primaryMoveCoroutine = null; }
        if (loopMoveCoroutine != null) { StopCoroutine(loopMoveCoroutine); loopMoveCoroutine = null; }
        isMoving = false; isMovingOnLoop = false; waitingForTurnChoice = false;
        currentDiceSteps = 0;
        hasStoppedForPassport = true;
        UpdateUIAndButton();
        if (passportUIPanel != null && !polpas14.pas14)
        {
            passportUIPanel.SetActive(true);
            Debug.Log("Snake: Passport UI activated by coordinate stop.");
        }
        else if (passportUIPanel == null) Debug.LogWarning("Snake: passportUIPanel is not assigned!");
        SavePlayerState();
    }

    // ЭТОТ МЕТОД УЖЕ БЫЛ В ВАШЕМ КОДЕ, ОСТАВЛЯЕМ ЕГО КАК ЕСТЬ
    public void ReachedTurnPoint()
    {
        if (waitingForTurnChoice)
        {
            Debug.Log("Snake: ReachedTurnPoint, but already waiting.");
            return;
        }
        Debug.Log("Snake: Reached Turn Point. Current steps: " + currentDiceSteps);
        isMoving = false; isMovingOnLoop = false;
        if (primaryMoveCoroutine != null) { StopCoroutine(primaryMoveCoroutine); primaryMoveCoroutine = null; }
        if (loopMoveCoroutine != null) { StopCoroutine(loopMoveCoroutine); loopMoveCoroutine = null; }
        waitingForTurnChoice = true;
        stepsRemainingAfterTurn = currentDiceSteps;
        if (turnChoiceUI != null) turnChoiceUI.SetActive(true);
        UpdateUIAndButton();
    }

    public void HandleTurnChoice(bool turnLeft)
    {
        if (!waitingForTurnChoice) { Debug.LogWarning("Snake: HandleTurnChoice called, but not waiting."); return; }
        if (turnChoiceUI != null) turnChoiceUI.SetActive(false);
        waitingForTurnChoice = false;
        currentDiceSteps = stepsRemainingAfterTurn;
        Debug.Log($"Snake: HandleTurnChoice. Left: {turnLeft}. Steps available: {currentDiceSteps}");
        Transform[] targetLoopWaypoints = turnLeft ? leftLoopWaypoints : rightLoopWaypoints;
        int loopCost = turnLeft ? leftLoopCost : rightLoopCost;

        if (targetLoopWaypoints != null && targetLoopWaypoints.Length > 0)
        {
            if (currentDiceSteps >= loopCost)
            {
                Debug.Log($"Snake: Starting {(turnLeft ? "left" : "right")} loop. Cost: {loopCost}.");
                isMovingOnLoop = true; UpdateUIAndButton();
                if (loopMoveCoroutine != null) StopCoroutine(loopMoveCoroutine);
                loopMoveCoroutine = StartCoroutine(MoveAlongLoopCoroutine(targetLoopWaypoints, loopCost));
            }
            else { Debug.Log($"Snake: Not enough steps for loop. Ending turn."); OnMovementFinished(); }
        }
        else
        {
            Debug.Log("Snake: Standard turn (no loop waypoints).");
            isMoving = true; UpdateUIAndButton();
            float rotationYAmount = turnLeft ? -90f : 90f;
            if (primaryMoveCoroutine != null) StopCoroutine(primaryMoveCoroutine);
            primaryMoveCoroutine = StartCoroutine(RotateCoroutine(rotationYAmount, () => {
                isMoving = false;
                if (currentDiceSteps > 0)
                {
                    if (CheckAndHandlePassportStopByCoordinate(transform.position, transform.position)) return;
                    Debug.Log("Snake: Continuing on main path after turn."); StartMoving(currentDiceSteps);
                }
                else OnMovementFinished();
            }));
        }
    }

    IEnumerator MoveAlongLoopCoroutine(Transform[] waypoints, int costOfLoop)
    {
        isMovingOnLoop = true; isMoving = true; UpdateUIAndButton();
        if (waypoints.Length > 0) yield return StartCoroutine(RotateTowardsTargetCoroutine(waypoints[0].position));
        Vector3 previousPosition;
        for (int i = 0; i < waypoints.Length; i++)
        {
            Vector3 startPosition = transform.position; Vector3 endPosition = waypoints[i].position;
            Debug.Log($"Snake: Loop step to waypoint {i} ({endPosition}).");
            float elapsedTime = 0;
            previousPosition = startPosition;
            while (elapsedTime < loopMoveDurationPerWaypoint)
            {
                previousPosition = transform.position;
                transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / loopMoveDurationPerWaypoint);
                if (i + 1 < waypoints.Length) RotateTowardsTargetDuringMovement(waypoints[i + 1].position);
                else RotateTowardsTargetDuringMovement(endPosition + waypoints[i].forward);
                elapsedTime += Time.deltaTime;
                if (CheckAndHandlePassportStopByCoordinate(previousPosition, transform.position)) yield break;
                yield return null;
            }
            previousPosition = transform.position;
            transform.position = endPosition;
            if (CheckAndHandlePassportStopByCoordinate(previousPosition, transform.position)) yield break;
        }
        currentDiceSteps -= costOfLoop; UpdateMovesValueUIText(currentDiceSteps);
        Debug.Log($"Snake: Loop finished. Cost: {costOfLoop}. Steps: {currentDiceSteps}.");
        if (waypoints.Length > 0) transform.rotation = waypoints[waypoints.Length - 1].rotation;
        isMovingOnLoop = false; isMoving = false;
        if (CheckAndHandlePassportStopByCoordinate(transform.position, transform.position)) yield break;
        if (currentDiceSteps > 0) { Debug.Log("Snake: Continuing on main path after loop."); StartMoving(currentDiceSteps); }
        else OnMovementFinished();
    }

    IEnumerator RotateCoroutine(float angleY, System.Action onRotationComplete)
    {
        isMoving = true; UpdateUIAndButton();
        Quaternion s = transform.rotation, e = s * Quaternion.Euler(0, angleY, 0); float t = 0;
        while (t < rotateDuration) { transform.rotation = Quaternion.Slerp(s, e, t / rotateDuration); t += Time.deltaTime; yield return null; }
        transform.rotation = e; isMoving = false;
        onRotationComplete?.Invoke();
    }

    IEnumerator RotateTowardsTargetCoroutine(Vector3 targetPos)
    {
        isMoving = true; UpdateUIAndButton();
        Vector3 dir = (targetPos - transform.position).normalized;
        if (dir != Vector3.zero)
        {
            Quaternion tRot = Quaternion.LookRotation(dir); float t = 0; Quaternion sRot = transform.rotation;
            while (t < rotateDuration) { transform.rotation = Quaternion.Slerp(sRot, tRot, t / rotateDuration); t += Time.deltaTime; yield return null; }
            transform.rotation = tRot;
        }
        isMoving = false;
    }

    void RotateTowardsTargetDuringMovement(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position).normalized;
        if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * loopRotateSpeed);
    }

    void UpdateUIAndButton() { UpdateMovesValueUIText(currentDiceSteps); UpdateButtonRollDiceVisibility(); }
    void UpdateMovesValueUIText(int m) { if (movesValueText != null) movesValueText.text = m.ToString(); }
    void UpdateButtonRollDiceVisibility() { if (buttonRollDice != null) buttonRollDice.SetActive(!IsCurrentlyExecutingMovement() && currentDiceSteps <= 0); }
    public bool IsCurrentlyExecutingMovement() { return isMoving || waitingForTurnChoice || isMovingOnLoop; }

    void OnTriggerEnter(Collider other)
    {
        if (IsCurrentlyExecutingMovement()) return;
        if (currentDiceSteps <= 0 && !waitingForTurnChoice)
        {
            if (other.TryGetComponent(out Vopros v)) { SavePlayerState(); SceneManager.LoadScene("Vopros"); }
            return;
        }
        Debug.Log("Snake: OnTriggerEnter with " + other.name);
        // ИСПРАВЛЕНИЕ ЗДЕСЬ: Убеждаемся, что вызываем метод с правильным именем ReachedTurnPoint()
        if (other.CompareTag("TurnPointTrigger") && !waitingForTurnChoice)
        {
            ReachedTurnPoint(); // Правильный вызов метода
        }
        else if (other.TryGetComponent(out Vopros v)) { SavePlayerState(); SceneManager.LoadScene("Vopros"); }
    }
}