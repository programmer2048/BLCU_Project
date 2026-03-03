using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // 必须引用
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement; // 用于场景跳转
using TMPro;
using UnityEngine.UI; // 用于 Button

public class RhythmGameManager : MonoBehaviour
{
    // ... (保留之前的 Header) ...
    [Header("资源设置")]
    public AudioSource musicSource;
    public AudioClip musicClip;
    public TextAsset jsonFile;

    [Header("轨道设置")]
    public Transform[] laneContainers;
    public UIString[] strings;
    public UnityEngine.UI.Image[] laneTouchFeedbacks;
    public RectTransform hitLineReference;

    [Header("UI & 特效")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public RectTransform effectCanvasLayer;

    // --- 新增：UI 面板与按钮 ---
    [Header("UI Panels & Buttons")]
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText; // 结算面板上的分显示
    public Button resumeBtn;
    public Button restartBtnPause;
    public Button menuBtnPause;
    public Button restartBtnOver;
    public Button menuBtnOver;

    [Header("Game Settings")]
    public float noteAppearDistance = 1200f;
    public string mainMenuSceneName = "01_MainUI"; // 主菜单场景名

    [Header("预制体")]
    public GameObject tapNotePrefab;
    public GameObject holdNotePrefab;
    public GameObject trapNotePrefab;
    public GameObject feedbackPrefab;
    public GameObject pulsePrefab;
    public GameObject phantomNotePrefab;

    // --- 内部变量 ---
    private int currentScore = 0;
    private int combo = 0;
    private int scorePerPerfect = 2;
    private int scorePerGood = 1;
    private List<ChartNote> allNotes = new List<ChartNote>();
    private List<NoteObject> activeNotes = new List<NoteObject>();
    private int nextNoteIndex = 0;
    private ChartJSON currentChart;

    // 音频同步核心变量
    private double dspSongStartTime;
    private double pauseBeginDspTime; // 记录暂停瞬间的时间
    private float secPerBeat;

    private bool isGameRunning = false;
    private bool isPaused = false; // 暂停状态标记
    private bool isGameOver = false; // 游戏结束标记

    private readonly float startDelay = 2.0f;
    public float songPosition { get; private set; }
    public float songPositionInBeats { get; private set; }
    public float pixelsPerBeat { get; private set; }
    private float hitLineY;
    private float perfectDist = 60f;
    private Coroutine comboAnimCoroutine;

    void Start()
    {
        InitializeGame();
        BindButtons(); // 绑定按钮事件
    }

    // --- 新增：按钮绑定逻辑 ---
    void BindButtons()
    {
        if (resumeBtn) resumeBtn.onClick.AddListener(() => TogglePause(false));
        if (restartBtnPause) restartBtnPause.onClick.AddListener(RetryLevel);
        if (menuBtnPause) menuBtnPause.onClick.AddListener(ReturnToMenu);

        if (restartBtnOver) restartBtnOver.onClick.AddListener(RetryLevel);
        if (menuBtnOver) menuBtnOver.onClick.AddListener(ReturnToMenu);

        if (pausePanel) pausePanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    public void InitializeGame()
    {
        if (hitLineReference != null && laneContainers.Length > 0)
        {
            Vector3 worldPos = hitLineReference.position;
            Vector3 localPos = laneContainers[0].InverseTransformPoint(worldPos);
            hitLineY = localPos.y;
            perfectDist = hitLineReference.rect.height * 0.5f;
        }
        else hitLineY = -300f;

        foreach (var img in laneTouchFeedbacks) if (img) img.canvasRenderer.SetAlpha(0f);
        UpdateScoreUI();
        if (comboText) comboText.gameObject.SetActive(false);

        if (jsonFile != null) ParseChartAndLoadAudio(jsonFile.text, false);
        else StartCoroutine(LoadResourcesFromDisk());
    }

    // ... (LoadResources, LoadAudioRoutine 保持不变) ...
    IEnumerator LoadResourcesFromDisk() { yield break; } // 省略具体实现
    IEnumerator LoadAudioRoutine(string f) { yield break; } // 省略具体实现

    void OnReadyToPlay() { StartGameplay(); }

    public void StartGameplay()
    {
        double outputLatency = AudioSettings.GetConfiguration().dspBufferSize / (double)AudioSettings.outputSampleRate;
        dspSongStartTime = AudioSettings.dspTime + startDelay + outputLatency;

        musicSource.PlayScheduled(dspSongStartTime);
        isGameRunning = true;
        isPaused = false;
        isGameOver = false;
    }

    void ParseChartAndLoadAudio(string jsonText, bool loadAudio)
    {
        currentChart = JsonUtility.FromJson<ChartJSON>(jsonText);
        if (currentChart.metadata.bpm <= 0) currentChart.metadata.bpm = 120;
        secPerBeat = 60f / currentChart.metadata.bpm;
        allNotes = currentChart.notes;
        allNotes.Sort((a, b) => a.beat.CompareTo(b.beat));
        pixelsPerBeat = noteAppearDistance / 4f;
        if (loadAudio) StartCoroutine(LoadAudioRoutine(currentChart.metadata.musicFile));
        else if (musicClip != null) { musicSource.clip = musicClip; OnReadyToPlay(); }
    }

    void Update()
    {
        // 如果游戏未开始，或者处于暂停/结束状态，不执行核心逻辑
        if (!isGameRunning || isPaused || isGameOver)
        {
            HandlePauseInputOnly(); // 允许在暂停时检测 ESC 继续
            return;
        }

        // 1. 计算当前歌曲位置
        songPosition = (float)(AudioSettings.dspTime - dspSongStartTime);
        songPositionInBeats = songPosition / secPerBeat;

        // 2. 生成音符
        while (nextNoteIndex < allNotes.Count && allNotes[nextNoteIndex].beat < songPositionInBeats + 4.0f)
        {
            SpawnNoteObject(allNotes[nextNoteIndex]);
            nextNoteIndex++;
        }

        // 3. 处理输入和音符状态
        HandleInput();
        UpdateActiveNotes();

        // 4. 新增：检测游戏结束 (歌曲播放结束 且 列表无剩余音符)
        CheckGameEnd();
    }

    // --- 新增：ESC 暂停检测 ---
    void HandlePauseInputOnly()
    {
        if (isGameOver) return;
        // 使用 New Input System 检测 ESC
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TogglePause(!isPaused);
        }
    }

    // --- 新增：暂停逻辑 (包含音频同步修正) ---
    public void TogglePause(bool pauseStatus)
    {
        if (isGameOver) return;

        isPaused = pauseStatus;

        if (isPaused)
        {
            // 暂停
            Time.timeScale = 0f; // 停止 Update 中的动画 deltaTime
            musicSource.Pause();
            pauseBeginDspTime = AudioSettings.dspTime; // 记录暂停时刻

            if (pausePanel) pausePanel.SetActive(true);
        }
        else
        {
            // 恢复
            Time.timeScale = 1f;
            musicSource.UnPause();

            // 关键：计算暂停了多久，并将歌曲开始时间向后推，
            // 否则 songPosition 会因为 dspTime 一直在走而瞬间跳跃。
            double pauseDuration = AudioSettings.dspTime - pauseBeginDspTime;
            dspSongStartTime += pauseDuration;

            if (pausePanel) pausePanel.SetActive(false);
        }
    }

    // --- 新增：游戏结束逻辑 ---
    void CheckGameEnd()
    {
        // 如果音符都生成完了，且活动音符也都销毁了，且音乐播完了 (或超时)
        bool noMoreNotes = nextNoteIndex >= allNotes.Count && activeNotes.Count == 0;
        bool musicFinished = !musicSource.isPlaying && songPosition > 1f; // songPosition > 1f 防止刚开始没播放就被判定结束

        if (noMoreNotes || (musicFinished && noMoreNotes))
        {
            // 稍微延迟一下显示结算，体验更好
            StartCoroutine(GameOverRoutine());
        }
    }

    IEnumerator GameOverRoutine()
    {
        isGameOver = true;
        yield return new WaitForSeconds(1.0f); // 等待最后特效播完

        Debug.Log("Game Completed!");
        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (finalScoreText) finalScoreText.text = $"最终得分: {currentScore}";

        // --- 核心：修改存档数据 ---
        AddRevenueToSave();
    }

    void AddRevenueToSave()
    {
        // 假设规则：得分的 10% 转化为金钱
        int moneyEarned = Mathf.FloorToInt(currentScore * 0.1f);

        // 增加金钱 (引用 SaveManager)
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CurrentGameData.money += moneyEarned;
            // 如果 SaveManager 需要手动保存，请调用 SaveManager.Instance.Save();
            Debug.Log($"旅费增加: {moneyEarned}");
        }
        else
        {
            Debug.LogWarning("SaveManager Instance not found!");
        }
    }

    // --- 新增：场景跳转逻辑 ---
    public void RetryLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        // 如果中途退出也要加钱，可以在这里调用 AddRevenueToSave()，通常中途退出不给钱
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ... (SpawnNoteObject, UpdateActiveNotes 保持不变) ...
    void SpawnNoteObject(ChartNote data)
    {
        if (data.lane < 0 || data.lane >= laneContainers.Length) return;
        GameObject prefab = tapNotePrefab;
        R_NoteType type = R_NoteType.Tap;
        if (data.type == "hold") { prefab = holdNotePrefab; type = R_NoteType.Hold; }
        else if (data.type == "trap") { prefab = trapNotePrefab; type = R_NoteType.Trap; }

        if (prefab == null) return;
        GameObject obj = Instantiate(prefab, laneContainers[data.lane]);
        NoteObject note = obj.GetComponent<NoteObject>();
        note.Init(this, data.beat, data.lane, type, data.duration, hitLineY);
        activeNotes.Add(note);
    }

    void UpdateActiveNotes()
    {
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            NoteObject note = activeNotes[i];
            if (note == null) { activeNotes.RemoveAt(i); continue; }
            float distToLine = (note.targetBeat - songPositionInBeats) * pixelsPerBeat;

            if (distToLine < -perfectDist - 20f)
            {
                if (note.type == R_NoteType.Trap)
                {
                    RemoveActiveNote(note);
                    Destroy(note.gameObject);
                }
                else if (!note.isHolding && !note.isHit)
                {
                    OnNoteMiss(note);
                }
            }
            if (note.isHolding) AddScore(1);
        }
    }

    void HandleInput()
    {
        if (Keyboard.current == null) return;

        // 只有游戏运行时才响应轨道输入
        if (!isPaused && !isGameOver)
        {
            CheckLaneInput(0, Keyboard.current.dKey);
            CheckLaneInput(1, Keyboard.current.fKey);
            CheckLaneInput(2, Keyboard.current.jKey);
            CheckLaneInput(3, Keyboard.current.kKey);
            CheckLaneInput(4, Keyboard.current.lKey);
        }

        // Update 中已经单独处理了 HandlePauseInputOnly 用于检测 ESC
        HandlePauseInputOnly();
    }

    // ... (CheckLaneInput, TriggerLaneVisuals, FadeOutLaneVisuals, OnNoteHit, OnTrapHit, OnNoteMiss, OnHoldComplete 保持不变) ...

    void CheckLaneInput(int lane, KeyControl key)
    {
        if (key.wasPressedThisFrame)
        {
            TriggerLaneVisuals(lane);
            NoteObject target = GetClosestHittableNote(lane);
            if (target != null)
            {
                float currentDist = Mathf.Abs((target.targetBeat - songPositionInBeats) * pixelsPerBeat);
                if (currentDist <= perfectDist * 2.5f)
                {
                    if (target.type == R_NoteType.Trap) OnTrapHit(target);
                    else OnNoteHit(target, currentDist <= perfectDist * 1.2f);
                }
            }
        }
        if (key.wasReleasedThisFrame)
        {
            FadeOutLaneVisuals(lane);
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                NoteObject note = activeNotes[i];
                if (note.laneIndex == lane && note.type == R_NoteType.Hold && note.isHolding)
                {
                    float endBeat = note.targetBeat + note.holdDuration;
                    if (songPositionInBeats < endBeat - 0.15f) OnNoteMiss(note);
                    else OnHoldComplete(note);
                }
            }
        }
    }

    void TriggerLaneVisuals(int lane)
    {
        float pluckRatio = (lane * 2 + 1) / 10f;
        if (lane < strings.Length && strings[lane]) strings[lane].Pluck(pluckRatio, 800f);
        if (lane < laneTouchFeedbacks.Length && laneTouchFeedbacks[lane]) laneTouchFeedbacks[lane].canvasRenderer.SetAlpha(0.6f);
        SpawnPulseEffect(lane);
        SpawnPhantomNote(lane);
    }
    void FadeOutLaneVisuals(int lane)
    {
        if (lane < laneTouchFeedbacks.Length && laneTouchFeedbacks[lane]) laneTouchFeedbacks[lane].CrossFadeAlpha(0f, 0.2f, false);
    }
    void OnNoteHit(NoteObject note, bool isPerfect)
    {
        combo++;
        UpdateComboUI();
        if (note.type == R_NoteType.Tap)
        {
            note.TriggerHit();
            int score = isPerfect ? scorePerPerfect : scorePerGood;
            string text = isPerfect ? "完美" : "不错";
            Color col = isPerfect ? new Color(1f, 0.8f, 0.2f) : Color.cyan;
            ShowFeedback(text, col, GetLaneWorldPos(note.laneIndex));
            AddScore(score);
            RemoveActiveNote(note);
        }
        else if (note.type == R_NoteType.Hold)
        {
            note.StartHolding();
            AddScore(scorePerGood);
        }
    }
    void OnTrapHit(NoteObject note)
    {
        combo = 0;
        UpdateComboUI();
        ShowFeedback("受伤", Color.red, GetLaneWorldPos(note.laneIndex));
        AddScore(-50);
        note.TriggerHit();
        RemoveActiveNote(note);
    }
    public void OnNoteMiss(NoteObject note)
    {
        combo = 0;
        UpdateComboUI();
        Vector3 pos = note != null ? note.rectTrans.position : GetLaneWorldPos(note.laneIndex);
        ShowFeedback("错过", Color.gray, pos);
        RemoveActiveNote(note);
        if (note != null) Destroy(note.gameObject);
    }
    public void OnHoldComplete(NoteObject note)
    {
        combo++;
        UpdateComboUI();
        ShowFeedback("完美", new Color(1f, 0.9f, 0.3f), GetLaneWorldPos(note.laneIndex));
        AddScore(scorePerPerfect);
        RemoveActiveNote(note);
        if (note != null) Destroy(note.gameObject);
    }

    // ... (AddScore, UpdateScoreUI, UpdateComboUI, AnimateComboText, ShowFeedback, SpawnPhantomNote, Helper Methods 保持不变) ...
    void AddScore(int val) { currentScore += val; UpdateScoreUI(); }
    void UpdateScoreUI() { if (scoreText) scoreText.text = $"得分: {currentScore}"; }
    void UpdateComboUI() { if (comboText) { if (combo > 1) { comboText.text = $"{combo} 连击"; comboText.gameObject.SetActive(true); if (comboAnimCoroutine != null) StopCoroutine(comboAnimCoroutine); comboAnimCoroutine = StartCoroutine(AnimateComboText()); } else comboText.gameObject.SetActive(false); } }
    IEnumerator AnimateComboText() { float timer = 0f; float duration = 0.1f; Vector3 startScale = Vector3.one * 1.5f; Vector3 endScale = Vector3.one; while (timer < duration) { timer += Time.deltaTime; if (comboText) comboText.transform.localScale = Vector3.Lerp(startScale, endScale, timer / duration); yield return null; } if (comboText) comboText.transform.localScale = endScale; }
    void ShowFeedback(string text, Color col, Vector3 worldPos)
    {
        if (!feedbackPrefab || !effectCanvasLayer) return;
        GameObject obj = Instantiate(feedbackPrefab, effectCanvasLayer);
        Canvas canvas = effectCanvasLayer.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(effectCanvasLayer, screenPoint, uiCamera, out Vector2 localPoint);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = localPoint + new Vector2(0, 40f);
        rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        FeedbackEffect script = obj.GetComponent<FeedbackEffect>();
        if (script) script.Setup(text, col);
    }
    void SpawnPhantomNote(int lane)
    {
        if (!phantomNotePrefab || !effectCanvasLayer) return;
        RectTransform stringRect = null;
        if (lane < strings.Length && strings[lane] != null) stringRect = strings[lane].GetComponent<RectTransform>();
        Vector3 targetWorldPos;
        if (stringRect != null)
        {
            float ratio = (lane * 2 + 1) / 10f;
            Vector3[] corners = new Vector3[4];
            stringRect.GetWorldCorners(corners);
            Vector3 leftCenter = (corners[0] + corners[1]) * 0.5f;
            Vector3 rightCenter = (corners[3] + corners[2]) * 0.5f;
            targetWorldPos = Vector3.Lerp(leftCenter, rightCenter, ratio);
        }
        else { targetWorldPos = GetLaneWorldPos(lane); targetWorldPos.y -= 300f; }
        GameObject obj = Instantiate(phantomNotePrefab, effectCanvasLayer);
        Canvas canvas = effectCanvasLayer.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, targetWorldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(effectCanvasLayer, screenPoint, uiCamera, out Vector2 localPoint);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = localPoint;
        rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        PhantomNote script = obj.GetComponent<PhantomNote>();
        string[] scaleChars = { "宫", "商", "角", "徵", "羽" };
        Color[] smokeColors = { new Color(1f, 0.4f, 0.4f), new Color(1f, 0.8f, 0.2f), new Color(0.4f, 1f, 0.4f), new Color(0.2f, 0.8f, 1f), new Color(0.8f, 0.4f, 1f) };
        int idx = lane % 5;
        if (script) script.Setup(scaleChars[idx], smokeColors[idx]);
    }
    Vector3 GetLaneWorldPos(int lane)
    {
        if (lane >= 0 && lane < laneContainers.Length)
        {
            Vector3 pos = laneContainers[lane].position;
            if (hitLineReference != null) pos.y = hitLineReference.position.y;
            return pos;
        }
        return Vector3.zero;
    }
    NoteObject GetClosestHittableNote(int lane)
    {
        NoteObject c = null; float min = float.MaxValue;
        foreach (var n in activeNotes)
        {
            if (n.laneIndex == lane && !n.isHit && !n.isHolding)
            {
                float d = Mathf.Abs((n.targetBeat - songPositionInBeats) * pixelsPerBeat);
                if (d < perfectDist * 3f && d < min) { min = d; c = n; }
            }
        }
        return c;
    }
    void RemoveActiveNote(NoteObject n) { if (activeNotes.Contains(n)) activeNotes.Remove(n); }
    void SpawnPulseEffect(int lane)
    {
        if (!pulsePrefab || !strings[lane]) return;
        RectTransform rt = strings[lane].GetComponent<RectTransform>();
        GameObject obj = Instantiate(pulsePrefab, rt);
        obj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        StringPulse s = obj.GetComponent<StringPulse>();
        if (s) s.Setup(1f, rt.rect.width);
    }
}