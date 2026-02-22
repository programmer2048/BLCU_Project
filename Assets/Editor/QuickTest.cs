using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections;

/// <summary>
/// 编辑器测试工具 —— 只触发不用DontDestroyOnLoad
/// </summary>
public class QuickTest : Editor
{
    [MenuItem("Test/Flow Test - Go PartTimeJob")]
    static void TestPartTimeJob()
    {
        if (!Application.isPlaying) return;
        var gfm = Object.FindAnyObjectByType<GameFlowManager>();
        if (gfm != null) { gfm.GoToPartTimeJob(); Debug.Log("[QTest] GoToPartTimeJob called"); }
        else Debug.LogError("[QTest] GameFlowManager not found!");
    }

    [MenuItem("Test/Flow Test - Go MainUI")]
    static void TestMainUI()
    {
        if (!Application.isPlaying) return;
        var gfm = Object.FindAnyObjectByType<GameFlowManager>();
        if (gfm != null) { gfm.GoToMainUI(); Debug.Log("[QTest] GoToMainUI called"); }
        else Debug.LogError("[QTest] GameFlowManager not found!");
    }

    [MenuItem("Test/Flow Test - Go MiniGames")]
    static void TestMiniGames()
    {
        if (!Application.isPlaying) return;
        var gfm = Object.FindAnyObjectByType<GameFlowManager>();
        if (gfm != null) { gfm.ChangeState(GameState.MiniGame); Debug.Log("[QTest] Go MiniGames called"); }
        else Debug.LogError("[QTest] GameFlowManager not found!");
    }

    [MenuItem("Test/Flow Test - Go Story")]
    static void TestStory()
    {
        if (!Application.isPlaying) return;
        var gfm = Object.FindAnyObjectByType<GameFlowManager>();
        if (gfm != null) { gfm.ChangeState(GameState.Story); Debug.Log("[QTest] Go Story called"); }
        else Debug.LogError("[QTest] GameFlowManager not found!");
    }

    [MenuItem("Test/Flow Test - Go Album")]
    static void TestAlbum()
    {
        if (!Application.isPlaying) return;
        var gfm = Object.FindAnyObjectByType<GameFlowManager>();
        if (gfm != null) { gfm.GoToAlbum(); Debug.Log("[QTest] Go Album called"); }
        else Debug.LogError("[QTest] GameFlowManager not found!");
    }

    [MenuItem("Test/Flow Test - Go Social")]
    static void TestSocial()
    {
        if (!Application.isPlaying) return;
        var gfm = Object.FindAnyObjectByType<GameFlowManager>();
        if (gfm != null) { gfm.GoToSocial(); Debug.Log("[QTest] Go Social called"); }
        else Debug.LogError("[QTest] GameFlowManager not found!");
    }

    [MenuItem("Test/Flow Test - Go Ending")]
    static void TestEnding()
    {
        if (!Application.isPlaying) return;
        var gfm = Object.FindAnyObjectByType<GameFlowManager>();
        if (gfm != null) { gfm.GoToEnding(); Debug.Log("[QTest] Go Ending called"); }
        else Debug.LogError("[QTest] GameFlowManager not found!");
    }

    [MenuItem("Test/Flow Test - Start Match3")]
    static void TestMatch3()
    {
        if (!Application.isPlaying) return;
        var gfm = Object.FindAnyObjectByType<GameFlowManager>();
        if (gfm != null) { gfm.GoToMatch3(); Debug.Log("[QTest] Start Match3 called"); }
        else Debug.LogError("[QTest] GameFlowManager not found!");
    }

    [MenuItem("Test/Flow Test - Start Rhythm")]
    static void TestRhythm()
    {
        if (!Application.isPlaying) return;
        var gfm = Object.FindAnyObjectByType<GameFlowManager>();
        if (gfm != null) { gfm.GoToRhythm(); Debug.Log("[QTest] Start Rhythm called"); }
        else Debug.LogError("[QTest] GameFlowManager not found!");
    }

    [MenuItem("Test/Flow Test - Add 500 Currency")]
    static void TestCurrency()
    {
        if (!Application.isPlaying) return;
        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm != null) { gm.AddCurrency(500); Debug.Log($"[QTest] Currency: {gm.TravelFee}"); }
        else Debug.LogError("[QTest] GameManager not found!");
    }

    [MenuItem("Test/Flow Test - Unlock Spot 0")]
    static void TestUnlock()
    {
        if (!Application.isPlaying) return;
        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm != null)
        {
            gm.UnlockSpot(0);
            bool unlocked = gm.IsSpotUnlocked(0);
            Debug.Log($"[QTest] UnlockSpot(0) => unlocked={unlocked}, Fee={gm.TravelFee}");
        }
        else Debug.LogError("[QTest] GameManager not found!");
    }

    [MenuItem("Test/Flow Test - Explore Spot 0")]
    static void TestExplore()
    {
        if (!Application.isPlaying) return;
        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm != null)
        {
            gm.SetSpotExplored(0);
            bool explored = gm.IsSpotExplored(0);
            Debug.Log($"[QTest] SetSpotExplored(0) => explored={explored}");
        }
        else Debug.LogError("[QTest] GameManager not found!");
    }

    [MenuItem("Test/Flow Test - Status")]
    static void TestStatus()
    {
        if (!Application.isPlaying) return;
        var gm = Object.FindAnyObjectByType<GameManager>();
        var gfm = Object.FindAnyObjectByType<GameFlowManager>();
        string scene = SceneManager.GetActiveScene().name;
        string state = gfm?.CurrentState.ToString() ?? "NULL";
        string fee = gm?.TravelFee.ToString() ?? "NULL";
        string msg = $"[QTest] Scene:{scene} State:{state} Fee:{fee}";
        Debug.Log(msg);
        File.WriteAllText("F:/Unity/UnityProjects/BLCU_Project/test_status.txt", msg);
    }
}
