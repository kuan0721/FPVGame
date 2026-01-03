using UnityEngine;

public class CameraSwitchOnInput : MonoBehaviour
{
    [Header("Cameras")]
    public Camera startCamera;   // 第一顆：看無人機與平台
    public Camera fpvCamera;     // 第二顆：掛在無人機上的第一視角

    [Header("Input Axis Names")]
    public string axisThrottle = "Throttle";
    public string axisRoll = "Roll";
    public string axisPitch = "Pitch";
    public string axisYaw = "Yaw";

    [Header("Detection")]
    [Range(0f, 0.3f)] public float deadzone = 0.08f;

    [Tooltip("啟動後延遲檢測秒數，避免第一幀初始化雜訊。")]
    [Range(0f, 3f)] public float startDelay = 0.6f;

    [Tooltip("是否檢測油門。若你油門軸在待機時不是 0，建議仍可開，但會用 baseline 差值判斷。")]
    public bool detectThrottle = true;

    [Tooltip("是否在 Start 時記錄各軸 baseline，之後用與 baseline 的差值判斷。")]
    public bool useBaselineCalibration = true;

    bool switched = false;
    float startTime;

    // baseline
    float bT, bR, bP, bY;

    void Start()
    {
        startTime = Time.time;

        // 初始：Start Camera 開，FPV Camera 關
        SetCameraState(startOn: true);
        Debug.Log($"Start() -> startCamera:{startCamera.name} enabled={startCamera.enabled}, fpvCamera:{fpvCamera.name} enabled={fpvCamera.enabled}");


        // 記錄 baseline（避免控制器微漂移或油門中立不是 0 造成誤判）
        if (useBaselineCalibration)
        {
            bR = SafeGetAxis(axisRoll);
            bP = SafeGetAxis(axisPitch);
            bY = SafeGetAxis(axisYaw);
            bT = SafeGetAxis(axisThrottle);
        }
        else
        {
            bR = bP = bY = bT = 0f;
        }
    }

    void Update()
    {
        if (switched) return;

        // 先略過啟動時間，避免第一幀噪聲直接觸發
        if (Time.time - startTime < startDelay) return;

        if (HasUserInput())
        {
            switched = true;
            SetCameraState(startOn: false);
        }
    }

    bool HasUserInput()
    {
        float r = SafeGetAxis(axisRoll);
        float p = SafeGetAxis(axisPitch);
        float y = SafeGetAxis(axisYaw);

        // 用「相對 baseline 的差」判斷
        if (Mathf.Abs(r - bR) > deadzone) return true;
        if (Mathf.Abs(p - bP) > deadzone) return true;
        if (Mathf.Abs(y - bY) > deadzone) return true;

        if (detectThrottle)
        {
            float t = SafeGetAxis(axisThrottle);

            // 油門也用 baseline 差值判斷，避免待機值不是 0 的設備誤判
            if (Mathf.Abs(t - bT) > deadzone) return true;
        }

        return false;
    }

    float SafeGetAxis(string axis)
    {
        if (string.IsNullOrWhiteSpace(axis)) return 0f;
        try { return Input.GetAxis(axis); }
        catch { return 0f; }
    }

    void SetCameraState(bool startOn)
    {
        // 1) 先把所有相機關掉（避免 fpvCamera3 之類的漏網之魚）
        foreach (var cam in Camera.allCameras)
            cam.enabled = false;

        // 2) 再只開你指定的兩台
        if (startCamera != null) startCamera.enabled = startOn;
        if (fpvCamera != null) fpvCamera.enabled = !startOn;

        // 3) AudioListener 同步（可選，但建議）
        var al1 = startCamera != null ? startCamera.GetComponent<AudioListener>() : null;
        var al2 = fpvCamera != null ? fpvCamera.GetComponent<AudioListener>() : null;
        if (al1 != null) al1.enabled = startOn;
        if (al2 != null) al2.enabled = !startOn;
    }
}
