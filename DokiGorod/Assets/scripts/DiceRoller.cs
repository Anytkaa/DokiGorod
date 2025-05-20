// GameController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections; // ����������� ��� �������
using UnityEngine.SceneManagement; // ����������� ��� ������������ ����

public class GameController : MonoBehaviour
{
    public GameObject Dice;
    public Text DiceNumberText;
    public float rollForceMin = 5f;
    public float rollForceMax = 15f;
    public float rollTorqueMin = 20f;
    public float rollTorqueMax = 50f;
    public float upwardForceFactor = 0.75f;

    public string mainSceneName = "MainScene"; // ��� ����� �������� �����
    public float delayBeforeReturn = 1.0f; // �������� � �������� ����� ���������

    private Rigidbody diceRigidbody;
    private DiceFaceData[] faces;
    private bool isRolling = false;
    private int lastRolledValue = 0;
    private bool resultShown = false; // ����, ����� �������� �� ����������� ����� ���

    void Start()
    {
        if (Dice == null)
        {
            Debug.LogError("Dice Root Object �� �������� � GameController!");
            enabled = false;
            return;
        }

        diceRigidbody = Dice.GetComponent<Rigidbody>();
        if (diceRigidbody == null)
        {
            Debug.LogError("�� Dice Root Object ����������� ��������� Rigidbody!");
            enabled = false;
            return;
        }

        // --- ���������: ������ Rigidbody �������������� ��� ������ ---
        if (diceRigidbody != null)
        {
            diceRigidbody.isKinematic = true; // ����� ����� "������" �� ������� ������
        }
        // --- ����� ���������� ---

        faces = Dice.GetComponentsInChildren<DiceFaceData>();
        if (faces.Length != 6)
        {
            Debug.LogWarning("������� " + faces.Length + " ������ � DieFaceData. ��������� 6.");
        }

        if (DiceNumberText == null)
        {
            Debug.LogError("Dice Number Text Output �� �������� � GameController!");
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
        // --- ���������: ��������� �������������� ����� ����� ������� ---
        if (diceRigidbody.isKinematic)
        {
            diceRigidbody.isKinematic = false; // ������ ����� ����� ����������� ������
        }
        // --- ����� ���������� ---

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
                DiceNumberText.text = "������!";
            }
            Debug.LogError("�� ������� ���������� ������� �����!");
        }
    }

    IEnumerator ReturnToMainSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (string.IsNullOrEmpty(mainSceneName))
        {
            Debug.LogError("��� MainScene �� ������� � GameController!");
            yield break;
        }
        SceneManager.LoadScene(mainSceneName);
    }


    void ExecuteRollFunction(int rolledValue)
    {
        Debug.Log("������: " + rolledValue);
    }

    public int GetLastRolledValue()
    {
        return lastRolledValue;
    }
}