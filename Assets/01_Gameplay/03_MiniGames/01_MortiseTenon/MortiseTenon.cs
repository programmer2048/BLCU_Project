using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 榫卯大师 —— 简单拖拽拼图小游戏
/// 将散落的零件拖到正确位置
/// </summary>
public class MortiseTenon : MonoBehaviour
{
    [Header("UI")]
    public RectTransform playArea;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statusText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public Button backButton;

    [Header("设置")]
    public float gameDuration = 60f;
    public int pieceCount = 4;

    private List<DragPiece> _pieces = new List<DragPiece>();
    private List<RectTransform> _targets = new List<RectTransform>();
    private float _timeRemaining;
    private bool _isPlaying;
    private int _completedCount;

    void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBack);
    }

    public void StartGame()
    {
        Debug.Log("[MortiseTenon] 榫卯大师开始！");
        _isPlaying = true;
        _timeRemaining = gameDuration;
        _completedCount = 0;
        if (resultPanel != null) resultPanel.SetActive(false);
        ClearPieces();
        CreatePuzzle();
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

    private void CreatePuzzle()
    {
        Color[] colors = { Color.red, Color.blue, Color.green, Color.yellow };

        for (int i = 0; i < pieceCount; i++)
        {
            // 创建目标位置（占位框）
            GameObject target = new GameObject($"Target_{i}");
            RectTransform trt = target.AddComponent<RectTransform>();
            trt.SetParent(playArea != null ? playArea : transform, false);
            float tx = (i % 2) * 150 - 75;
            float ty = (i / 2) * 150 - 75;
            trt.anchoredPosition = new Vector2(tx, ty);
            trt.sizeDelta = new Vector2(120, 120);
            Image tImg = target.AddComponent<Image>();
            tImg.color = new Color(colors[i].r, colors[i].g, colors[i].b, 0.3f);
            _targets.Add(trt);

            // 创建可拖拽棋子（随机位置）
            GameObject piece = new GameObject($"Piece_{i}");
            RectTransform prt = piece.AddComponent<RectTransform>();
            prt.SetParent(playArea != null ? playArea : transform, false);
            float px = Random.Range(-250f, 250f);
            float py = Random.Range(-350f, -200f);
            prt.anchoredPosition = new Vector2(px, py);
            prt.sizeDelta = new Vector2(100, 100);
            Image pImg = piece.AddComponent<Image>();
            pImg.color = colors[i];

            var drag = piece.AddComponent<DragPiece>();
            drag.targetPosition = trt.anchoredPosition;
            drag.snapDistance = 40f;
            drag.onSnapped = () => OnPieceSnapped();

            // 文字标签
            GameObject label = new GameObject("Label");
            RectTransform lrt = label.AddComponent<RectTransform>();
            lrt.SetParent(prt, false);
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text = $"榫{i + 1}";
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            _pieces.Add(drag);
        }
    }

    private void OnPieceSnapped()
    {
        _completedCount++;
        if (statusText != null)
            statusText.text = $"完成: {_completedCount}/{pieceCount}";

        if (_completedCount >= pieceCount)
            EndGame(true);
    }

    private void EndGame(bool success)
    {
        _isPlaying = false;
        if (resultPanel != null) resultPanel.SetActive(true);

        if (success)
        {
            Debug.Log("[MortiseTenon] 拼图完成！解锁木塔模型摆件");
            if (resultText != null) resultText.text = "拼图完成！\n解锁: 木塔模型摆件";
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddAlbumItem("木塔模型摆件");
                GameManager.Instance.SetSpotExplored(0);
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
        ClearPieces();
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToMainUI();
    }

    private void ClearPieces()
    {
        foreach (var p in _pieces)
            if (p != null) Destroy(p.gameObject);
        _pieces.Clear();
        foreach (var t in _targets)
            if (t != null) Destroy(t.gameObject);
        _targets.Clear();
    }
}

/// <summary>
/// 可拖拽UI零件
/// </summary>
public class DragPiece : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public Vector2 targetPosition;
    public float snapDistance = 40f;
    public System.Action onSnapped;
    public bool isSnapped;

    private RectTransform _rt;
    private Canvas _canvas;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isSnapped) return;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isSnapped) return;
        float scale = _canvas != null ? _canvas.scaleFactor : 1f;
        _rt.anchoredPosition += eventData.delta / scale;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isSnapped) return;
        float dist = Vector2.Distance(_rt.anchoredPosition, targetPosition);
        if (dist <= snapDistance)
        {
            _rt.anchoredPosition = targetPosition;
            isSnapped = true;
            GetComponent<Image>().raycastTarget = false;
            onSnapped?.Invoke();
        }
    }
}
