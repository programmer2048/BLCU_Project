using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Boot场景 —— 初始化所有管理器
/// 这是游戏启动的第一个场景
/// </summary>
public class BootSceneController : MonoBehaviour
{
    [Header("开始界面UI")]
    public GameObject titlePanel;
    public TextMeshProUGUI titleText;
    public Button startButton;
    public Button quitButton;

    [Header("加载界面UI")]
    public TextMeshProUGUI loadingText;

    [Header("自动进入主菜单")]
    public bool autoEnterMainUI = true;
    public float autoEnterDelay = 1.0f;

    void Awake()
    {
        Debug.Log("[Boot] 启动场景初始化...");

        // 确保核心管理器存在（DontDestroyOnLoad）
        EnsureManager<GameManager>("GameManager");
        EnsureManager<GameFlowManager>("GameFlowManager");
        EnsureManager<UIManager>("UIManager");
        EnsureManager<SaveManager>("SaveManager");
        EnsureManager<DebugManager>("DebugManager");
    }

    void Start()
    {
        AutoBindUIIfMissing();

        if (titlePanel != null) titlePanel.SetActive(true);
        if (titleText != null) titleText.text = "山河故人归\n点击开始旅程";

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnClickStart);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnClickQuit);
        }

        if (titlePanel == null && loadingText != null)
        {
            loadingText.text = "正在加载中...";
        }

        if (startButton == null && autoEnterMainUI)
        {
            StartCoroutine(AutoEnterMainUI());
        }
    }

    private void AutoBindUIIfMissing()
    {
        if (titlePanel == null)
            titlePanel = GameObject.Find("TitlePanel");

        if (titleText == null)
        {
            var go = GameObject.Find("TitleText");
            if (go != null) titleText = go.GetComponent<TextMeshProUGUI>();
        }

        if (startButton == null)
        {
            var go = GameObject.Find("StartButton");
            if (go != null) startButton = go.GetComponent<Button>();
        }

        if (quitButton == null)
        {
            var go = GameObject.Find("QuitButton");
            if (go != null) quitButton = go.GetComponent<Button>();
        }

        if (loadingText == null)
        {
            var go = GameObject.Find("LoadingText");
            if (go != null) loadingText = go.GetComponent<TextMeshProUGUI>();
        }
    }

    private IEnumerator AutoEnterMainUI()
    {
        if (loadingText != null)
            loadingText.text = "正在加载中...";

        yield return null;

        if (autoEnterDelay > 0f)
            yield return new WaitForSeconds(autoEnterDelay);

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToMainUI();
        else
            Debug.LogError("[Boot] GameFlowManager not found, cannot enter MainUI.");
    }

    private void OnClickStart()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.StartNewGameFromTitle();
    }

    private void OnClickQuit()
    {
        Application.Quit();
    }

    private void EnsureManager<T>(string name) where T : MonoBehaviour
    {
        if (Object.FindAnyObjectByType<T>() == null)
        {
            var go = new GameObject(name);
            go.AddComponent<T>();
            Debug.Log($"[Boot] 创建管理器: {name}");
        }
    }
}
