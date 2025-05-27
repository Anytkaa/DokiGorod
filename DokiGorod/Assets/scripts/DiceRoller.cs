// GameController.cs (��� DiceRoller.cs - � ����� � �������)
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

    // �������: public snake playerSnakeScript; 

    [Header("��������� ����")]
    public string mainSceneName = "MainScene"; // ��� ����� �������� ������� ����� (���� ���������� ���������)
    // diceRollSceneName �� �����, ���� ���� ������ ��� � ����� ������ ������.
    // ���� ���� ������ � MainScene � ��������� � ����� ������, �� diceRollSceneName �����.
    // ���� �� ����� ������, ���� ������ � ����� ������.
    public float delayBeforeReturnToGame = 1.0f;


    private Rigidbody diceRigidbody;
    private DiceFaceData[] faces; // ���������, ��� � ��� ���� ������ DiceFaceData �� ������ ������
    private bool isRolling = false;
    private int lastRolledValue = 0;
    private bool resultShownAndProcessed = false; // ����, ����� �������� ��������� ���������

    void Start()
    {
        if (DiceRootObject == null)
        {
            Debug.LogError("DiceRootObject �� �������� � GameController (����� ������)!");
            enabled = false; return;
        }

        diceRigidbody = DiceRootObject.GetComponent<Rigidbody>();
        if (diceRigidbody == null)
        {
            Debug.LogError("�� DiceRootObject ����������� ��������� Rigidbody! (����� ������)");
            enabled = false; return;
        }

        diceRigidbody.isKinematic = true; // ����� ��������� �� ������

        // ��������������, ��� � ��� ���� ������ DiceFaceData �� ������ ����� ������,
        // ������� ������ �������� ���� �����. ���� ���, ��� ����� ����� ������������.
        faces = DiceRootObject.GetComponentsInChildren<DiceFaceData>();
        if (faces.Length == 0) // �������� �� 0, ��� ��� 6 �� ������ ������
        {
            Debug.LogWarning("�� ������� ������ � ����������� DiceFaceData. ����������� ���������� ����� �� ��������.");
        }
        else if (faces.Length != 6)
        {
            Debug.LogWarning("������� " + faces.Length + " ������ � DieFaceData. ��������� 6.");
        }


        if (DiceNumberTextOutput != null) DiceNumberTextOutput.text = "";

        // �������: �������� playerSnakeScript, ��� ��� ��� ����� ������ ���.
        // if (playerSnakeScript == null) Debug.LogError("PlayerSnakeScript �� �������� � GameController! �������� �� ������ ���������.");

        resultShownAndProcessed = false;

        // ���� ���� ������ � ��������� ����� ��� ������ ������,
        // ����� ������������ ������ ����� ��� ������ ���� ����� ��� �� �������.
        // ���, ���� �� ���������� �� ��� ����� �� ������ "���� �����" �� MainScene,
        // �� ������ ������ �������������� �� ������-�� ������� �����.
        // ��� �������, ������� InitiateRoll() �� �������, ���� ����� ����������� ��� ����� ��������.
    }

    void Update()
    {
        // ���� ��������� ��� ��� ������� � ���������, ������ �� ������.
        if (resultShownAndProcessed) return;

        // ���� ���� ������ � �����, ������� ������������ ��� ������,
        // �� InitiateRoll() ����� ���������� ����� ��� ��� �������� �����.
        // ��� ����������� ����� ����� � �������:
        if (!isRolling && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            // �������: �������� playerSnakeScript.IsCurrentlyExecutingMovement()
            // ��� ����� �� ����� � ��������� ������. ��� ������ ������� �����.
            InitiateRoll();
        }

        if (isRolling)
        {
            // ������� IsSleeping() ����� �������, ��� �������� ��������, ��� ����������� ���������
            if (diceRigidbody.IsSleeping() && diceRigidbody.velocity.magnitude < 0.1f && diceRigidbody.angularVelocity.magnitude < 0.1f)
            {
                // �������������� ��������, ����� ���������, ��� �� ������������� �����������
                StartCoroutine(DelayedFaceCheck());
            }
        }
    }

    // ��������� �������� ����� ������������ �����, ����� ����� ����� "����������"
    IEnumerator DelayedFaceCheck()
    {
        if (!isRolling) yield break; // ���� ��� �� ������� (��������, ��������� ���������)

        yield return new WaitForSeconds(0.25f); // ��������� �����

        // ��������� ��� ���, �� ��������� �� �� ����� �� ��� �����
        if (diceRigidbody.IsSleeping() && diceRigidbody.velocity.magnitude < 0.1f && diceRigidbody.angularVelocity.magnitude < 0.1f)
        {
            isRolling = false; // ������ ����� ������������� ����
            DetermineTopFaceAndProceed();
        }
        else
        {
            // ���� �� ����� ����� ��������, ������ �� ������, ���� ���������� IsSleeping
            Debug.Log("Dice was sleeping but started moving again slightly during delay.");
        }
    }


    // ���� ����� ���������� ��� ������ �������� ������ ������.
    // ���� ����� ������ ����������� �� ������ �� MainScene, ��, ��������,
    // �� �������� �������� InitiateRoll() ������������� ��� �������� ���� �����.
    public void InitiateRoll()
    {
        if (isRolling || resultShownAndProcessed) return;

        if (diceRigidbody.isKinematic)
        {
            diceRigidbody.isKinematic = false;
        }

        isRolling = true;
        lastRolledValue = 0;
        // resultShownAndProcessed ������������ �����, ���� �� ������ ��������� ������������ ������ � ���� �����
        // �� ���� ����� �����������, �� ����� �� ����������.
        // ��� ������ "���� ������ - ������� �� MainScene", resultShownAndProcessed ������������ � Start()

        diceRigidbody.WakeUp(); // ����������� "�����" Rigidbody
        diceRigidbody.velocity = Vector3.zero;
        diceRigidbody.angularVelocity = Vector3.zero;

        float forceMagnitude = Random.Range(rollForceMin, rollForceMax);
        // ������� ������� ����� � � ��������� �������������� �������
        Vector3 horizontalForceDirection = new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f)).normalized;
        if (horizontalForceDirection == Vector3.zero) horizontalForceDirection = Vector3.forward; // �� ������, ���� ��� ���������� 0

        Vector3 finalForceDirection = (horizontalForceDirection + Vector3.up * upwardForceFactor).normalized;
        diceRigidbody.AddForce(finalForceDirection * forceMagnitude, ForceMode.Impulse);

        float torqueMagnitude = Random.Range(rollTorqueMin, rollTorqueMax);
        diceRigidbody.AddTorque(Random.insideUnitSphere.normalized * torqueMagnitude, ForceMode.Impulse);
        Debug.Log("GameController (Dice Scene): Dice Roll Initiated.");
    }

    void DetermineTopFaceAndProceed()
    {
        if (resultShownAndProcessed) return; // ������������� ������� ���������
        if (faces == null || faces.Length == 0)
        {
            Debug.LogError("��� ������ (DiceFaceData) ��� ����������� ����������!");
            // � �������� �������� ����� ������� ��������� ����� � �������
            lastRolledValue = Random.Range(1, 7);
            Debug.LogWarning("��������: ������ (��������) " + lastRolledValue);
        }
        else
        {
            DiceFaceData topFace = null;
            float highestYPoint = -Mathf.Infinity;

            foreach (DiceFaceData face in faces)
            {
                // ���������� �����, ������� ������� �������� "�����"
                // (�� ��������� ��� Y �������� ������������ � ������� ���� Y)
                // ���, ���� ����� ��� ��������� �������, �� transform.up �����.
                // ������� �� ����� ��������� ������.
                // ���� ������ ������������, ��� DiceFaceData ����� �� �������,
                // ��� transform.up ��������� ������ �� �����.
                float dotProduct = Vector3.Dot(face.transform.up, Vector3.up);
                if (dotProduct > highestYPoint)
                {
                    highestYPoint = dotProduct;
                    topFace = face;
                }
            }
            if (topFace != null)
            {
                lastRolledValue = topFace.faceValue; // ��������������, ��� � DiceFaceData ���� ���� public int faceValue;
            }
            else
            {
                Debug.LogError("�� ������� ���������� ������� �����! ���������� ��������� ��������.");
                lastRolledValue = Random.Range(1, 7);
            }
        }


        if (DiceNumberTextOutput != null) DiceNumberTextOutput.text = lastRolledValue.ToString();
        Debug.Log("GameController (Dice Scene): ������: " + lastRolledValue);

        PlayerPrefs.SetInt("LastDiceRoll", lastRolledValue);
        PlayerPrefs.Save();
        Debug.Log("GameController (Dice Scene): ��������� ������ �������� � PlayerPrefs: " + lastRolledValue);

        resultShownAndProcessed = true; // ��������, ��� ��������� ���������

        if (!string.IsNullOrEmpty(mainSceneName))
        {
            StartCoroutine(ReturnToGameSceneAfterDelay(delayBeforeReturnToGame));
        }
        else
        {
            Debug.LogError("��� mainSceneName �� �����������! ������ ������������.");
        }
    }

    IEnumerator ReturnToGameSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("GameController (Dice Scene): ������������ � ����� " + mainSceneName);
        SceneManager.LoadScene(mainSceneName);
    }
}