using UnityEngine;

public class CameraToggleC : MonoBehaviour
{
    [Header("Assign Cameras Here")]
    public Camera sceneCamera;   // 無人機第一人稱相機
    public Camera droneCamera;   // 無人機第三人稱相機

    [Header("Assign Drone Root (has Rigidbody)")]
    public Transform droneRoot;  
    public Vector3 restartPosition = new Vector3(0f, 5f, -1f);
    public Vector3 restartEulerAngles = Vector3.zero;

    [Header("Keys")]
    public KeyCode toggleKey = KeyCode.C;
    public KeyCode restartKey = KeyCode.R;

    [Header("Start View")]
    public bool startWithDroneView = false;

    void Start()
    {
        ApplyView(startWithDroneView);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            bool nextIsDrone = !IsDroneActive();
            ApplyView(nextIsDrone);
        }

        if (Input.GetKeyDown(restartKey))
        {
            RestartDrone();
        }
    }

    bool IsDroneActive()
    {
        return droneCamera != null && droneCamera.enabled;
    }

    void ApplyView(bool useDroneView)
    {
        if (sceneCamera != null)
        {
            sceneCamera.enabled = !useDroneView;
            if (sceneCamera.TryGetComponent<AudioListener>(out var al0)) al0.enabled = !useDroneView;
        }

        if (droneCamera != null)
        {
            droneCamera.enabled = useDroneView;
            if (droneCamera.TryGetComponent<AudioListener>(out var al1)) al1.enabled = useDroneView;
        }
    }

    void RestartDrone()
    {
        if (droneRoot == null) return;

        // 如果有 Rigidbody，重置物理狀態，避免瞬移後還帶著速度/角速度
        Rigidbody rb = droneRoot.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // 用 MovePosition/MoveRotation 會比較符合物理流程
            rb.MovePosition(restartPosition);
            rb.MoveRotation(Quaternion.Euler(restartEulerAngles));
        }
        else
        {
            droneRoot.position = restartPosition;
            droneRoot.rotation = Quaternion.Euler(restartEulerAngles);
        }
    }
}
