// RotateCheck.cs
using UnityEngine;

public class RotateCheck : MonoBehaviour
{
    public MeshRenderer visualConfirmationRenderer; // Перетащите сюда MeshRenderer объекта развилки или другого объекта
    public Color triggerEnterColor = Color.red;
    private Color originalColor;

    void Start()
    {
        if (visualConfirmationRenderer != null)
        {
            // Лучше создавать новый экземпляр материала, чтобы не менять общий ассет материала
            // если этот же материал используется другими объектами, которые не должны менять цвет.
            // Если это уникальный материал только для этого объекта, то можно и visualConfirmationRenderer.material.color
            Material newMaterialInstance = new Material(visualConfirmationRenderer.material);
            visualConfirmationRenderer.material = newMaterialInstance;
            originalColor = visualConfirmationRenderer.material.color;
        }
        else
        {
            Debug.LogWarning("RotateCheck: visualConfirmationRenderer не назначен!", this.gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("RotateCheck: OnTriggerEnter - Объект '" + other.gameObject.name + "' вошел в триггер.");

        if (other.TryGetComponent(out snake playerSnake))
        {
            Debug.Log("RotateCheck: Персонаж Snake вошел в триггер. Вызываем ReachedTurnPoint.");
            if (visualConfirmationRenderer != null)
            {
                visualConfirmationRenderer.material.color = triggerEnterColor;
            }
            // ИСПРАВЛЕНИЕ ЗДЕСЬ: убираем аргумент '0'
            playerSnake.ReachedTurnPoint();
        }
        else
        {
            Debug.Log("RotateCheck: Объект '" + other.gameObject.name + "' вошел в триггер, но это не Snake.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (visualConfirmationRenderer != null && other.TryGetComponent(out snake playerSnakeOnExit))
        {
            visualConfirmationRenderer.material.color = originalColor;
            Debug.Log("RotateCheck: OnTriggerExit - Персонаж Snake покинул триггер.");
        }
    }
}