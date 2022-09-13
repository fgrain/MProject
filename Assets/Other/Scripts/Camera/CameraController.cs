using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ECameraMode
{
    Character,
    EnemyTarger,

    Count
}
public class CameraController : MonoBehaviour
{
    public CinemachineVirtualCameraBase MainCharacter;
    public CinemachineVirtualCameraBase EnemyTarget;

    private static CinemachineVirtualCameraBase[] m_CMCams = new CinemachineVirtualCameraBase[(int)ECameraMode.Count];

    public static void SetCameraMode(ECameraMode Mode)
    {
        for (int i = 0; i < m_CMCams.Length; ++i)
        {
            if (i != (int)Mode)
            {
                m_CMCams[i].enabled = false;
            }
        }
        m_CMCams[(int)Mode].enabled = true; 
    }

    private void Start()
    {
        InitCMCameras();
    }
    private void InitCMCameras()
    {
        m_CMCams[(int)ECameraMode.Character] = MainCharacter;
        m_CMCams[(int)ECameraMode.EnemyTarger] = EnemyTarget;
    }
}