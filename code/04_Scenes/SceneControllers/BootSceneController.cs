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
    private string settingsSceneName = "Setup";
    private string aboutSceneName = "About";
    public int mainSceneBuildIndex = 1;

    [Header("--- 开场动画设置 ---")]
    public GameObject introVideoPanel;   
    public CanvasGroup videoCanvasGroup; 
    public VideoPlayer introVideoPlayer; 
    public VideoClip introClip;        
    public AudioClip bgmClip;

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
        SettingsManager.Instance.ApplyVolume();

        if (BGMManager.Instance.bgm.clip != bgmClip)
        {
            BGMManager.Instance.bgm.clip = bgmClip;
            BGMManager.Instance.bgm.Play();
        }

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

    private void OnClickMainStart()
    {
        Debug.Log("[Boot] 点击主菜单开始...");
        startButton.interactable = false;

        BGMManager.Instance.bgm.Stop(); // 暂停 bgm

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

        introVideoPlayer.Play();
        if (skipButton != null) skipButton.gameObject.SetActive(true);
        isVideoSkipped = false;

        Debug.Log("[Boot] 视频播放中...");
        while (introVideoPlayer.isPlaying && !isVideoSkipped)
        {
            yield return null;
        }
        Debug.Log("[Boot] 视频阶段结束");
        introVideoPlayer.Pause(); // 暂停在当前帧（如果是自然播放结束，通常停在最后一帧）

        if (skipButton != null) skipButton.gameObject.SetActive(false); // 隐藏跳过按钮
        if (finalStartButton != null)
        {
            finalStartButton.gameObject.SetActive(true);
        }
        else
        {
            OnFinalStartClicked();
        }
    }
    private void OnSkipVideo()
    {
        isVideoSkipped = true;
    }
    private void OnFinalStartClicked()
    {
        Debug.Log("[Boot] 最终确认进入游戏...");

        if (finalStartButton != null) finalStartButton.interactable = false; // 防止连点
        SaveManager.Instance.CreateNewGame();
        StartCoroutine(LoadSceneAsyncProcess());
    }

    IEnumerator LoadSceneAsyncProcess()
    {
        // 这里可以直接开始加载
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainSceneBuildIndex, LoadSceneMode.Single);

        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    private void OnClickSettings() { 
        //if (!string.IsNullOrEmpty(settingsSceneName)) SceneManager.LoadScene(settingsSceneName);
    }
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