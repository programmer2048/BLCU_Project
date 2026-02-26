using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using TMPro;
using System.IO;
using UnityEngine.InputSystem.Controls;

public class RhythmGameManager : MonoBehaviour
{
    // ... (保留之前的 Header 和变量定义) ...
    [Header("资源设置")]
    public AudioSource musicSource;
    public AudioClip musicClip;
    public TextAsset jsonFile;

    [Header("轨道配置")]
    public Transform[] laneContainers;
    public UIString[] strings;
    public UnityEngine.UI.Image[] laneTouchFeedbacks;
    public RectTransform hitLineReference;

    [Header("UI & 反馈")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public RectTransform effectCanvasLayer;

    public float noteAppearDistance = 1200f;

    [Header("预制体")]
    public GameObject tapNotePrefab;
    public GameObject holdNotePrefab;
    public GameObject trapNotePrefab; // [新增] 陷阱预制体
    public GameObject feedbackPrefab;
    public GameObject pulsePrefab;
    public GameObject phantomNotePrefab;

    // --- 内部变量 ---
    private int currentScore = 0;
    private int combo = 0;
    private int scorePerPerfect = 100;
    private int scorePerGood = 50;
    private List<ChartNote> allNotes = new List<ChartNote>();
    private List<NoteObject> activeNotes = new List<NoteObject>();
    private int nextNoteIndex = 0;
    private ChartJSON currentChart;
    private double dspSongStartTime;
    private float secPerBeat;
    private bool isGameRunning = false;
    private readonly float startDelay = 2.0f;
    public float songPosition { get; private set; }
    public float songPositionInBeats { get; private set; }
    public float pixelsPerBeat { get; private set; }
    private float hitLineY;
    private float perfectDist = 60f;
    private Coroutine comboAnimCoroutine;

    void Start() { InitializeGame(); }

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

    // ... (LoadResourcesFromDisk, LoadAudioRoutine, OnReadyToPlay, StartGameplay 保持不变) ...
    IEnumerator LoadResourcesFromDisk() { /*略，保持原样*/ yield break; }
    IEnumerator LoadAudioRoutine(string f) { /*略，保持原样*/ yield break; }
    void OnReadyToPlay() { StartGameplay(); }
    public void StartGameplay()
    {
        double outputLatency = AudioSettings.GetConfiguration().dspBufferSize / (double)AudioSettings.outputSampleRate;
        dspSongStartTime = AudioSettings.dspTime + startDelay + outputLatency;
        musicSource.PlayScheduled(dspSongStartTime);
        isGameRunning = true;
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
        if (!isGameRunning) return;

        songPosition = (float)(AudioSettings.dspTime - dspSongStartTime);
        songPositionInBeats = songPosition / secPerBeat;

        while (nextNoteIndex < allNotes.Count && allNotes[nextNoteIndex].beat < songPositionInBeats + 4.0f)
        {
            SpawnNoteObject(allNotes[nextNoteIndex]);
            nextNoteIndex++;
        }

        HandleInput();
        UpdateActiveNotes();
    }

    // --- [修改] 支持 Trap 生成 ---
    void SpawnNoteObject(ChartNote data)
    {
        if (data.lane < 0 || data.lane >= laneContainers.Length) return;

        GameObject prefab = tapNotePrefab;
        R_NoteType type = R_NoteType.Tap;

        if (data.type == "hold")
        {
            prefab = holdNotePrefab;
            type = R_NoteType.Hold;
        }
        else if (data.type == "trap") // [新增] 陷阱逻辑
        {
            prefab = trapNotePrefab;
            type = R_NoteType.Trap;
        }

        if (prefab == null) return;

        GameObject obj = Instantiate(prefab, laneContainers[data.lane]);
        NoteObject note = obj.GetComponent<NoteObject>();

        note.Init(this, data.beat, data.lane, type, data.duration, hitLineY);
        activeNotes.Add(note);
    }

    // --- [修改] 陷阱不做 Miss 处理 ---
    void UpdateActiveNotes()
    {
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            NoteObject note = activeNotes[i];
            if (note == null) { activeNotes.RemoveAt(i); continue; }
            float distToLine = (note.targetBeat - songPositionInBeats) * pixelsPerBeat;

            // 越过判定线
            if (distToLine < -perfectDist - 20f)
            {
                if (note.type == R_NoteType.Trap)
                {
                    // [新增] 陷阱如果漏过去了，是好事，直接销毁，不中断连击
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
        CheckLaneInput(0, Keyboard.current.dKey);
        CheckLaneInput(1, Keyboard.current.fKey);
        CheckLaneInput(2, Keyboard.current.jKey);
        CheckLaneInput(3, Keyboard.current.kKey);
        CheckLaneInput(4, Keyboard.current.lKey);
    }

    void CheckLaneInput(int lane, KeyControl key)
    {
        // --- 按下逻辑 ---
        if (key.wasPressedThisFrame)
        {
            TriggerLaneVisuals(lane);
            NoteObject target = GetClosestHittableNote(lane);

            if (target != null)
            {
                float currentDist = Mathf.Abs((target.targetBeat - songPositionInBeats) * pixelsPerBeat);

                // 判定范围内
                if (currentDist <= perfectDist * 2.5f)
                {
                    // [新增] 陷阱判定：如果按到了陷阱，触发惩罚
                    if (target.type == R_NoteType.Trap)
                    {
                        OnTrapHit(target);
                    }
                    else
                    {
                        // 普通音符判定
                        bool isPerfect = currentDist <= perfectDist * 1.2f;
                        OnNoteHit(target, isPerfect);
                    }
                }
            }
        }

        // --- [修改] 松手逻辑：恢复严格判定 ---
        if (key.wasReleasedThisFrame)
        {
            FadeOutLaneVisuals(lane);
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                NoteObject note = activeNotes[i];
                if (note.laneIndex == lane && note.type == R_NoteType.Hold && note.isHolding)
                {
                    float endBeat = note.targetBeat + note.holdDuration;
                    float diff = Mathf.Abs(songPositionInBeats - endBeat);

                    // [恢复] 严格的判定
                    // 必须在结束时间点附近松手 (误差 < 0.15拍) 且 不能提前太多
                    // 如果当前时间已经超过结束时间太久，通常会自动完成或者Miss，这里处理的是玩家主动松手

                    if (songPositionInBeats < endBeat - 0.15f)
                    {
                        // 松手太早 (未到结束点)
                        OnNoteMiss(note);
                    }
                    else
                    {
                        // 在结束点附近松手，完美
                        OnHoldComplete(note);
                    }
                }
            }
        }
    }

    // --- 视觉反馈 ---

    void TriggerLaneVisuals(int lane)
    {
        float pluckRatio = (lane * 2 + 1) / 10f;
        if (lane < strings.Length && strings[lane])
            strings[lane].Pluck(pluckRatio, 800f);

        if (lane < laneTouchFeedbacks.Length && laneTouchFeedbacks[lane])
            laneTouchFeedbacks[lane].canvasRenderer.SetAlpha(0.6f);

        SpawnPulseEffect(lane);
        SpawnPhantomNote(lane);
    }

    void FadeOutLaneVisuals(int lane)
    {
        if (lane < laneTouchFeedbacks.Length && laneTouchFeedbacks[lane])
            laneTouchFeedbacks[lane].CrossFadeAlpha(0f, 0.2f, false);
    }

    void OnNoteHit(NoteObject note, bool isPerfect)
    {
        combo++;
        UpdateComboUI();
        if (note.type == R_NoteType.Tap)
        {
            note.TriggerHit();
            int score = isPerfect ? scorePerPerfect : scorePerGood;
            string text = isPerfect ? "妙" : "佳";
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

    // [新增] 陷阱命中逻辑
    void OnTrapHit(NoteObject note)
    {
        combo = 0; // 断连
        UpdateComboUI();

        // 显示红色警告
        ShowFeedback("陷", Color.red, GetLaneWorldPos(note.laneIndex));

        // 可选：扣分
        AddScore(-50);

        note.TriggerHit(); // 播放消失动画
        RemoveActiveNote(note);
    }

    public void OnNoteMiss(NoteObject note)
    {
        combo = 0;
        UpdateComboUI();
        Vector3 pos = note != null ? note.rectTrans.position : GetLaneWorldPos(note.laneIndex);
        ShowFeedback("漏", Color.gray, pos);
        RemoveActiveNote(note);
        if (note != null) Destroy(note.gameObject);
    }

    public void OnHoldComplete(NoteObject note)
    {
        combo++;
        UpdateComboUI();
        Vector3 endPos = GetLaneWorldPos(note.laneIndex);
        // "绝" 的反馈
        ShowFeedback("绝", new Color(1f, 0.9f, 0.3f), endPos);
        AddScore(scorePerPerfect);
        RemoveActiveNote(note);
        if (note != null) Destroy(note.gameObject);
    }

    // ... (AddScore, UpdateScoreUI, UpdateComboUI, AnimateComboText 保持不变) ...
    void AddScore(int val) { currentScore += val; UpdateScoreUI(); }
    void UpdateScoreUI() { if (scoreText) scoreText.text = $"旅费: {currentScore}"; }
    void UpdateComboUI() { if (comboText) { if (combo > 1) { comboText.text = $"{combo} 连"; comboText.gameObject.SetActive(true); if (comboAnimCoroutine != null) StopCoroutine(comboAnimCoroutine); comboAnimCoroutine = StartCoroutine(AnimateComboText()); } else comboText.gameObject.SetActive(false); } }
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

    // --- [重点修改] 精确计算分位点生成 PhantomNote ---
    void SpawnPhantomNote(int lane)
    {
        if (!phantomNotePrefab || !effectCanvasLayer) return;

        // 1. 获取对应的弦
        RectTransform stringRect = null;
        if (lane < strings.Length && strings[lane] != null)
        {
            stringRect = strings[lane].GetComponent<RectTransform>();
        }

        Vector3 targetWorldPos;

        if (stringRect != null)
        {
            // [核心算法]：获取弦的世界角点，进行线性插值
            // Pluck 的逻辑是 (lane * 2 + 1) / 10f
            // 假设 strings[lane] 代表该轨道的弦对象
            float ratio = (lane * 2 + 1) / 10f;

            // 获取四个角点：0=左下, 1=左上, 2=右上, 3=右下
            Vector3[] corners = new Vector3[4];
            stringRect.GetWorldCorners(corners);

            // 计算左边缘的中心点和右边缘的中心点
            Vector3 leftCenter = (corners[0] + corners[1]) * 0.5f;
            Vector3 rightCenter = (corners[3] + corners[2]) * 0.5f;

            // 在左右之间根据 ratio 进行插值
            // 这样无论弦是横着放、竖着放还是斜着放，点都在弦上对应的比例位置
            targetWorldPos = Vector3.Lerp(leftCenter, rightCenter, ratio);
        }
        else
        {
            // 备用方案：没有弦对象时，使用轨道位置
            targetWorldPos = GetLaneWorldPos(lane);
            targetWorldPos.y -= 300f;
        }

        GameObject obj = Instantiate(phantomNotePrefab, effectCanvasLayer);
        Canvas canvas = effectCanvasLayer.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;

        // 2. 坐标转换
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, targetWorldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            effectCanvasLayer,
            screenPoint,
            uiCamera,
            out Vector2 localPoint
        );

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = localPoint;
        rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        PhantomNote script = obj.GetComponent<PhantomNote>();
        string[] scaleChars = { "宫", "商", "角", "徵", "羽" };
        Color[] smokeColors = {
            new Color(1f, 0.4f, 0.4f),
            new Color(1f, 0.8f, 0.2f),
            new Color(0.4f, 1f, 0.4f),
            new Color(0.2f, 0.8f, 1f),
            new Color(0.8f, 0.4f, 1f)
        };
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
            // 排除已经打过的，排除正在长按的(按下的瞬间)，
            // 如果是 Trap，也在检查范围内
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