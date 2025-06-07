using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public GameObject documentsButton;  // кнопка "Пакет документов"
    public GameObject throwDiceButton;  // кнопка "Кинь кубик"

    // Флаг для отслеживания состояния видимости кнопки "Кинь кубик"
    private bool isThrowDiceButtonVisible = true;

    public void OnDocumentsButtonClicked()
    {
        // Переключаем видимость кнопки "Кинь кубик"
        isThrowDiceButtonVisible = !isThrowDiceButtonVisible;
        throwDiceButton.SetActive(isThrowDiceButtonVisible);

        // Дополнительно можно тут добавить логику переключения камеры и т.п.
    }
}