 # FPVGame
專案簡介

本專案為一套以 Unity 開發的 FPV（First Person View）穿越機模擬器，結合實體穿越機飛行經驗與模擬器操作概念，實作接近真實 ACRO 模式的飛行控制、Gate 穿越判定、競速計時系統，以及第一／第三人稱視角切換。
系統著重於操控手感、賽道規則與練習效率，適合作為穿越機競速遊戲原型或教學／研究用途。

開發環境

遊戲引擎：Unity（建議 2021 LTS 以上）
程式語言：C#
輸入裝置：USB 遙控器 / 搖桿（使用 Unity Old Input Manager）
UI：TextMeshPro

素材來源：Unity Asset Store（Simple Drone 免費素材）

專案功能概覽
1. 穿越機飛行控制（FPVController）
使用 Rigidbody 物理系統
ACRO（角速度）飛行模式，類似 Betaflight 操控邏輯
支援 Roll / Pitch / Yaw / Throttle
可調整 Deadzone、輸入平滑（Smoothing）
支援 RC Rate / Super Rate / Expo 設定
起飛地面抑制，避免翻車
可依需求覆寫 Rigidbody 參數（質量、阻力）

2. 視角系統（CameraSwitchOnInput / CameraToggleC）
第一人稱（FPV）與第三人稱視角切換
遊戲開始時顯示場景鏡頭，操作後自動切換 FPV
快捷鍵 C 切換視角
快捷鍵 R 重置無人機位置與角度
重置時同步清除 Rigidbody 速度與角速度，避免慣性殘留

3. 賽道與 Gate 系統（GateIdentity / RaceDirector）
每個 Gate 具備獨立中央 Trigger
必須實際穿越 Gate 中心才算有效
集中式順序管理（不可跳關、逆向）
顏色提示：
綠色：目前目標 Gate
紅色：已完成 Gate
起點顯示 START，終點顯示 FINISH

4. 競速計時與最佳成績（MapTimeDisplay）
油門輸入觸發開始計時（避免誤觸）
顯示目前時間與最佳成績
每張地圖獨立記錄 Best Time
使用 PlayerPrefs 儲存，重新進入場景仍保留紀錄

5. 穿越機馬達音效（FPVMotorSound）
油門大小即時控制音量與 Pitch
手動循環音效，避免 MP3 Loop 斷音問題
3D 音效設定，關閉 Doppler
提升飛行時的沉浸感與回饋感


| 功能      |       操作      |
| ---------|-----------------|
| 油門      | 遙控器 Throttle |
| 翻滾      | Roll           |
| 俯仰　　　 | Pitch          |
| 偏航      | Yaw            |
| 切換視角 　| C              |
| 重置無人機 | R              |

FPVController.cs        // 飛行控制（核心）
CameraSwitchOnInput.cs // 啟動後自動切 FPV
CameraToggleC.cs       // 視角切換與重置
GateIdentity.cs        // 單一 Gate 行為
RaceDirector.cs        // 賽道規則與流程管理
MapTimeDisplay.cs      // 計時與最佳成績
FPVMotorSound.cs       // 馬達音效模擬

使用說明

開啟 Unity 專案
確認 Input Manager 已設定 Throttle / Roll / Pitch / Yaw
將遙控器連接電腦
播放場景後操作油門即可開始飛行與計時
依序穿越綠色 Gate 完成賽事
