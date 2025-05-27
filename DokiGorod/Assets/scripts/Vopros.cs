using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Vopros : MonoBehaviour
{
    public Button[] answerButtons; // Массив кнопок ответов
    public Color correctColor = Color.green; // Цвет для правильного ответа
    public string gameSceneName = "MainScene"; // Название сцены с игрой

    // Start is called before the first frame update
    void Start()
    {
        // Назначаем обработчики для всех кнопок
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i; // Важно создать локальную копию для замыкания
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnAnswerSelected(int answerIndex)
    {
        // Проверяем, правильный ли ответ (индекс 1 - вторая кнопка)
        bool isCorrect = (answerIndex == 1); // Нумерация с 0
        
        if (isCorrect)
        {
            // Подсвечиваем правильный ответ зеленым
            answerButtons[answerIndex].image.color = correctColor;
            
            // Ждем немного и возвращаемся в игру
            Invoke("ReturnToGame", 1f);
        }
        else
        {
            // Можно добавить обработку неправильного ответа (например, подсветить красным)
            answerButtons[answerIndex].image.color = Color.red;
        }
    }

    private void ReturnToGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}
