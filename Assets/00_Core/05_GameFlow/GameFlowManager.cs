using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 游戏流程管理器 —— 控制全局状态流转与场景切换
/// 所有场景跳转必须通过此Manager
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("当前状态")]
    [SerializeField] private GameState _currentState = GameState.None;
    public GameState CurrentState => _currentState;

    // 场景名称映射
    private const string SCENE_BOOT = "00_Boot";
    private const string SCENE_MAIN_UI = "01_MainUI";
    private const string SCENE_PARTTIME = "02_PartTimeJobs";
    private const string SCENE_MINIGAME = "03_MiniGames";
    private const string SCENE_STORY = "04_StoryScenes";
    private const string SCENE_ALBUM = "05_Album";

    // 当前正在进行的小游戏章节索引 (0-3)
    public int CurrentChapterIndex { get; set; } = -1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        ChangeState(GameState.Boot);
    }

    /// <summary>
    /// 切换游戏状态
    /// </summary>
    public void ChangeState(GameState newState)
    {
        if (_currentState == newState) return;
        var oldState = _currentState;
        _currentState = newState;
        Debug.Log($"[GameFlow] 状态切换: {oldState} → {newState}");
        EventBus.Publish<GameState>(GameEvent.OnGameStateChanged, newState);
        HandleStateTransition(newState);
    }

    private void HandleStateTransition(GameState state)
    {
        switch (state)
        {
            case GameState.Boot:
                StartCoroutine(BootSequence());
                break;
            case GameState.Prologue:
                StartCoroutine(PrologueSequence());
                break;
            case GameState.MainUI:
                LoadScene(SCENE_MAIN_UI);
                break;
            case GameState.Social:
                // 在MainUI场景内切换面板，不换场景
                EventBus.Publish(GameEvent.OnDialogueStart);
                break;
            case GameState.PartTimeJob:
                LoadScene(SCENE_PARTTIME);
                break;
            case GameState.Match3:
                // 在PartTimeJob场景内激活Match3
                EventBus.Publish(GameEvent.OnMatch3Start);
                break;
            case GameState.Rhythm:
                // 在PartTimeJob场景内激活Rhythm
                EventBus.Publish(GameEvent.OnRhythmStart);
                break;
            case GameState.Story:
                LoadScene(SCENE_STORY);
                break;
            case GameState.MiniGame:
                LoadScene(SCENE_MINIGAME);
                break;
            case GameState.Album:
                LoadScene(SCENE_ALBUM);
                break;
            case GameState.Ending:
                StartCoroutine(EndingSequence());
                break;
        }
    }

    private IEnumerator EndingSequence()
    {
        if (SceneManager.GetActiveScene().name != SCENE_STORY)
        {
            LoadScene(SCENE_STORY);
            yield return null;
            yield return null;
        }

        EventBus.Publish(GameEvent.OnEndingTriggered);
    }

    private IEnumerator BootSequence()
    {
        if (SceneManager.GetActiveScene().name != SCENE_BOOT)
        {
            LoadScene(SCENE_BOOT);
            yield return null;
        }

        Debug.Log("[GameFlow] Boot: 进入开始界面，等待玩家点击开始...");
    }

    private IEnumerator PrologueSequence()
    {
        Debug.Log("[GameFlow] Prologue: 序章开始...");
        Debug.Log("[GameFlow] Prologue: 等待2秒...");
        yield return new WaitForSeconds(2f);
        Debug.Log("[GameFlow] Prologue: 等待完成，切换到MainUI");
        try
        {
            ChangeState(GameState.MainUI);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameFlow] PrologueSequence异常: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>加载场景</summary>
    public void LoadScene(string sceneName)
    {
        Debug.Log($"[GameFlow] 加载场景: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    // ── 便捷方法 ──
    public void GoToMainUI() => ChangeState(GameState.MainUI);
    public void GoToPartTimeJob() => ChangeState(GameState.PartTimeJob);
    public void GoToMatch3() => ChangeState(GameState.Match3);
    public void GoToRhythm() => ChangeState(GameState.Rhythm);
    public void GoToStory() => ChangeState(GameState.Story);
    public void GoToMiniGame(int chapterIndex)
    {
        CurrentChapterIndex = chapterIndex;
        ChangeState(GameState.MiniGame);
    }
    public void GoToAlbum() => ChangeState(GameState.Album);
    public void GoToSocial() => ChangeState(GameState.Social);
    public void GoToEnding() => ChangeState(GameState.Ending);

    public void StartNewGameFromTitle()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.InitializeData();

        ChangeState(GameState.Prologue);
    }
}
