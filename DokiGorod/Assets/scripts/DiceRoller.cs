// GameController.cs
using UnityEngine;
using UnityEngine.UI; // ���������� ��� ������ � UI Text (Legacy)
using System.Collections; // ��� �������, ���� ����������� (���� �� ����������)

public class GameController : MonoBehaviour
{
    public GameObject Dice; // ���������� ���� ��� ������� ������ "Dice" �� ��������
    public Text DiceNumberText; // ���������� ���� ��� ������ "DiceNumberText" �� ��������
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
            Debug.LogError("Dice Root Object �� �������� � GameController!");
            enabled = false; // ��������� ������, ���� ��� ������
            return;
        }

        diceRigidbody = Dice.GetComponent<Rigidbody>();
        if (diceRigidbody == null)
        {
            Debug.LogError("�� Dice Root Object ����������� ��������� Rigidbody!");
            enabled = false;
            return;
        }

        if (diceRigidbody != null)
        {
            diceRigidbody.isKinematic = true; // ������ ��� ��������������, ����� �� ����� ��� �����������
                                              // ��� diceRigidbody.useGravity = false; // ����� ��������� ����������
        }

        // ������� ��� ���������� DieFaceData �� �������� �������� diceRootObject
        faces = Dice.GetComponentsInChildren<DiceFaceData>();
        if (faces.Length != 6) // ������� ��������, ��� � ��� 6 ������
        {
            Debug.LogWarning("������� " + faces.Length + " ������ � DieFaceData. ��������� 6.");
        }

        if (DiceNumberText == null)
        {
            Debug.LogError("Dice Number Text Output �� �������� � GameController!");
            // ����� �� ��������� ������, �� ����� ����������� �� �����
        }
    }

    void Update()
    {
        // ��������� ����� ��� ������ ������
        if (!isRolling && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            RollDice();
        }

        // ��������, ����������� �� �����
        if (isRolling)
        {
            // Rigidbody.IsSleeping() ������ �������� ��� ����������� ������ ���������
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
        if (diceRigidbody.isKinematic) // ���� ��� ��������������
        {
            diceRigidbody.isKinematic = false; // �������� ������� ������
                                               // if (!diceRigidbody.useGravity) diceRigidbody.useGravity = true; // ���� ��������� ����������
        }

        isRolling = true;
        lastRolledValue = 0; // ���������� ���������� ��������

        // "���������" Rigidbody, ���� �� ����
        diceRigidbody.WakeUp();

        // �������� ���������� ���� � �������� ��� ������� ������
        diceRigidbody.velocity = Vector3.zero;
        diceRigidbody.angularVelocity = Vector3.zero;

        float forceMagnitude = Random.Range(rollForceMin, rollForceMax);

        // 1. ���������� ��������� ����������� � �������������� ���������
        Vector3 horizontalForceDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        // �����������, ����� �������� ������ �����������, ���� ������ �� �������
        if (horizontalForceDirection.sqrMagnitude > 0.001f) // ��������, ����� �������� ������� �� ����, ���� ��� Random.Range ���� 0
        {
            horizontalForceDirection.Normalize();
        }
        else
        {
            horizontalForceDirection = Vector3.forward; // �������� �������, ���� �������� �������������� ������� ������
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
        float highestYPoint = -Mathf.Infinity; // ��� ����������� ����� ������� ����� �����

        foreach (DiceFaceData face in faces)
        {
            // ���������� face.transform.up, ��� ��� ������������, ��� ��������� ��� Y ����� ���������� ������
            // Dot product � Vector3.up �������, ��������� "�����" ������� �����
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
            ExecuteRollFunction(lastRolledValue); // ����� ������� ��� ���������� ������
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

    void ExecuteRollFunction(int rolledValue)
    {
        // ����� �� ������ �������� ������, ��������� �� ��������� �����
        switch (rolledValue)
        {
            case 1:
                Debug.Log("����������� �������� ��� 1");
                break;
            case 6:
                Debug.Log("����������� �������� ��� 6");
                // ��������: player.Move(rolledValue);
                break;
            // ... � ��� ����� ��� ������ ��������
            default:
                break;
        }
    }

    // ��������� �����, ����� ������ ��������� �������� �������� (���� ����� �� ������� �������)
    public int GetLastRolledValue()
    {
        return lastRolledValue;
    }
}