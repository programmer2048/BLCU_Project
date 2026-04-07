using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class DifferenceGameManager : MonoBehaviour
{
    public static DifferenceGameManager Instance { get; private set; }

    [Header("游戏设置")]
    public int totalDifferences = 3;
    public string nextSceneName = "01_MainUI"; 

    [Header("UI 组件")]
    public TextMeshProUGUI progressText; 
    public GameObject winPanel;      
    public GameObject errorEffect;      

    private int currentFound = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        winPanel.SetActive(false);
        if (errorEffect) errorEffect.SetActive(false);
        UpdateUI();
    }

    public void OnDifferenceFound()
    {
        currentFound++;
        UpdateUI();

        if (currentFound >= totalDifferences)
        {
            GameWin();
        }
    }

    // 处理点错的情况
    public void OnBackgroundClicked()
    {
        Debug.Log("点错了！");
        if (errorEffect != null)
        {
            errorEffect.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(HideErrorEffect());
        }
    }

    private System.Collections.IEnumerator HideErrorEffect()
    {
        yield return new WaitForSeconds(0.5f);
        if (errorEffect) errorEffect.SetActive(false);
    }

    void UpdateUI()
    {
        if (progressText != null)
            progressText.text = $"进度: {currentFound} / {totalDifferences}";
    }

    void GameWin()
    {
        Debug.Log("全部找到！");
        winPanel.SetActive(true);
        SaveManager.Instance.CurrentGameData.chapterSubState = 0;
        SaveManager.Instance.CurrentGameData.currentChapter = 3;
        // 保存通关状态，比如给玩家发钱或者解锁下一章
        // SaveManager.Instance.CurrentGameData.money += 100;
        // SaveManager.Instance.SaveCurrentGame();
    }

    // 绑定在胜利面板的按钮上
    public void GoToNextScene()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.CurrentGameData != null)
        {
            bool isReplay = PlayerPrefs.GetInt("IsReplayMode", 0) == 1;
            if (!isReplay)
            {
                SaveManager.Instance.CurrentGameData.currentChapter = 3;
                SaveManager.Instance.CurrentGameData.chapterSubState = 0;
                SaveManager.Instance.SaveCurrentGame();
            }
            // 清理 Replay 标记
            PlayerPrefs.DeleteKey("IsReplayMode");
            // 返回主界面
            SceneManager.LoadScene("01_MainUI");
        }
    }
}