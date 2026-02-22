using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 时光寻宝 —— 点击找不同
/// 对比两张图，点击找出5处不同
/// </summary>
public class FindDifferences : MonoBehaviour
{
    [Header("UI")]
    public RectTransform playArea;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI timerText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public Button backButton;

    [Header("设置")]
    public int totalDifferences = 5;
    public float gameDuration = 90f;

    private List<GameObject> _diffSpots = new List<GameObject>();
    private int _foundCount;
    private float _timeRemaining;
    private bool _isPlaying;

    void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBack);
    }

    public void StartGame()
    {
        Debug.Log("[FindDifferences] 时光寻宝开始！");
        _isPlaying = true;
        _timeRemaining = gameDuration;
        _foundCount = 0;
        if (resultPanel != null) resultPanel.SetActive(false);
        ClearSpots();
        CreateDifferences();
        UpdateStatus();
    }

    void Update()
    {
        if (!_isPlaying) return;
        _timeRemaining -= Time.deltaTime;
        if (timerText != null)
            timerText.text = $"时间: {Mathf.CeilToInt(_timeRemaining)}s";
        if (_timeRemaining <= 0)
            EndGame(false);
    }

    private void CreateDifferences()
    {
        // 创建两个"图片"区域
        for (int side = 0; side < 2; side++)
        {
            GameObject imgBg = new GameObject($"Photo_{(side == 0 ? "Old" : "New")}");
            RectTransform bgRt = imgBg.AddComponent<RectTransform>();
            bgRt.SetParent(playArea != null ? playArea : transform, false);
            float xOffset = side == 0 ? -200 : 200;
            bgRt.anchoredPosition = new Vector2(xOffset, 0);
            bgRt.sizeDelta = new Vector2(350, 450);
            Image bgImg = imgBg.AddComponent<Image>();
            bgImg.color = side == 0 ? new Color(0.8f, 0.7f, 0.5f) : new Color(0.6f, 0.6f, 0.6f);

            // 标题
            GameObject title = new GameObject("Title");
            RectTransform trt = title.AddComponent<RectTransform>();
            trt.SetParent(bgRt, false);
            trt.anchoredPosition = new Vector2(0, 200);
            trt.sizeDelta = new Vector2(300, 40);
            TextMeshProUGUI tmp = title.AddComponent<TextMeshProUGUI>();
            tmp.text = side == 0 ? "1937年老照片" : "现在的照片";
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        // 在右侧图片上放置可点击的"不同之处"
        for (int i = 0; i < totalDifferences; i++)
        {
            GameObject spot = new GameObject($"DiffSpot_{i}");
            RectTransform srt = spot.AddComponent<RectTransform>();
            srt.SetParent(playArea != null ? playArea : transform, false);
            float sx = 200 + Random.Range(-130f, 130f);
            float sy = Random.Range(-170f, 150f);
            srt.anchoredPosition = new Vector2(sx, sy);
            srt.sizeDelta = new Vector2(50, 50);

            Image sImg = spot.AddComponent<Image>();
            sImg.color = new Color(1f, 0.3f, 0.3f, 0.5f);

            Button btn = spot.AddComponent<Button>();
            int idx = i;
            btn.onClick.AddListener(() => OnSpotClicked(idx, spot));

            _diffSpots.Add(spot);
        }
    }

    private void OnSpotClicked(int index, GameObject spot)
    {
        if (!_isPlaying) return;
        _foundCount++;
        Debug.Log($"[FindDifferences] 找到不同之处 {_foundCount}/{totalDifferences}");

        // 标记为已找到
        var img = spot.GetComponent<Image>();
        if (img != null) img.color = new Color(0.2f, 0.9f, 0.2f, 0.8f);
        spot.GetComponent<Button>().interactable = false;

        UpdateStatus();
        if (_foundCount >= totalDifferences)
            EndGame(true);
    }

    private void UpdateStatus()
    {
        if (statusText != null)
            statusText.text = $"找到: {_foundCount}/{totalDifferences}";
    }

    private void EndGame(bool success)
    {
        _isPlaying = false;
        if (resultPanel != null) resultPanel.SetActive(true);

        if (success)
        {
            Debug.Log("[FindDifferences] 全部找到！解锁佛光寺老照片");
            if (resultText != null) resultText.text = "全部找到！\n解锁: 佛光寺老照片";
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddAlbumItem("佛光寺老照片");
                GameManager.Instance.SetSpotExplored(1);
            }
        }
        else
        {
            if (resultText != null) resultText.text = "时间到!\n再试一次吧";
        }
        EventBus.Publish(GameEvent.OnMiniGameEnd);
    }

    private void OnBack()
    {
        ClearSpots();
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToMainUI();
    }

    private void ClearSpots()
    {
        foreach (var s in _diffSpots)
            if (s != null) Destroy(s.gameObject);
        _diffSpots.Clear();
        // 清理照片背景
        if (playArea != null)
            foreach (Transform child in playArea)
                Destroy(child.gameObject);
    }
}
