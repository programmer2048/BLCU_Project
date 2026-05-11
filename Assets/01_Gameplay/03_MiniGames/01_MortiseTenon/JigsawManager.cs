using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class JigsawManager : MonoBehaviour
{
    public static JigsawManager Instance;

    [Header("核心对象")]
    public GameObject piecesParent; 
    public GameObject completeImage; 
    public GameObject victoryPanel;   

    [Header("设置")]
    public int totalPieces = 10;

    private int lockedCount = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (piecesParent != null) piecesParent.SetActive(true);
        if (completeImage != null) completeImage.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);

        lockedCount = 0;
    }

    // 由 JigsawPiece 调用
    public void OnPieceLocked()
    {
        lockedCount++;

        // 检查是否全部拼完
        if (lockedCount >= totalPieces)
        {
            StartCoroutine(WinSequence());
        }
    }

    // 胜利流程协程
    IEnumerator WinSequence()
    {
        yield return new WaitForSeconds(0.2f);

        if (piecesParent != null) piecesParent.SetActive(false);
        if (completeImage != null) completeImage.SetActive(true);

        // 可选：在这里播放一个“光效”或“音效”，掩盖替换时的突兀感
        // AudioManager.Play("LevelComplete");

        yield return new WaitForSeconds(1.0f);

        if (victoryPanel != null) victoryPanel.SetActive(true);

        Debug.Log("游戏胜利流程结束");
    }

    public void ExitToMap()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.CurrentGameData != null)
        {
            bool isReplay = PlayerPrefs.GetInt("IsReplayMode", 0) == 1;
            if (!isReplay)
            {
                SaveManager.Instance.CurrentGameData.currentChapter = 2;
                SaveManager.Instance.CurrentGameData.chapterSubState = 0;
                SaveManager.Instance.SaveCurrentGame();
            }
            // 清理 Replay 标记
            PlayerPrefs.DeleteKey("IsReplayMode");
            // 返回主界面
            TransitionManager.Instance.SwitchScene("01_MainUI");
        }
    }
}