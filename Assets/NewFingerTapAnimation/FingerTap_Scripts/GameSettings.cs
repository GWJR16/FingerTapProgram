using UnityEngine;

public class GameSettings : MonoBehaviour
{
    void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0; // Disable VSync to let targetFrameRate control FPS
    }
}
