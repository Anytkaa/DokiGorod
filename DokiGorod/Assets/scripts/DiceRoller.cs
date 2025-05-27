// GameController.cs (или DiceRoller.cs - в сцене с кубиком)
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

    // УДАЛЕНО: public snake playerSnakeScript; 

    [Header("Настройки Сцен")]
    public string mainSceneName = "MainScene"; // Имя вашей основной игровой сцены (куда передается результат)
    // diceRollSceneName не нужен, если этот скрипт УЖЕ в сцене броска кубика.
    // Если этот скрипт в MainScene и ПЕРЕХОДИТ в сцену кубика, то diceRollSceneName нужен.
    // Судя по вашей логике, этот скрипт в сцене кубика.
    public float delayBeforeReturnToGame = 1.0f;


    private Rigidbody diceRigidbody;
    private DiceFaceData[] faces; // Убедитесь, что у вас есть скрипт DiceFaceData на гранях кубика
    private bool isRolling = false;
    private int lastRolledValue = 0;
    private bool resultShownAndProcessed = false; // Флаг, чтобы избежать повторной обработки

    void Start()
    {
        if (DiceRootObject == null)
        {
            Debug.LogError("DiceRootObject не назначен в GameController (сцена кубика)!");
            enabled = false; return;
        }

        diceRigidbody = DiceRootObject.GetComponent<Rigidbody>();
        if (diceRigidbody == null)
        {
            Debug.LogError("На DiceRootObject отсутствует компонент Rigidbody! (сцена кубика)");
            enabled = false; return;
        }

        diceRigidbody.isKinematic = true; // Кубик неактивен до броска

        // Предполагается, что у вас есть скрипт DiceFaceData на каждой грани кубика,
        // который хранит значение этой грани. Если нет, эту часть нужно адаптировать.
        faces = DiceRootObject.GetComponentsInChildren<DiceFaceData>();
        if (faces.Length == 0) // Проверка на 0, так как 6 не всегда строго
        {
            Debug.LogWarning("Не найдено граней с компонентом DiceFaceData. Определение результата может не работать.");
        }
        else if (faces.Length != 6)
        {
            Debug.LogWarning("Найдено " + faces.Length + " граней с DieFaceData. Ожидалось 6.");
        }


        if (DiceNumberTextOutput != null) DiceNumberTextOutput.text = "";

        // УДАЛЕНО: Проверка playerSnakeScript, так как его здесь больше нет.
        // if (playerSnakeScript == null) Debug.LogError("PlayerSnakeScript не назначен в GameController! Персонаж не сможет двигаться.");

        resultShownAndProcessed = false;

        // Если этот скрипт в отдельной сцене для броска кубика,
        // можно инициировать бросок сразу при старте этой сцены или по таймеру.
        // Или, если вы переходите на эту сцену по кнопке "Кинь кубик" из MainScene,
        // то бросок должен инициироваться по какому-то условию здесь.
        // Для примера, оставим InitiateRoll() по нажатию, если нужно тестировать эту сцену отдельно.
    }

    void Update()
    {
        // Если результат уже был показан и обработан, ничего не делаем.
        if (resultShownAndProcessed) return;

        // Если этот скрипт в сцене, которая АКТИВИРУЕТСЯ для броска,
        // то InitiateRoll() может вызываться извне или при загрузке сцены.
        // Для автономного теста сцены с кубиком:
        if (!isRolling && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            // УДАЛЕНО: Проверка playerSnakeScript.IsCurrentlyExecutingMovement()
            // Эта сцена не знает о состоянии игрока. Она просто бросает кубик.
            InitiateRoll();
        }

        if (isRolling)
        {
            // Условие IsSleeping() более надежно, чем проверка скорости, для определения остановки
            if (diceRigidbody.IsSleeping() && diceRigidbody.velocity.magnitude < 0.1f && diceRigidbody.angularVelocity.magnitude < 0.1f)
            {
                // Дополнительная проверка, чтобы убедиться, что он действительно остановился
                StartCoroutine(DelayedFaceCheck());
            }
        }
    }

    // Небольшая задержка перед определением грани, чтобы кубик точно "успокоился"
    IEnumerator DelayedFaceCheck()
    {
        if (!isRolling) yield break; // Если уже не катится (например, результат обработан)

        yield return new WaitForSeconds(0.25f); // Маленькая пауза

        // Проверяем еще раз, не покатился ли он снова за эту паузу
        if (diceRigidbody.IsSleeping() && diceRigidbody.velocity.magnitude < 0.1f && diceRigidbody.angularVelocity.magnitude < 0.1f)
        {
            isRolling = false; // Теперь точно останавливаем флаг
            DetermineTopFaceAndProceed();
        }
        else
        {
            // Если он снова начал катиться, ничего не делаем, ждем следующего IsSleeping
            Debug.Log("Dice was sleeping but started moving again slightly during delay.");
        }
    }


    // Этот метод вызывается для начала процесса броска кубика.
    // Если сцена кубика загружается по кнопке из MainScene, то, возможно,
    // вы захотите вызывать InitiateRoll() автоматически при загрузке этой сцены.
    public void InitiateRoll()
    {
        if (isRolling || resultShownAndProcessed) return;

        if (diceRigidbody.isKinematic)
        {
            diceRigidbody.isKinematic = false;
        }

        isRolling = true;
        lastRolledValue = 0;
        // resultShownAndProcessed сбрасывается здесь, если вы хотите позволить многократные броски в этой сцене
        // Но если сцена одноразовая, то можно не сбрасывать.
        // Для логики "один бросок - переход на MainScene", resultShownAndProcessed сбрасывается в Start()

        diceRigidbody.WakeUp(); // Обязательно "будим" Rigidbody
        diceRigidbody.velocity = Vector3.zero;
        diceRigidbody.angularVelocity = Vector3.zero;

        float forceMagnitude = Random.Range(rollForceMin, rollForceMax);
        // Бросаем немного вверх и в случайную горизонтальную сторону
        Vector3 horizontalForceDirection = new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f)).normalized;
        if (horizontalForceDirection == Vector3.zero) horizontalForceDirection = Vector3.forward; // На случай, если обе компоненты 0

        Vector3 finalForceDirection = (horizontalForceDirection + Vector3.up * upwardForceFactor).normalized;
        diceRigidbody.AddForce(finalForceDirection * forceMagnitude, ForceMode.Impulse);

        float torqueMagnitude = Random.Range(rollTorqueMin, rollTorqueMax);
        diceRigidbody.AddTorque(Random.insideUnitSphere.normalized * torqueMagnitude, ForceMode.Impulse);
        Debug.Log("GameController (Dice Scene): Dice Roll Initiated.");
    }

    void DetermineTopFaceAndProceed()
    {
        if (resultShownAndProcessed) return; // Предотвращаем двойную обработку
        if (faces == null || faces.Length == 0)
        {
            Debug.LogError("Нет граней (DiceFaceData) для определения результата!");
            // В качестве заглушки можно вернуть случайное число и перейти
            lastRolledValue = Random.Range(1, 7);
            Debug.LogWarning("Заглушка: выпало (случайно) " + lastRolledValue);
        }
        else
        {
            DiceFaceData topFace = null;
            float highestYPoint = -Mathf.Infinity;

            foreach (DiceFaceData face in faces)
            {
                // Определяем грань, которая смотрит наиболее "вверх"
                // (ее локальная ось Y наиболее сонаправлена с мировой осью Y)
                // Или, если грани это отдельные объекты, то transform.up грани.
                // Зависит от вашей настройки кубика.
                // Этот пример предполагает, что DiceFaceData висит на объекте,
                // чей transform.up указывает наружу от грани.
                float dotProduct = Vector3.Dot(face.transform.up, Vector3.up);
                if (dotProduct > highestYPoint)
                {
                    highestYPoint = dotProduct;
                    topFace = face;
                }
            }
            if (topFace != null)
            {
                lastRolledValue = topFace.faceValue; // Предполагается, что в DiceFaceData есть поле public int faceValue;
            }
            else
            {
                Debug.LogError("Не удалось определить верхнюю грань! Используем случайное значение.");
                lastRolledValue = Random.Range(1, 7);
            }
        }


        if (DiceNumberTextOutput != null) DiceNumberTextOutput.text = lastRolledValue.ToString();
        Debug.Log("GameController (Dice Scene): Выпало: " + lastRolledValue);

        PlayerPrefs.SetInt("LastDiceRoll", lastRolledValue);
        PlayerPrefs.Save();
        Debug.Log("GameController (Dice Scene): Результат броска сохранен в PlayerPrefs: " + lastRolledValue);

        resultShownAndProcessed = true; // Отмечаем, что результат обработан

        if (!string.IsNullOrEmpty(mainSceneName))
        {
            StartCoroutine(ReturnToGameSceneAfterDelay(delayBeforeReturnToGame));
        }
        else
        {
            Debug.LogError("Имя mainSceneName не установлено! Некуда возвращаться.");
        }
    }

    IEnumerator ReturnToGameSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("GameController (Dice Scene): Возвращаемся в сцену " + mainSceneName);
        SceneManager.LoadScene(mainSceneName);
    }
}