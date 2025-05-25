// GameController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [Header("Объекты Кубика")]
    public GameObject DiceRootObject; // Ваш главный объект кубика с Rigidbody
    public Text DiceNumberTextOutput; // UI Text для отображения результата броска

    [Header("Настройки Броска")]
    public float rollForceMin = 5f;
    public float rollForceMax = 15f;
    public float rollTorqueMin = 20f;
    public float rollTorqueMax = 50f;
    public float upwardForceFactor = 0.75f;

    [Header("Связь с Персонажем")]
    public snake playerSnakeScript; // ССЫЛКА НА СКРИПT SNAKE ВАШЕГО ПЕРСОНАЖА

    [Header("Настройки Сцен")]
    public string mainSceneName = "MainScene"; // Имя вашей основной сцены (где происходит бросок)
    public string diceRollSceneName = "Dice"; // Имя сцены, ЕСЛИ бросок происходит в отдельной сцене
                                              // Если бросок в той же сцене, что и игра, эта переменная не так важна
    public float delayBeforeReturnToGame = 1.0f; // Задержка после показа результата кубика (если это отдельная сцена)


    private Rigidbody diceRigidbody;
    private DiceFaceData[] faces;
    private bool isRolling = false;
    private int lastRolledValue = 0;
    private bool resultShownAndProcessed = false;

    void Start()
    {
        if (DiceRootObject == null)
        {
            Debug.LogError("DiceRootObject не назначен в GameController!");
            enabled = false; return;
        }

        diceRigidbody = DiceRootObject.GetComponent<Rigidbody>();
        if (diceRigidbody == null)
        {
            Debug.LogError("На DiceRootObject отсутствует компонент Rigidbody!");
            enabled = false; return;
        }

        // Если кубик должен "висеть" до броска
        diceRigidbody.isKinematic = true;

        faces = DiceRootObject.GetComponentsInChildren<DiceFaceData>();
        if (faces.Length != 6)
        {
            Debug.LogWarning("Найдено " + faces.Length + " граней с DieFaceData. Ожидалось 6.");
        }

        if (DiceNumberTextOutput != null) DiceNumberTextOutput.text = ""; // Очищаем текст при старте
        if (playerSnakeScript == null) Debug.LogError("PlayerSnakeScript не назначен в GameController! Персонаж не сможет двигаться.");

        resultShownAndProcessed = false;
    }

    void Update()
    {
        // Если результат уже обработан и мы, возможно, в отдельной сцене броска, не делаем ничего
        if (resultShownAndProcessed && SceneManager.GetActiveScene().name == diceRollSceneName) return;


        // Обработка ввода для начала броска (например, из UI кнопки или клавиши)
        // Эту логику можно перенести в метод, вызываемый UI кнопкой "Кинь кубик"
        // на основной игровой сцене, если кубик не в отдельной сцене.
        // Для примера оставим обработку клавиш, если этот скрипт на сцене Dice
        if (!isRolling && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            // Проверяем, что персонаж не двигается, прежде чем позволить бросить кубик снова
            if (playerSnakeScript != null && !playerSnakeScript.IsCurrentlyExecutingMovement()) // Предполагается, что в snake.cs есть метод IsMoving()
            {
                InitiateRoll();
            }
            else if (playerSnakeScript == null) // Если скрипт игрока не назначен, все равно бросаем
            {
                InitiateRoll();
            }
            else
            {
                Debug.Log("GameController: Нельзя бросить кубик, персонаж еще двигается.");
            }
        }


        if (isRolling)
        {
            if (diceRigidbody.IsSleeping())
            {
                isRolling = false;
                DetermineTopFaceAndProceed();
            }
        }
    }

    // Этот метод теперь можно вызывать из UI кнопки "Кинь кубик" на основной игровой сцене
    public void InitiateRoll()
    {
        if (isRolling) return; // Не бросать, если уже крутится

        // Если кубик должен "висеть" до броска
        if (diceRigidbody.isKinematic)
        {
            diceRigidbody.isKinematic = false;
        }

        isRolling = true;
        lastRolledValue = 0;
        resultShownAndProcessed = false;


        diceRigidbody.WakeUp();
        diceRigidbody.velocity = Vector3.zero;
        diceRigidbody.angularVelocity = Vector3.zero;

        float forceMagnitude = Random.Range(rollForceMin, rollForceMax);
        Vector3 horizontalForceDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        if (horizontalForceDirection.sqrMagnitude > 0.001f) { horizontalForceDirection.Normalize(); }
        else { horizontalForceDirection = Vector3.forward; }
        Vector3 finalForceDirection = (horizontalForceDirection + Vector3.up * upwardForceFactor).normalized;
        diceRigidbody.AddForce(finalForceDirection * forceMagnitude, ForceMode.Impulse);
        float torqueMagnitude = Random.Range(rollTorqueMin, rollTorqueMax);
        diceRigidbody.AddTorque(Random.insideUnitSphere * torqueMagnitude, ForceMode.Impulse);
    }

    void DetermineTopFaceAndProceed()
    {
        if (faces.Length == 0 || resultShownAndProcessed) return;

        DiceFaceData topFace = null;
        float highestYPoint = -Mathf.Infinity;

        foreach (DiceFaceData face in faces)
        {
            float dotProduct = Vector3.Dot(face.transform.up, Vector3.up);
            if (dotProduct > highestYPoint)
            {
                highestYPoint = dotProduct;
                topFace = face;
            }
        }

        if (topFace != null)
        {
            lastRolledValue = topFace.faceValue;
            if (DiceNumberTextOutput != null) DiceNumberTextOutput.text = lastRolledValue.ToString();
            Debug.Log("GameController: Выпало: " + lastRolledValue);

            // Сохраняем результат для MainScene
            PlayerPrefs.SetInt("LastDiceRoll", lastRolledValue);
            PlayerPrefs.Save();
            Debug.Log("Dice roll result saved to PlayerPrefs: " + lastRolledValue);

            resultShownAndProcessed = true;
            if (!string.IsNullOrEmpty(mainSceneName)) // mainSceneName здесь - это ваша игровая сцена
            {
                StartCoroutine(ReturnToGameSceneAfterDelay(delayBeforeReturnToGame)); // Возвращаемся в игровую сцену
            }
        }
    }

    IEnumerator ReturnToGameSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(mainSceneName);
    }

    // ExecuteRollFunction больше не нужен здесь, логика хода у персонажа
}

// Не забудьте добавить в snake.cs метод:
// public bool IsMoving() { return isMoving || waitingForTurnChoice; }