using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class snake : MonoBehaviour
{
    public GameObject pas14;
    public GameObject pas20;
    public GameObject player;
    public GameObject button;
    Text text;
    public static int diceNumber = 0;
    public static int money = 2000;
    public static int maxon = 1;
    public static int maxim = 1;
    [Range(0, 10), SerializeField] public static float moveSpeed;
    [Range(0, 35), SerializeField] private float _rotateSpeed;

    private float x, y, z, c;

    void Start()
    {
        gameObject.name = "player";
        if (PlayerPrefs.HasKey("PlayerPositionX"))
        {
            float savedRotationY = PlayerPrefs.GetFloat("PlayerRotationY", 0f);
            x = PlayerPrefs.GetFloat("PlayerPositionX");
            y = PlayerPrefs.GetFloat("PlayerPositionY");
            z = PlayerPrefs.GetFloat("PlayerPositionZ");

            transform.position = new Vector3(x + 2, 1, z);
            transform.rotation = Quaternion.Euler(0, savedRotationY, 0);
        }

        UpdateMoveSpeed();
    }

    void Update()
    {
        PlayerPrefs.SetFloat("PlayerRotationY", transform.rotation.eulerAngles.y);
        UpdateMoveSpeed();
        MoveHead(moveSpeed);
        Rotate(_rotateSpeed);
    }

    void UpdateMoveSpeed()
    {
        if (diceNumber > 0)
        {
            button.SetActive(false);
            moveSpeed = 5;
        }
        else if (diceNumber <= 0 && maxim == 1)
        {
            button.SetActive(true);
            moveSpeed = 0;
        }
        else if (diceNumber <= 0)
        {
            moveSpeed = 0;
        }

        if (polpas14.pas14 == true)
        {
            pas14.SetActive(true);
        }
        if (polpas20.pas20 == true)
        {
            pas20.SetActive(true);
        }
    }

    void MoveHead(float speed)
    {
        transform.position = transform.position + transform.right * speed * Time.deltaTime;
    }

    void Rotate(float speed)
    {
        float angle = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
        transform.Rotate(0, angle, 0);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Eat eat))
        {
            snake.diceNumber -= 1;
            moveSpeed += 2;
        }
        else if (other.TryGetComponent(out Vopros vopros))
        {
            if (snake.diceNumber == 1)
            {
                PlayerPrefs.SetFloat("PlayerRotationY", transform.rotation.eulerAngles.y);
                PlayerPrefs.SetFloat("PlayerPositionX", transform.position.x);
                PlayerPrefs.SetFloat("PlayerPositionY", transform.position.y);
                PlayerPrefs.SetFloat("PlayerPositionZ", transform.position.z);
                SceneManager.LoadScene("Vopros");
            }
        }
    }
}
