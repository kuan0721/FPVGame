using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class MapTimeDisplay : MonoBehaviour
{
    [Header("UI (TextMeshProUGUI)")]
    public TMP_Text titleText;
    public TMP_Text timeText;

    [Header("Labels")]
    public string playerName = "Player One";

    [Header("Start Condition")]
    public bool startOnThrottleInput = true;
    public string throttleAxisName = "Throttle";
    [Range(0f, 1f)] public float throttleThreshold = 0.12f; // 門檻，越大越不會誤觸發
    [Tooltip("啟動後先略過這段時間，避免第一幀噪聲。")]
    [Range(0f, 2f)] public float inputWarmupSeconds = 0.5f;

    [Header("Timer")]
    public bool autoStartOnSceneLoad = false; // 改成 false：不進場就開始

    float startTime;
    bool running;

    float sceneEnterTime;
    bool baselineReady;
    float throttleBaseline;

    string MapKey => $"BEST_TIME_{SceneManager.GetActiveScene().name}";

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        sceneEnterTime = Time.time;

        if (titleText != null) titleText.text = playerName;

        // 進場先顯示 00:00.000，不開始跑
        UpdateUI(0f, false);

        // 若你硬要進場自動開始，才開這個
        if (autoStartOnSceneLoad && !startOnThrottleInput)
            StartRun();
    }

    void Update()
    {
        // 尚未開始：等油門觸發
        if (!running && startOnThrottleInput)
        {
            if (Time.time - sceneEnterTime >= inputWarmupSeconds)
            {
                // 第一次 warmup 過後記錄 baseline
                if (!baselineReady)
                {
                    throttleBaseline = SafeGetAxis(throttleAxisName);
                    baselineReady = true;
                }

                float t = SafeGetAxis(throttleAxisName);
                // 用相對 baseline 差值判斷，避免油門中立不是 0 的設備誤判
                if (Mathf.Abs(t - throttleBaseline) > throttleThreshold)
                {
                    StartRun();
                }
            }

            return;
        }

        // 已開始：正常計時
        if (running)
        {
            float t = Time.time - startTime;
            UpdateUI(t, true);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneEnterTime = Time.time;
        baselineReady = false;

        if (autoStartOnSceneLoad && !startOnThrottleInput)
            StartRun();
        else
            UpdateUI(0f, false);
    }

    public void StartRun()
    {
        startTime = Time.time;
        running = true;
    }

    public void FinishRun()
    {
        if (!running) return;

        float finalTime = Time.time - startTime;
        running = false;

        float best = GetBestTime();
        if (best <= 0f || finalTime < best)
        {
            SetBestTime(finalTime);
            best = finalTime;
        }

        UpdateUI(finalTime, false);
    }

    float GetBestTime()
    {
        return PlayerPrefs.GetFloat(MapKey, 0f);
    }

    void SetBestTime(float t)
    {
        PlayerPrefs.SetFloat(MapKey, t);
        PlayerPrefs.Save();
    }

    void UpdateUI(float currentTime, bool isRunning)
    {
        float best = GetBestTime();
        string cur = FormatTime(currentTime);
        string bestStr = best > 0f ? FormatTime(best) : "--:--.---";

        if (timeText != null)
        {
            if (!running && startOnThrottleInput)
            {
                // 尚未開始時的提示
                timeText.text = $"Map Time: 00:00.000\nBest: {bestStr}";
            }
            else
            {
                timeText.text = $"Map Time: {cur}\nBest: {bestStr}";
            }
        }
    }

    string FormatTime(float t)
    {
        int min = Mathf.FloorToInt(t / 60f);
        float sec = t - min * 60f;
        return $"{min:00}:{sec:00.000}";
    }

    float SafeGetAxis(string axis)
    {
        if (string.IsNullOrWhiteSpace(axis)) return 0f;
        try { return Input.GetAxis(axis); }
        catch { return 0f; }
    }
}
