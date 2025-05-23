using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class snake : MonoBehaviour
{
    // Публичные переменные
    public GameObject pas14;
    public GameObject pas20;
    public GameObject buttonRollDice;
    public static int money = 5000;

    [Header("Настройки движения")]
    public float stepDistance = 10.0f;
    public float moveDuration = 0.5f;
    public float rotateDuration = 0.3f;

    [Header("UI для поворотов")]
    public GameObject turnChoiceUI;
    public Button turnLeftButton;
    public Button turnRightButton;

    [Header("UI отображения")]
    public Text movesValueText;

    // Приватные переменные
    private bool isMoving = false;
    private bool waitingForTurnChoice = false;
    private int stepsRemainingAfterTurn = 0;
    private Coroutine moveCoroutine;
    private int currentDiceSteps = 0;

    // Ключи для PlayerPrefs
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

        if (PlayerPrefs.HasKey(DiceRollKey))
        {
            int stepsFromDice = PlayerPrefs.GetInt(DiceRollKey);
            Debug.Log("Snake: Found dice roll result in PlayerPrefs (" + DiceRollKey + "): " + stepsFromDice);
            PlayerPrefs.DeleteKey(DiceRollKey);
            PlayerPrefs.Save();

            if (stepsFromDice > 0)
            {
                UpdateMovesValueUIText(stepsFromDice);
                Debug.Log("Snake: Calling StartMoving with " + stepsFromDice + " steps from Start().");
                StartMoving(stepsFromDice);
            }
            else
            {
                UpdateMovesValueUIText(0);
                Debug.Log("Snake: Steps from dice is 0 or less, not moving from initial dice roll.");
                UpdateButtonRollDiceVisibility();
            }
        }
        else
        {
            Debug.Log("Snake: No dice roll result found in PlayerPrefs with key: " + DiceRollKey);
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
            Debug.Log("Snake: Player state loaded. Position: " + transform.position + ", RotationY: " + savedRotationY);
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
        Debug.Log("Snake: Player state saved. Position: " + transform.position + ", RotationY: " + transform.rotation.eulerAngles.y);
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

    public void StartMoving(int steps)
    {
        if (isMoving || waitingForTurnChoice)
        {
            Debug.LogWarning("Snake: Already moving or waiting for turn choice. New movement for " + steps + " steps ignored.");
            return;
        }
        currentDiceSteps = steps;
        UpdateMovesValueUIText(currentDiceSteps);
        Debug.Log("Snake: StartMoving called for " + currentDiceSteps + " steps.");
        UpdateButtonRollDiceVisibility();

        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveStepsCoroutine(currentDiceSteps));
    }

    IEnumerator MoveStepsCoroutine(int stepsToMoveInitially)
    {
        isMoving = true;
        Debug.Log("Snake: MoveStepsCoroutine started. Initial steps: " + stepsToMoveInitially + ". Current steps: " + currentDiceSteps);

        while (currentDiceSteps > 0 && !waitingForTurnChoice)
        {
            Vector3 startPosition = transform.position;
            Vector3 endPosition = startPosition + transform.forward * stepDistance;
            float elapsedTime = 0;

            Debug.Log("Snake: Moving one step from " + startPosition + " to " + endPosition + ". Steps remaining: " + (currentDiceSteps - 1));

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
        Debug.Log("Snake: MoveStepsCoroutine finished. isMoving: " + isMoving + ", waitingForTurnChoice: " + waitingForTurnChoice + ", currentDiceSteps: " + currentDiceSteps);

        if (!waitingForTurnChoice)
        {
            OnMovementFinished();
        }
    }

    void OnMovementFinished()
    {
        Debug.Log("Snake: All movement completed.");
        if (currentDiceSteps < 0) currentDiceSteps = 0;
        UpdateMovesValueUIText(currentDiceSteps);
        UpdateButtonRollDiceVisibility();
        SavePlayerState();
    }

    public void ReachedTurnPoint()
    {
        if (waitingForTurnChoice)
        {
            Debug.Log("Snake: ReachedTurnPoint called, but already waiting for turn choice. Ignored.");
            return;
        }

        if (isMoving || (currentDiceSteps > 0 && !waitingForTurnChoice))
        {
            Debug.Log("Snake: Reached Turn Point. CurrentDiceSteps: " + currentDiceSteps);
            waitingForTurnChoice = true;
            isMoving = false;

            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
                Debug.Log("Snake: MoveCoroutine stopped by ReachedTurnPoint.");
            }

            stepsRemainingAfterTurn = currentDiceSteps;

            if (turnChoiceUI != null)
            {
                turnChoiceUI.SetActive(true);
                Debug.Log("Snake: TurnChoiceUI activated.");
            }
            UpdateButtonRollDiceVisibility();
        }
        else
        {
            Debug.LogWarning("Snake: Reached Turn Point but not in a state to show turn UI.");
        }
    }

    public void HandleTurnChoice(bool turnLeft)
    {
        if (!waitingForTurnChoice)
        {
            Debug.LogWarning("Snake: HandleTurnChoice called, but not waiting for a choice.");
            return;
        }
        if (turnChoiceUI != null) turnChoiceUI.SetActive(false);

        Debug.Log("Snake: HandleTurnChoice. Turn Left: " + turnLeft + ". Steps to continue: " + stepsRemainingAfterTurn);

        float rotationYAmount = turnLeft ? -90f : 90f;
        StartCoroutine(RotateCoroutine(rotationYAmount, () => {
            waitingForTurnChoice = false;

            if (stepsRemainingAfterTurn > 0)
            {
                currentDiceSteps = stepsRemainingAfterTurn;
                UpdateMovesValueUIText(currentDiceSteps);
                Debug.Log("Snake: Continuing movement for " + currentDiceSteps + " steps after turn.");
                StartMoving(currentDiceSteps);
            }
            else
            {
                Debug.Log("Snake: No steps remaining after turn choice.");
                OnMovementFinished();
            }
            stepsRemainingAfterTurn = 0;
        }));
    }

    IEnumerator RotateCoroutine(float angleY, System.Action onRotationComplete)
    {
        isMoving = true;
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
        isMoving = false;
        onRotationComplete?.Invoke();
    }

    void UpdateButtonRollDiceVisibility()
    {
        if (buttonRollDice != null)
        {
            bool canRoll = !isMoving && !waitingForTurnChoice && currentDiceSteps <= 0;
            buttonRollDice.SetActive(canRoll);
        }
    }

    public bool IsMoving()
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
        if (isMoving || waitingForTurnChoice)
        {
            return;
        }

        Debug.Log("Snake: OnTriggerEnter with " + other.name);

        if (other.CompareTag("TurnPointTrigger"))
        {
            Debug.Log("Snake: Hit a TurnPointTrigger directly.");
            ReachedTurnPoint();
        }
        else if (other.TryGetComponent(out Eat eatScript))
        {
            Debug.Log("Snake: Entered Eat trigger.");
        }
        else if (other.TryGetComponent(out Vopros voprosScript))
        {
            Debug.Log("Snake: Entered Vopros trigger. Saving state and loading Vopros scene.");
            SavePlayerState();
            SceneManager.LoadScene("Vopros");
        }
    }
}