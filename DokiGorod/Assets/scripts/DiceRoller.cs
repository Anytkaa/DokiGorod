// GameController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections; // Обязательно для корутин
using UnityEngine.SceneManagement; // Обязательно для переключения сцен

public class GameController : MonoBehaviour
{
    public GameObject Dice;
    public Text DiceNumberText;
    public float rollForceMin = 5f;
    public float rollForceMax = 15f;
    public float rollTorqueMin = 20f;
    public float rollTorqueMax = 50f;
    public float upwardForceFactor = 0.75f;

    public string mainSceneName = "MainScene"; // Имя вашей основной сцены
    public float delayBeforeReturn = 1.0f; // Задержка в секундах перед возвратом

    private Rigidbody diceRigidbody;
    private DiceFaceData[] faces;
    private bool isRolling = false;
    private int lastRolledValue = 0;
    private bool resultShown = false; // Флаг, чтобы корутина не запускалась много раз

    void Start()
    {
        if (Dice == null)
        {
            Debug.LogError("Dice Root Object не назначен в GameController!");
            enabled = false;
            return;
        }

        diceRigidbody = Dice.GetComponent<Rigidbody>();
        if (diceRigidbody == null)
        {
            Debug.LogError("На Dice Root Object отсутствует компонент Rigidbody!");
            enabled = false;
            return;
        }

        // --- ДОБАВЛЕНО: Делаем Rigidbody кинематическим при старте ---
        if (diceRigidbody != null)
        {
            diceRigidbody.isKinematic = true; // Кубик будет "висеть" до первого броска
        }
        // --- КОНЕЦ ДОБАВЛЕНИЯ ---

        faces = Dice.GetComponentsInChildren<DiceFaceData>();
        if (faces.Length != 6)
        {
            Debug.LogWarning("Найдено " + faces.Length + " граней с DieFaceData. Ожидалось 6.");
        }

        if (DiceNumberText == null)
        {
            Debug.LogError("Dice Number Text Output не назначен в GameController!");
        }
        resultShown = false;
    }

    void Update()
    {
        if (resultShown) return;

        if (!isRolling && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            RollDice();
        }

        if (isRolling)
        {
            if (diceRigidbody.IsSleeping())
            {
                isRolling = false;
                DetermineTopFace();
            }
        }
    }

    void RollDice()
    {
        // --- ДОБАВЛЕНО: Отключаем кинематический режим перед броском ---
        if (diceRigidbody.isKinematic)
        {
            diceRigidbody.isKinematic = false; // Теперь кубик будет подчиняться физике
        }
        // --- КОНЕЦ ДОБАВЛЕНИЯ ---

        isRolling = true;
        lastRolledValue = 0;
        resultShown = false;

        diceRigidbody.WakeUp();
        diceRigidbody.velocity = Vector3.zero;
        diceRigidbody.angularVelocity = Vector3.zero;

        float forceMagnitude = Random.Range(rollForceMin, rollForceMax);
        Vector3 horizontalForceDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        if (horizontalForceDirection.sqrMagnitude > 0.001f)
        {
            horizontalForceDirection.Normalize();
        }
        else
        {
            horizontalForceDirection = Vector3.forward;
        }
        Vector3 finalForceDirection = (horizontalForceDirection + Vector3.up * upwardForceFactor).normalized;
        diceRigidbody.AddForce(finalForceDirection * forceMagnitude, ForceMode.Impulse);
        float torqueMagnitude = Random.Range(rollTorqueMin, rollTorqueMax);
        diceRigidbody.AddTorque(Random.insideUnitSphere * torqueMagnitude, ForceMode.Impulse);
    }

    void DetermineTopFace()
    {
        if (faces.Length == 0 || resultShown) return;

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
            if (DiceNumberText != null)
            {
                DiceNumberText.text = lastRolledValue.ToString();
            }
            ExecuteRollFunction(lastRolledValue);

            resultShown = true;
            StartCoroutine(ReturnToMainSceneAfterDelay(delayBeforeReturn));
        }
        else
        {
            if (DiceNumberText != null)
            {
                DiceNumberText.text = "Ошибка!";
            }
            Debug.LogError("Не удалось определить верхнюю грань!");
        }
    }

    IEnumerator ReturnToMainSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (string.IsNullOrEmpty(mainSceneName))
        {
            Debug.LogError("Имя MainScene не указано в GameController!");
            yield break;
        }
        SceneManager.LoadScene(mainSceneName);
    }


    void ExecuteRollFunction(int rolledValue)
    {
        Debug.Log("Выпало: " + rolledValue);
    }

    public int GetLastRolledValue()
    {
        return lastRolledValue;
    }
}