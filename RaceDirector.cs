using System.Collections.Generic;
using UnityEngine;

public class RaceDirector : MonoBehaviour
{
    [Header("主角設定")]
    public GameObject targetDrone;

    [Header("所有的 Gates (請按順序拖入)")]
    public List<GateIdentity> gates = new List<GateIdentity>();

    [Header("Timer (MapTimeDisplay)")]
    public MapTimeDisplay timer;   // ★拖入你的計時UI物件上的 MapTimeDisplay

    private int currentTargetIndex = 0;

    void Start()
    {
        Debug.Log($"遊戲開始，偵測到 {gates.Count} 個 Gate。");

        for (int i = 0; i < gates.Count; i++)
        {
            if (gates[i] == null) continue;

            gates[i].SetupGate(i, this);

            if (i == 0) gates[i].SetGreen();
            else gates[i].SetRed();

            if (i == 0) gates[i].UpdateLabel("START");
            else if (i == gates.Count - 1) gates[i].UpdateLabel("FINISH");
        }
    }

    public void ReportGatePass(int gateID, GameObject passingObject)
    {
        if (passingObject != targetDrone) return;

        if (gateID == currentTargetIndex)
        {
            Debug.Log($"通過 Checkpoint: {gateID}");

            gates[currentTargetIndex].SetRed();
            currentTargetIndex++;

            if (currentTargetIndex < gates.Count)
            {
                gates[currentTargetIndex].SetGreen();
            }
            else
            {
                Debug.Log("恭喜完賽！");

                // ★完賽時結束計時，並寫入最佳成績
                if (timer != null)
                {
                    timer.FinishRun();
                }
                else
                {
                    Debug.LogWarning("RaceDirector.timer 未指定，無法結束計時/記錄最佳成績！");
                }
            }
        }
    }
}
