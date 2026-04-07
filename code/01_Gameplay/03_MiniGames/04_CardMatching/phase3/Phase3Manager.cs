using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MultiScratchManager : MonoBehaviour
{
    [Header("UI Containers")]
    public Transform cardGrid;
    public Button btnPrev;
    public Button btnNext;

    [Header("Prefabs")]
    public GameObject cardPrefab;
    public GameObject ornamentPrefab;
    public Transform leftSidebar;
    public Transform rightSidebar;

    [Header("Brush Settings")]
    public RectTransform brushCursor;
    public Image brushIconImage;
    public float maxBrushVisualSize = 120f;
    public Texture2D brushAlphaTex;
    public float brushDrawRadius = 30f;

    [Header("Progress Tracking")]
    public int totalCompleted = 0; // 完成的总数

    private List<ScratchCard> spawnedCards = new List<ScratchCard>();
    private MaidData selectedData = null;
    private int startIndex = 0;
    private const int VISIBLE_COUNT = 4;

    void Start()
    {
        brushIconImage.enabled = false;
        CanvasGroup cg = brushCursor.GetComponent<CanvasGroup>() ?? brushCursor.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;

        InitializeLevel();

        btnPrev.onClick.AddListener(() => ChangePage(-1));
        btnNext.onClick.AddListener(() => ChangePage(1));

        RefreshDisplay();
    }

    void InitializeLevel()
    {
        for (int i = 0; i < MaidGameManager.Instance.allMaids.Count; i++)
        {
            MaidData data = MaidGameManager.Instance.allMaids[i];
            GameObject cGo = Instantiate(cardPrefab, cardGrid);
            ScratchCard card = cGo.GetComponent<ScratchCard>();
            card.Init(data, 512);
            spawnedCards.Add(card);

            Transform side = (i < 4) ? leftSidebar : rightSidebar;
            GameObject oGo = Instantiate(ornamentPrefab, side);
            oGo.GetComponent<OrnamentItem>().Init(data, this);
        }
    }

    // 当某张卡片完成时调用
    public void OnCardFinished(int maidId)
    {
        totalCompleted++;
        Debug.Log($"卡片 {maidId} 已完成！总完成进度: {totalCompleted}/{MaidGameManager.Instance.allMaids.Count}");

        // 如果全部 8 张都完成了
        if (totalCompleted >= MaidGameManager.Instance.allMaids.Count)
        {
            OnAllCardsFinished();
        }
    }

    private void OnAllCardsFinished()
    {
        Debug.Log("<color=cyan>Phase 3 完成！进入明信片环节。</color>");

        // 延迟一小会儿体验更好
        Invoke(nameof(TriggerNextPhase), 1.0f);
    }
    void TriggerNextPhase()
    {
        Debug.Log("Phase 3 完成，准备进入 Phase 4...");
        // 禁用自身防止重复触发
        this.enabled = false;
        MaidGameManager.Instance.UnlockNextPhase(4);
        MaidGameManager.Instance.ChangePhase(4);
    }

    void Update()
    {
        brushCursor.position = Input.mousePosition;
        if (selectedData == null) return;

        ScratchCard hoveredCard = spawnedCards.Find(c => c.gameObject.activeSelf && c.IsMouseOver());

        if (Input.GetMouseButton(0) && hoveredCard != null)
        {
            // 如果卡片已经完成了，就不再涂抹
            if (hoveredCard.IsFinished()) return;

            if (hoveredCard.GetData().id == selectedData.id)
            {
                if (hoveredCard.GetHitUV(Input.mousePosition, out Vector2 currentUV))
                {
                    hoveredCard.DrawAt(currentUV, brushDrawRadius, brushAlphaTex);
                }
            }
        }
    }

    public void ChangePage(int direction)
    {
        startIndex += direction * VISIBLE_COUNT;
        if (startIndex >= MaidGameManager.Instance.allMaids.Count) startIndex = 0;
        else if (startIndex < 0) startIndex = MaidGameManager.Instance.allMaids.Count - VISIBLE_COUNT;
        RefreshDisplay();
    }

    void RefreshDisplay()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            bool isVisible = (i >= startIndex && i < startIndex + VISIBLE_COUNT);
            spawnedCards[i].gameObject.SetActive(isVisible);
            if (!isVisible) spawnedCards[i].ResetHover();
        }
    }

    public void SelectOrnament(MaidData data)
    {
        selectedData = data;
        brushIconImage.enabled = true;
        brushIconImage.sprite = data.iconSprite;
        SetBrushSize(data.iconSprite);
    }

    void SetBrushSize(Sprite sp)
    {
        float ratio = sp.rect.width / sp.rect.height;
        if (ratio > 1) brushIconImage.rectTransform.sizeDelta = new Vector2(maxBrushVisualSize, maxBrushVisualSize / ratio);
        else brushIconImage.rectTransform.sizeDelta = new Vector2(maxBrushVisualSize * ratio, maxBrushVisualSize);
    }
}