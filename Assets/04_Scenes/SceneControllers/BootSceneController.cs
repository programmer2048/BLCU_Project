using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class BootSceneController : MonoBehaviour
{
    [Header("核心按钮")]
    public Button startButton;     
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
    public Button skipButton;            
    public Button finalStartButton;      

    // 状态标记
    private bool hasSaveFile = false;
    private bool isVideoSkipped = false; 

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

        if (introVideoPanel != null)
        {
            introVideoPanel.SetActive(false); 
            if (videoCanvasGroup == null) videoCanvasGroup = introVideoPanel.GetComponent<CanvasGroup>();
            if (videoCanvasGroup == null) videoCanvasGroup = introVideoPanel.AddComponent<CanvasGroup>();
            videoCanvasGroup.alpha = 0f; 
        }

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
            skipButton.onClick.AddListener(OnSkipVideo);
        }

        if (finalStartButton != null)
        {
            finalStartButton.gameObject.SetActive(false); 
            finalStartButton.onClick.AddListener(OnFinalStartClicked);
        }

        CheckSaveState();
    }

    private void CheckSaveState()
    {
        bool canContinue = SaveManager.Instance.HasAnySave();
        hasSaveFile = canContinue;

        if (startButtonText != null) startButtonText.text = canContinue ? "继续旅程" : "开始寻梁";
        startButton.interactable = true;
    }

    private void OnClickMainStart()
    {
        Debug.Log("[Boot] 点击主菜单开始...");
        startButton.interactable = false;
        BGMManager.Instance.bgm.Stop(); 

        if (hasSaveFile)
        {
            Debug.Log("[Boot] 继续旧存档...");
            SaveManager.Instance.ContinueLastGame();
            
            // ★ 修改点1：直接调用全局管理器切换到游戏场景（使用 int 索引）
            TransitionManager.Instance.SwitchScene(mainSceneBuildIndex);
        }
        else
        {
            Debug.Log("[Boot] 准备进入新游戏视频流程...");
            StartCoroutine(PlayIntroFlow());
        }
    }

    IEnumerator PlayIntroFlow()
    {
        introVideoPanel.SetActive(true);
        if (skipButton != null) skipButton.gameObject.SetActive(false); 

        introVideoPlayer.clip = introClip;
        introVideoPlayer.Prepare();
        while (!introVideoPlayer.isPrepared) yield return null;

        float fadeDuration = 1.0f; 
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

        while (introVideoPlayer.isPlaying && !isVideoSkipped) yield return null;
        
        introVideoPlayer.Pause(); 

        if (skipButton != null) skipButton.gameObject.SetActive(false); 
        if (finalStartButton != null) finalStartButton.gameObject.SetActive(true);
        else OnFinalStartClicked();
    }

    private void OnSkipVideo() => isVideoSkipped = true;

    private void OnFinalStartClicked()
    {
        Debug.Log("[Boot] 最终确认进入游戏...");
        if (finalStartButton != null) finalStartButton.interactable = false; 
        
        SaveManager.Instance.CreateNewGame();
        
        TransitionManager.Instance.SwitchScene(mainSceneBuildIndex);
    }

    private void OnClickSettings() 
    { 
        if (!string.IsNullOrEmpty(settingsSceneName)) 
        {
            SettingsManager.Instance.ToggleSettings();
            //TransitionManager.Instance.SwitchScene(settingsSceneName);
        }
    }

    private void OnClickAbout() 
    { 
        if (!string.IsNullOrEmpty(aboutSceneName)) 
        {
            TransitionManager.Instance.SwitchScene(aboutSceneName);
        }
    }

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