// GameController.cs
using UnityEngine;
using UnityEngine.UI; // Необходимо для работы с UI Text (Legacy)
using System.Collections; // Для корутин, если понадобятся (пока не используем)

public class GameController : MonoBehaviour
{
    public GameObject Dice; // Перетащите сюда ваш главный объект "Dice" из иерархии
    public Text DiceNumberText; // Перетащите сюда ваш объект "DiceNumberText" из иерархии
    public float rollForceMin = 5f;
    public float rollForceMax = 15f;
    public float rollTorqueMin = 20f;
    public float rollTorqueMax = 50f;

    private Rigidbody diceRigidbody;
    private DiceFaceData[] faces;
    private bool isRolling = false;
    private int lastRolledValue = 0;

    void Start()
    {
        if (Dice == null)
        {
            Debug.LogError("Dice Root Object не назначен в GameController!");
            enabled = false; // Выключаем скрипт, если нет кубика
            return;
        }

        diceRigidbody = Dice.GetComponent<Rigidbody>();
        if (diceRigidbody == null)
        {
            Debug.LogError("На Dice Root Object отсутствует компонент Rigidbody!");
            enabled = false;
            return;
        }

        if (diceRigidbody != null)
        {
            diceRigidbody.isKinematic = true; // Делаем его кинематическим, чтобы не падал под гравитацией
                                              // или diceRigidbody.useGravity = false; // Можно отключить гравитацию
        }

        // Находим все компоненты DieFaceData на дочерних объектах diceRootObject
        faces = Dice.GetComponentsInChildren<DiceFaceData>();
        if (faces.Length != 6) // Простая проверка, что у нас 6 граней
        {
            Debug.LogWarning("Найдено " + faces.Length + " граней с DieFaceData. Ожидалось 6.");
        }

        if (DiceNumberText == null)
        {
            Debug.LogError("Dice Number Text Output не назначен в GameController!");
            // Можно не выключать скрипт, но текст обновляться не будет
        }
    }

    void Update()
    {
        // Обработка ввода для начала броска
        if (!isRolling && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            RollDice();
        }

        // Проверка, остановился ли кубик
        if (isRolling)
        {
            // Rigidbody.IsSleeping() хорошо подходит для определения полной остановки
            if (diceRigidbody.IsSleeping())
            {
                isRolling = false;
                DetermineTopFace();
            }
        }
    }

    public float upwardForceFactor = 0.75f;

    void RollDice()
    {
        if (diceRigidbody.isKinematic) // Если был кинематическим
        {
            diceRigidbody.isKinematic = false; // Включаем обратно физику
                                               // if (!diceRigidbody.useGravity) diceRigidbody.useGravity = true; // Если отключали гравитацию
        }

        isRolling = true;
        lastRolledValue = 0; // Сбрасываем предыдущее значение

        // "Разбудить" Rigidbody, если он спал
        diceRigidbody.WakeUp();

        // Обнуляем предыдущие силы и вращения для чистого броска
        diceRigidbody.velocity = Vector3.zero;
        diceRigidbody.angularVelocity = Vector3.zero;

        float forceMagnitude = Random.Range(rollForceMin, rollForceMax);

        // 1. Генерируем случайное направление в горизонтальной плоскости
        Vector3 horizontalForceDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        // Нормализуем, чтобы получить только направление, если вектор не нулевой
        if (horizontalForceDirection.sqrMagnitude > 0.001f) // Проверка, чтобы избежать деления на ноль, если оба Random.Range дали 0
        {
            horizontalForceDirection.Normalize();
        }
        else
        {
            horizontalForceDirection = Vector3.forward; // Запасной вариант, если случайно сгенерировался нулевой вектор
        }
        Vector3 finalForceDirection = (horizontalForceDirection + Vector3.up * upwardForceFactor).normalized;
        diceRigidbody.AddForce(finalForceDirection * forceMagnitude, ForceMode.Impulse);
        float torqueMagnitude = Random.Range(rollTorqueMin, rollTorqueMax);
        diceRigidbody.AddTorque(Random.insideUnitSphere * torqueMagnitude, ForceMode.Impulse);
    }

    void DetermineTopFace()
    {
        if (faces.Length == 0) return;

        DiceFaceData topFace = null;
        float highestYPoint = -Mathf.Infinity; // Для определения самой высокой точки грани

        foreach (DiceFaceData face in faces)
        {
            // Используем face.transform.up, так как предполагаем, что локальная ось Y грани направлена наружу
            // Dot product с Vector3.up покажет, насколько "вверх" смотрит грань
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
            ExecuteRollFunction(lastRolledValue); // Вызов функции для дальнейшей логики
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

    void ExecuteRollFunction(int rolledValue)
    {
        // Здесь вы можете добавить логику, зависящую от выпавшего числа
        switch (rolledValue)
        {
            case 1:
                Debug.Log("Выполняется действие для 1");
                break;
            case 6:
                Debug.Log("Выполняется действие для 6");
                // Например: player.Move(rolledValue);
                break;
            // ... и так далее для других значений
            default:
                break;
        }
    }

    // Публичный метод, чтобы узнать последнее выпавшее значение (если нужно из другого скрипта)
    public int GetLastRolledValue()
    {
        return lastRolledValue;
    }
}