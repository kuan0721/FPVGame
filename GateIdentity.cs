using UnityEngine;
using TMPro;

public class GateIdentity : MonoBehaviour
{
    [Header("材質設定 (必填)")]
    public Material greenMat;
    public Material redMat;

    [Header("文字物件 (選填)")]
    // 中間的 Gate 這裡可以是空的 (None)，只有頭尾需要填
    public TMP_Text gateText;

    [HideInInspector]
    public int myIndex;

    private RaceDirector director;
    private Renderer myRenderer;

    public void SetupGate(int index, RaceDirector dir)
    {
        myIndex = index;
        director = dir;
        myRenderer = GetComponent<Renderer>();

        if (myRenderer == null) Debug.LogError($"Gate {index} 身上沒有 Mesh Renderer，無法變色！");
    }

    public void UpdateLabel(string text)
    {
        if (gateText != null)
        {
            // 確保文字是開啟的 (避免重置遊戲時文字還隱藏著)
            gateText.gameObject.SetActive(true);
            gateText.text = text;
        }
    }

    public void SetGreen()
    {
        if (myRenderer != null && greenMat != null) myRenderer.material = greenMat;
    }

    // ★★★ 修改這裡：變紅燈時，順便隱藏文字 ★★★
    public void SetRed()
    {
        // 1. 變更材質顏色
        if (myRenderer != null && redMat != null) myRenderer.material = redMat;

        // 2. 如果有綁定文字，就把它關掉 (隱藏)
        if (gateText != null)
        {
            gateText.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (director != null)
        {
            director.ReportGatePass(myIndex, other.gameObject);
        }
    }
}