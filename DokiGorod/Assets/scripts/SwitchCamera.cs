using UnityEngine;

public class SwitchCamera : MonoBehaviour
{
    // ������ �� ������ � �����
    public Camera camera1;
    public Camera camera2;

    // ���������� ��� ������� ������
    public void OnButtonClick()
    {
        // ������������ ����� ��������
        SwitchActiveCamera();
    }

    // ������� ��� ������������ ����� ��������
    private void SwitchActiveCamera()
    {
        // ���� camera1 �������, ������������ � � ���������� camera2, � ��������
        if (camera1 != null && camera2 != null)
        {
            camera1.enabled = !camera1.enabled;
            camera2.enabled = !camera2.enabled;
        }
    }
}
