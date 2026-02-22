using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Boot场景 —— 极简版 (无动画，纯逻辑)
/// </summary>
public class BootSceneController : MonoBehaviour
{
    [Header("核心按钮")]
    public Button startButton;
    public Button settingsButton;
    public Button aboutButton;
    public Button quitButton; // 建议保留，若不需要可在Inspector留空

    [Header("场景跳转配置")]
    public string settingsSceneName = "SettingsScene";
    public string aboutSceneName = "AboutScene";

    void Awake()
    {
        // 1. 初始化核心管理器 (这一步绝对不能省，否则后面游戏会报错)
        EnsureManager<GameManager>("GameManager");
        EnsureManager<GameFlowManager>("GameFlowManager");
        EnsureManager<UIManager>("UIManager");
        EnsureManager<SaveManager>("SaveManager");
        EnsureManager<DebugManager>("DebugManager");
    }

    void Start()
    {
        // 绑定按钮事件
        BindButton(startButton, OnClickStart);
        BindButton(settingsButton, OnClickSettings);
        BindButton(aboutButton, OnClickAbout);
        BindButton(quitButton, OnClickQuit);
    }

    // --- 点击逻辑 ---
    private void OnClickStart()
    {
        Debug.Log("[Boot] 点击开始，准备异步加载...");

        // 1. 禁用按钮，防止玩家狂点
        startButton.interactable = false;

        // 2. 开启协程进行后台加载
        StartCoroutine(LoadSceneAsyncProcess());
    }
    IEnumerator LoadSceneAsyncProcess()
    {
        // 开始后台加载场景，假设下一个场景的 Build Index 是 1 (或者用名字 "01_MainUI")
        // LoadSceneMode.Single 表示替换当前场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
        // 只要没加载完，就等待下一帧
        while (!asyncLoad.isDone)
        {
            // 这里可以做一些事情，比如让 loading 图标转动
            // float progress = asyncLoad.progress; 
            // Debug.Log($"加载进度: {progress * 100}%");

            yield return null;
        }
    }
    private void OnClickSettings()
    {
        Debug.Log("[Boot] 进入设置");
        if (!string.IsNullOrEmpty(settingsSceneName))
        {
            SceneManager.LoadScene(settingsSceneName);
        }
    }
    private void OnClickAbout()
    {
        Debug.Log("[Boot] 进入关于");
        if (!string.IsNullOrEmpty(aboutSceneName))
        {
            SceneManager.LoadScene(aboutSceneName);
        }
    }
    private void OnClickQuit()
    {
        Debug.Log("[Boot] 退出游戏");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    // --- 辅助工具 ---
    private void BindButton(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }
    }
    private void EnsureManager<T>(string name) where T : MonoBehaviour
    {
        if (Object.FindAnyObjectByType<T>() == null)
        {
            var go = new GameObject(name);
            go.AddComponent<T>();
            DontDestroyOnLoad(go);
        }
    }
}