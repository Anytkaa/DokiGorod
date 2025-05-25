// GameController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [Header("������� ������")]
    public GameObject DiceRootObject; // ��� ������� ������ ������ � Rigidbody
    public Text DiceNumberTextOutput; // UI Text ��� ����������� ���������� ������

    [Header("��������� ������")]
    public float rollForceMin = 5f;
    public float rollForceMax = 15f;
    public float rollTorqueMin = 20f;
    public float rollTorqueMax = 50f;
    public float upwardForceFactor = 0.75f;

    [Header("����� � ����������")]
    public snake playerSnakeScript; // ������ �� �����T SNAKE ������ ���������

    [Header("��������� ����")]
    public string mainSceneName = "MainScene"; // ��� ����� �������� ����� (��� ���������� ������)
    public string diceRollSceneName = "Dice"; // ��� �����, ���� ������ ���������� � ��������� �����
                                              // ���� ������ � ��� �� �����, ��� � ����, ��� ���������� �� ��� �����
    public float delayBeforeReturnToGame = 1.0f; // �������� ����� ������ ���������� ������ (���� ��� ��������� �����)


    private Rigidbody diceRigidbody;
    private DiceFaceData[] faces;
    private bool isRolling = false;
    private int lastRolledValue = 0;
    private bool resultShownAndProcessed = false;

    void Start()
    {
        if (DiceRootObject == null)
        {
            Debug.LogError("DiceRootObject �� �������� � GameController!");
            enabled = false; return;
        }

        diceRigidbody = DiceRootObject.GetComponent<Rigidbody>();
        if (diceRigidbody == null)
        {
            Debug.LogError("�� DiceRootObject ����������� ��������� Rigidbody!");
            enabled = false; return;
        }

        // ���� ����� ������ "������" �� ������
        diceRigidbody.isKinematic = true;

        faces = DiceRootObject.GetComponentsInChildren<DiceFaceData>();
        if (faces.Length != 6)
        {
            Debug.LogWarning("������� " + faces.Length + " ������ � DieFaceData. ��������� 6.");
        }

        if (DiceNumberTextOutput != null) DiceNumberTextOutput.text = ""; // ������� ����� ��� ������
        if (playerSnakeScript == null) Debug.LogError("PlayerSnakeScript �� �������� � GameController! �������� �� ������ ���������.");

        resultShownAndProcessed = false;
    }

    void Update()
    {
        // ���� ��������� ��� ��������� � ��, ��������, � ��������� ����� ������, �� ������ ������
        if (resultShownAndProcessed && SceneManager.GetActiveScene().name == diceRollSceneName) return;


        // ��������� ����� ��� ������ ������ (��������, �� UI ������ ��� �������)
        // ��� ������ ����� ��������� � �����, ���������� UI ������� "���� �����"
        // �� �������� ������� �����, ���� ����� �� � ��������� �����.
        // ��� ������� ������� ��������� ������, ���� ���� ������ �� ����� Dice
        if (!isRolling && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            // ���������, ��� �������� �� ���������, ������ ��� ��������� ������� ����� �����
            if (playerSnakeScript != null && !playerSnakeScript.IsCurrentlyExecutingMovement()) // ��������������, ��� � snake.cs ���� ����� IsMoving()
            {
                InitiateRoll();
            }
            else if (playerSnakeScript == null) // ���� ������ ������ �� ��������, ��� ����� �������
            {
                InitiateRoll();
            }
            else
            {
                Debug.Log("GameController: ������ ������� �����, �������� ��� ���������.");
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

    // ���� ����� ������ ����� �������� �� UI ������ "���� �����" �� �������� ������� �����
    public void InitiateRoll()
    {
        if (isRolling) return; // �� �������, ���� ��� ��������

        // ���� ����� ������ "������" �� ������
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
            Debug.Log("GameController: ������: " + lastRolledValue);

            // ��������� ��������� ��� MainScene
            PlayerPrefs.SetInt("LastDiceRoll", lastRolledValue);
            PlayerPrefs.Save();
            Debug.Log("Dice roll result saved to PlayerPrefs: " + lastRolledValue);

            resultShownAndProcessed = true;
            if (!string.IsNullOrEmpty(mainSceneName)) // mainSceneName ����� - ��� ���� ������� �����
            {
                StartCoroutine(ReturnToGameSceneAfterDelay(delayBeforeReturnToGame)); // ������������ � ������� �����
            }
        }
    }

    IEnumerator ReturnToGameSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(mainSceneName);
    }

    // ExecuteRollFunction ������ �� ����� �����, ������ ���� � ���������
}

// �� �������� �������� � snake.cs �����:
// public bool IsMoving() { return isMoving || waitingForTurnChoice; }