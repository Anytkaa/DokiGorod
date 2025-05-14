using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateCheck : MonoBehaviour
{
    public GameObject buttonl;
    public GameObject buttonr;
    public GameObject buttond;
    public static int nummm = 0;

    // Start is called before the first frame update
    void OnTriggerEnter(Collider other)
    {
        snake.maxim = 2;
        nummm = snake.diceNumber;
        snake.moveSpeed = 0;
        snake.diceNumber = 1;
        buttonl.SetActive(true);
        buttonr.SetActive(true);

        // ����������� ����� ������ ��� ��������� ����� �������

    }
}