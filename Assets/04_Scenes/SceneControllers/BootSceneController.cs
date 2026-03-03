using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;

public class BootSceneController : MonoBehaviour
{
    [Header("核心按钮")]
    public Button startButton;     // 主界面原本的开始/继续按钮
    public Button settingsButton;
    public Button aboutButton;
    public Button quitButton;

    [Header("UI 反馈")]
    public Text startButtonText;

    [Header("场景跳转配置")]
    public string settingsSceneName = "SettingsScene";
    public string aboutSceneName = "AboutScene";
    public int mainSceneBuildIndex = 1;

    [Header("--- 开场动画设置 ---")]
    public GameObject introVideoPanel;   // 整个视频层的父物体
    public CanvasGroup videoCanvasGroup; // 【必须】挂在 introVideoPanel 上，用于控制透明度
    public VideoPlayer introVideoPlayer; // 视频播放器
    public VideoClip introClip;          // 视频资源

    [Header("--- 视频层内部按钮 ---")]
    public Button skipButton;            // 视频播放时的“跳过”按钮
    public Button finalStartButton;      // 视频结束后出现的“进入游戏”按钮

    // 状态标记
    private bool hasSaveFile = false;
    private bool isVideoSkipped = false; // 标记是否点击了跳过

    void Awake()
    {
        EnsureManager<SaveManager>("SaveManager");
    }

    void Start()
    {
        BindButton(startButton, OnClickMainStart);
        BindButton(settingsButton, OnClickSettings);
        BindButton(aboutButton, OnClickAbout);
        BindButton(quitButton, OnClickQuit);

        // --- 初始化视频层状态 ---
        if (introVideoPanel != null)
        {
            introVideoPanel.SetActive(false); // 默认隐藏
            // 如果没有赋值，尝试自动获取
            if (videoCanvasGroup == null) videoCanvasGroup = introVideoPanel.GetComponent<CanvasGroup>();
            if (videoCanvasGroup == null) videoCanvasGroup = introVideoPanel.AddComponent<CanvasGroup>();

            videoCanvasGroup.alpha = 0f; // 确保初始透明度为0
        }

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
            skipButton.onClick.AddListener(OnSkipVideo);
        }

        if (finalStartButton != null)
        {
            finalStartButton.gameObject.SetActive(false); // 初始隐藏
            finalStartButton.onClick.AddListener(OnFinalStartClicked);
        }

        CheckSaveState();
    }

    private void CheckSaveState()
    {
        bool canContinue = SaveManager.Instance.HasAnySave();
        hasSaveFile = canContinue;

        if (startButtonText != null)
        {
            startButtonText.text = canContinue ? "继续旅程" : "开始寻梁";
        }
        startButton.interactable = true;
    }

    // --- 1. 主界面点击开始 ---
    private void OnClickMainStart()
    {
        Debug.Log("[Boot] 点击主菜单开始...");
        startButton.interactable = false;

        if (hasSaveFile)
        {
            // 旧存档：直接加载
            Debug.Log("[Boot] 继续旧存档...");
            SaveManager.Instance.ContinueLastGame();
            StartCoroutine(LoadSceneAsyncProcess());
        }
        else
        {
            // 新存档：进入视频流程
            Debug.Log("[Boot] 准备进入新游戏视频流程...");
            StartCoroutine(PlayIntroFlow());
        }
    }

    // --- 流程核心协程 ---
    IEnumerator PlayIntroFlow()
    {
        // 1. 激活面板，但此时 Alpha 是 0
        introVideoPanel.SetActive(true);
        if (skipButton != null) skipButton.gameObject.SetActive(false); // 先不显示跳过，等淡入完

        // 准备视频
        introVideoPlayer.clip = introClip;
        introVideoPlayer.Prepare();
        while (!introVideoPlayer.isPrepared) yield return null;

        // 2. 执行淡入动画 (0 -> 1)
        float fadeDuration = 1.0f; // 淡入耗时1秒
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            videoCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }
        videoCanvasGroup.alpha = 1f;

        // 3. 开始播放视频
        introVideoPlayer.Play();
        if (skipButton != null) skipButton.gameObject.SetActive(true);
        isVideoSkipped = false;

        Debug.Log("[Boot] 视频播放中...");

        // 4. 等待视频结束 或 被跳过
        // 注意：这里我们用一个循环来检测，如果 isVideoSkipped 变 true 或者 isPlaying 变 false (自然结束)
        while (introVideoPlayer.isPlaying && !isVideoSkipped)
        {
            yield return null;
        }

        // 5. 视频阶段结束的处理
        Debug.Log("[Boot] 视频阶段结束");
        introVideoPlayer.Pause(); // 暂停在当前帧（如果是自然播放结束，通常停在最后一帧）

        if (skipButton != null) skipButton.gameObject.SetActive(false); // 隐藏跳过按钮

        // 6. 显示“正式开始”按钮等待玩家点击
        if (finalStartButton != null)
        {
            finalStartButton.gameObject.SetActive(true);
            // 此时协程结束，等待 OnFinalStartClicked 被按钮触发
        }
        else
        {
            // 如果没配置那个按钮，就自动进下一步
            OnFinalStartClicked();
        }
    }

    // --- 辅助：跳过按钮逻辑 ---
    private void OnSkipVideo()
    {
        isVideoSkipped = true;
        // 此时协程里的 while 循环会退出，进入 Step 5
    }

    // --- 2. 视频结束后，点击“正式开始” ---
    private void OnFinalStartClicked()
    {
        Debug.Log("[Boot] 最终确认进入游戏...");

        if (finalStartButton != null) finalStartButton.interactable = false; // 防止连点

        // A. 创建存档 (此时才真正创建)
        SaveManager.Instance.CreateNewGame();

        // B. 直接加载场景
        // 注意：这里我们不隐藏 introVideoPanel，也不做 fade out。
        // 它会一直盖在屏幕上，直到新场景加载完毕把 BootScene 卸载掉。
        StartCoroutine(LoadSceneAsyncProcess());
    }

    IEnumerator LoadSceneAsyncProcess()
    {
        // 这里可以直接开始加载
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainSceneBuildIndex, LoadSceneMode.Single);

        // 禁止自动跳转（可选）：如果你想在加载完让用户再点一次，可以设为 false。
        // 但根据你的需求“直接跳转”，这里默认 true 即可。
        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    // ... (其他通用代码保持不变 Settings, About, Quit 等) ...
    private void OnClickSettings() { if (!string.IsNullOrEmpty(settingsSceneName)) SceneManager.LoadScene(settingsSceneName); }
    private void OnClickAbout() { if (!string.IsNullOrEmpty(aboutSceneName)) SceneManager.LoadScene(aboutSceneName); }
    private void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    private void BindButton(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn != null) { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(action); }
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