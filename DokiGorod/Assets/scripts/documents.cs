using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public GameObject documentsButton;        // Кнопка "Пакет документов"
    public GameObject throwDiceButton;        // Кнопка "Кинь кубик"
    public GameObject DiceNumberTextObject;   // Текст: число на кубике
    public GameObject DiceTextObject;         // Текст: надпись "выпало число"
    public GameObject MoneyTextObject;        // Текст: количество денег
    public GameObject MoneyObject;            // Панель или фон под деньги

    // Флаг для отслеживания состояния видимости элементов
    private bool isUIVisible = true;

    public void OnDocumentsButtonClicked()
    {
        // Переключаем видимость всех указанных UI-элементов
        isUIVisible = !isUIVisible;

        throwDiceButton.SetActive(isUIVisible);
        DiceNumberTextObject.SetActive(isUIVisible);
        DiceTextObject.SetActive(isUIVisible);
        MoneyTextObject.SetActive(isUIVisible);
        MoneyObject.SetActive(isUIVisible);
    }
}

