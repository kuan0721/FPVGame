using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FPVMotorSound : MonoBehaviour
{
    [Header("Throttle Axis")]
    public string throttleAxis = "Throttle";

    [Header("Audio Clip")]
    public AudioClip motorClip;

    [Header("Volume")]
    [Range(0f, 1f)] public float minVolume = 0.25f;
    [Range(0f, 1f)] public float maxVolume = 1.0f;

    [Header("Pitch")]
    public float minPitch = 0.9f;
    public float maxPitch = 1.9f;

    [Header("Smoothing")]
    public float smoothSpeed = 6f;

    [Header("Loop Settings")]
    [Tooltip("避免播放到結尾的安全緩衝（秒）")]
    public float loopSafetyTime = 0.05f;

    private AudioSource audioSource;
    private float currentVolume;
    private float currentPitch;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.clip = motorClip;
        audioSource.loop = false;              // ★ 關掉內建 loop
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f;       // 3D 音效
        audioSource.dopplerLevel = 0f;          // 穿越機不需要 Doppler

        currentVolume = minVolume;
        currentPitch = minPitch;
    }

    void Start()
    {
        if (motorClip != null)
        {
            audioSource.time = 0.1f; // 避開 MP3 頭部延遲
            audioSource.Play();
        }
    }

    void Update()
    {
        if (motorClip == null) return;

        // === 1. 讀取油門 ===
        float throttle = SafeGetAxis(throttleAxis);
        throttle = Mathf.Clamp01(throttle);

        // === 2. 計算目標音效參數 ===
        float targetVolume = Mathf.Lerp(minVolume, maxVolume, throttle);
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, throttle);

        // === 3. 平滑處理 ===
        currentVolume = Mathf.Lerp(currentVolume, targetVolume, Time.deltaTime * smoothSpeed);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * smoothSpeed);

        audioSource.volume = currentVolume;
        audioSource.pitch = currentPitch;

        // === 4. 手動無縫循環 ===
        float clipLength = motorClip.length;

        if (audioSource.time >= clipLength - loopSafetyTime)
        {
            // 回到前段「穩定區」，避免尾端破音
            audioSource.time = 0.1f;
        }
    }

    float SafeGetAxis(string axis)
    {
        if (string.IsNullOrWhiteSpace(axis)) return 0f;
        try { return Input.GetAxis(axis); }
        catch { return 0f; }
    }
}
