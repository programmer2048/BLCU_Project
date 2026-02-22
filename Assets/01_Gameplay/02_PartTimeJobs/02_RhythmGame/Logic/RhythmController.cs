using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 音游总控
/// </summary>
public class RhythmController : MonoBehaviour
{
    [Header("组件")]
    public RhythmSpawner spawner;
    public RhythmJudge judge;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI judgeText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI resultText;
    public GameObject resultPanel;
    public Button[] trackButtons;
    public Button backButton;
    public Image judgeLine;

    [Header("设置")]
    public float gameDuration = 45f;
    public float feeMultiplier = 0.3f;
    public KeyCode[] keyboardKeys = { KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K };

    private int _score;
    private float _timeRemaining;
    private bool _isPlaying;
    private int _combo;

    void Start()
    {
        if (spawner == null) spawner = GetComponentInChildren<RhythmSpawner>();
        if (judge == null) judge = GetComponentInChildren<RhythmJudge>();

        EnsureTrackButtonsCached();

        // 设置轨道按钮
        if (trackButtons != null)
        {
            for (int i = 0; i < trackButtons.Length; i++)
            {
                int track = i;
                if (trackButtons[i] != null)
                    trackButtons[i].onClick.AddListener(() => OnTrackClick(track));
            }
        }

        if (backButton != null)
            backButton.onClick.AddListener(ReturnToMainUI);
    }

    void OnEnable()
    {
        EventBus.Subscribe(GameEvent.OnRhythmStart, StartGame);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe(GameEvent.OnRhythmStart, StartGame);
    }

    public void StartGame()
    {
        Debug.Log("[Rhythm] 游戏开始！");
        _score = 0;
        _combo = 0;
        _timeRemaining = gameDuration;
        _isPlaying = true;

        if (resultPanel != null) resultPanel.SetActive(false);
        UpdateUI();

        spawner.StartSpawning();
    }

    void Update()
    {
        if (!_isPlaying) return;

        _timeRemaining -= Time.deltaTime;
        if (timerText != null)
            timerText.text = $"时间: {Mathf.CeilToInt(_timeRemaining)}s";

        if (_timeRemaining <= 0)
        {
            EndGame();
            return;
        }

        // 清理超出判定范围的音符
        CleanMissedNotes();

        HandleKeyboardInput();
    }

    private void HandleKeyboardInput()
    {
        int trackCount = keyboardKeys != null ? keyboardKeys.Length : 0;
        if (trackButtons != null && trackButtons.Length > 0)
            trackCount = Mathf.Min(trackCount, trackButtons.Length);

        if (trackCount <= 0) return;

        for (int i = 0; i < trackCount; i++)
        {
            if (IsTrackKeyPressed(i))
            {
                OnTrackClick(i);
            }
        }
    }

    private void EnsureTrackButtonsCached()
    {
        if (trackButtons != null && trackButtons.Length > 0) return;

        trackButtons = new Button[4];
        for (int i = 0; i < trackButtons.Length; i++)
        {
            var btnObj = GameObject.Find($"TrackButton_{i}");
            if (btnObj != null)
                trackButtons[i] = btnObj.GetComponent<Button>();
        }
    }

    private bool IsTrackKeyPressed(int track)
    {
        // 1. 基础检查
        if (keyboardKeys == null || track < 0 || track >= keyboardKeys.Length) return false;

        KeyCode key = keyboardKeys[track];

        // 2. 如果安装了 New Input System，优先使用新系统
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            // 这里利用 switch 表达式把旧的 KeyCode 映射到新的 Input System
            return key switch
            {
                KeyCode.D => keyboard.dKey.wasPressedThisFrame,
                KeyCode.F => keyboard.fKey.wasPressedThisFrame,
                KeyCode.J => keyboard.jKey.wasPressedThisFrame,
                KeyCode.K => keyboard.kKey.wasPressedThisFrame,
                // 如果你有其他按键（比如 A, S, W, Space），需要在这里补上
                // KeyCode.Space => keyboard.spaceKey.wasPressedThisFrame,
                _ => false
            };
        }
        return false; // 如果新系统启用但没有键盘，返回 false
#else
        // 3. 只有在没安装新系统（或旧系统有效）时，才运行这行旧代码
        return Input.GetKeyDown(key);
#endif
    }

    private void OnTrackClick(int track)
    {
        if (!_isPlaying) return;

        // 找到该轨道最接近判定线的音符
        RhythmNote closest = null;
        float closestDist = float.MaxValue;

        foreach (var note in spawner.ActiveNotes)
        {
            if (note == null || note.track != track || note.isHit) continue;
            float dist = Mathf.Abs(note.GetY() - judge.judgeLine);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = note;
            }
        }

        if (closest == null) return;

        var result = judge.Judge(closest);
        if (result != RhythmJudge.JudgeResult.Miss)
        {
            closest.isHit = true;
            _score += judge.GetScore(result);
            _combo++;
            spawner.RemoveNote(closest);

            if (judgeText != null)
                judgeText.text = $"{judge.GetJudgeText(result)} x{_combo}";
        }
        else
        {
            _combo = 0;
            if (judgeText != null)
                judgeText.text = "失";
        }

        UpdateUI();
    }

    private void CleanMissedNotes()
    {
        var notesToRemove = new List<RhythmNote>();
        foreach (var note in spawner.ActiveNotes)
        {
            if (note != null && note.GetY() < judge.judgeLine - judge.okRange - 50f)
            {
                notesToRemove.Add(note);
                _combo = 0;
            }
        }
        foreach (var note in notesToRemove)
            spawner.RemoveNote(note);
    }

    private void EndGame()
    {
        _isPlaying = false;
        spawner.StopSpawning();
        spawner.ClearAll();

        int earnedFee = Mathf.FloorToInt(_score * feeMultiplier);
        Debug.Log($"[Rhythm] 游戏结束! 得分: {_score}, 获得旅费: {earnedFee}");

        if (GameManager.Instance != null)
            GameManager.Instance.AddCurrency(earnedFee);

        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null)
            resultText.text = $"演奏结束!\n得分: {_score}\n获得旅费: {earnedFee}";

        EventBus.Publish(GameEvent.OnRhythmEnd);
    }

    private void ReturnToMainUI()
    {
        spawner.ClearAll();
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToMainUI();
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = $"得分: {_score}";
    }
}
