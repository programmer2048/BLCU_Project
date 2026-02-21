using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 侍女翻牌 —— 4对翻牌配对小游戏
/// </summary>
public class CardMatching : MonoBehaviour
{
    [Header("UI")]
    public RectTransform playArea;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI attemptsText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public Button backButton;

    [Header("设置")]
    public int pairCount = 4; // 4对 = 8张牌
    public float peekTime = 0.8f;

    private List<CardData> _cards = new List<CardData>();
    private CardData _firstFlipped;
    private CardData _secondFlipped;
    private int _matchedCount;
    private int _attempts;
    private bool _isPlaying;
    private bool _isChecking;

    private readonly string[] CardNames = { "持巾侍女", "持盘侍女", "抱琴侍女", "执扇侍女" };
    private readonly Color[] CardColors = {
        new Color(0.9f, 0.3f, 0.3f),
        new Color(0.3f, 0.7f, 0.3f),
        new Color(0.3f, 0.3f, 0.9f),
        new Color(0.9f, 0.7f, 0.2f)
    };

    void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBack);
    }

    public void StartGame()
    {
        Debug.Log("[CardMatching] 侍女翻牌开始！");
        _isPlaying = true;
        _matchedCount = 0;
        _attempts = 0;
        _firstFlipped = null;
        _secondFlipped = null;
        if (resultPanel != null) resultPanel.SetActive(false);
        ClearCards();
        CreateCards();
        UpdateStatus();
    }

    private void CreateCards()
    {
        // 创建8张牌 (4对)
        List<int> types = new List<int>();
        for (int i = 0; i < pairCount; i++)
        {
            types.Add(i);
            types.Add(i);
        }

        // 洗牌
        for (int i = types.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = types[i];
            types[i] = types[j];
            types[j] = temp;
        }

        int cols = 4;
        float cardW = 90f, cardH = 120f, gap = 10f;
        float totalW = cols * (cardW + gap);

        for (int i = 0; i < types.Count; i++)
        {
            int col = i % cols;
            int row = i / cols;

            GameObject cardObj = new GameObject($"Card_{i}");
            RectTransform rt = cardObj.AddComponent<RectTransform>();
            rt.SetParent(playArea != null ? playArea : transform, false);
            float x = col * (cardW + gap) - totalW / 2f + cardW / 2f;
            float y = -row * (cardH + gap) + cardH / 2f;
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(cardW, cardH);

            Image img = cardObj.AddComponent<Image>();
            img.color = new Color(0.5f, 0.5f, 0.5f); // 背面颜色

            Button btn = cardObj.AddComponent<Button>();

            // 牌面文字（初始隐藏）
            GameObject label = new GameObject("Label");
            RectTransform lrt = label.AddComponent<RectTransform>();
            lrt.SetParent(rt, false);
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(5, 5);
            lrt.offsetMax = new Vector2(-5, -5);
            TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text = CardNames[types[i]];
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            label.SetActive(false);

            var cardData = new CardData
            {
                type = types[i],
                cardObject = cardObj,
                image = img,
                label = label,
                faceColor = CardColors[types[i]],
                isFlipped = false,
                isMatched = false
            };
            _cards.Add(cardData);

            var cd = cardData;
            btn.onClick.AddListener(() => OnCardClicked(cd));
        }
    }

    private void OnCardClicked(CardData card)
    {
        if (!_isPlaying || _isChecking || card.isFlipped || card.isMatched) return;

        // 翻牌
        FlipCard(card, true);

        if (_firstFlipped == null)
        {
            _firstFlipped = card;
        }
        else
        {
            _secondFlipped = card;
            _attempts++;
            UpdateStatus();
            StartCoroutine(CheckMatch());
        }
    }

    private IEnumerator CheckMatch()
    {
        _isChecking = true;
        yield return new WaitForSeconds(peekTime);

        if (_firstFlipped.type == _secondFlipped.type)
        {
            // 配对成功
            _firstFlipped.isMatched = true;
            _secondFlipped.isMatched = true;
            _matchedCount++;
            Debug.Log($"[CardMatching] 配对成功! {CardNames[_firstFlipped.type]}");

            if (_matchedCount >= pairCount)
                EndGame(true);
        }
        else
        {
            // 配对失败，翻回去
            FlipCard(_firstFlipped, false);
            FlipCard(_secondFlipped, false);
        }

        _firstFlipped = null;
        _secondFlipped = null;
        _isChecking = false;
    }

    private void FlipCard(CardData card, bool faceUp)
    {
        card.isFlipped = faceUp;
        card.image.color = faceUp ? card.faceColor : new Color(0.5f, 0.5f, 0.5f);
        card.label.SetActive(faceUp);
    }

    private void UpdateStatus()
    {
        if (statusText != null)
            statusText.text = $"配对: {_matchedCount}/{pairCount}";
        if (attemptsText != null)
            attemptsText.text = $"尝试: {_attempts}";
    }

    private void EndGame(bool success)
    {
        _isPlaying = false;
        if (resultPanel != null) resultPanel.SetActive(true);

        if (success)
        {
            Debug.Log("[CardMatching] 配对完成！解锁侍女明信片");
            if (resultText != null) resultText.text = $"配对完成！\n尝试次数: {_attempts}\n解锁: 侍女明信片";
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddAlbumItem("侍女明信片");
                GameManager.Instance.SetSpotExplored(3);
            }
        }
        EventBus.Publish(GameEvent.OnMiniGameEnd);
    }

    private void OnBack()
    {
        ClearCards();
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToMainUI();
    }

    private void ClearCards()
    {
        foreach (var c in _cards)
            if (c.cardObject != null) Destroy(c.cardObject);
        _cards.Clear();
    }

    private class CardData
    {
        public int type;
        public GameObject cardObject;
        public Image image;
        public GameObject label;
        public Color faceColor;
        public bool isFlipped;
        public bool isMatched;
    }
}
