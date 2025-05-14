using UnityEngine;

public class SwitchCamera : MonoBehaviour
{
    // Ссылки на камеры в сцене
    public Camera camera1;
    public Camera camera2;

    // Вызывается при нажатии кнопки
    public void OnButtonClick()
    {
        // Переключение между камерами
        SwitchActiveCamera();
    }

    // Функция для переключения между камерами
    private void SwitchActiveCamera()
    {
        // Если camera1 активна, деактивируем её и активируем camera2, и наоборот
        if (camera1 != null && camera2 != null)
        {
            camera1.enabled = !camera1.enabled;
            camera2.enabled = !camera2.enabled;
        }
    }
}
