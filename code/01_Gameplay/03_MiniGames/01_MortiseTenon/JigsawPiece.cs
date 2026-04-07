using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class JigsawPiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("设置")]
    private float snapDistance = 30f; // 吸附距离阈值

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 correctPosition; // 记录正确的位置
    private bool isLocked = false;   // 是否已经拼好

    // 用于处理不规则点击区域
    private Image image;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        correctPosition = rectTransform.localPosition;
        image.alphaHitTestMinimumThreshold = 0.1f;
    }

    void Start()
    {
        ScatterPiece();
    }

    private void ScatterPiece()
    {
        float rangeX = 400f;
        float rangeY = 100f;
        float randX = Random.Range(-rangeX, rangeX);
        float randY = Random.Range(-rangeY, rangeY);

        rectTransform.localPosition = new Vector3(correctPosition.x + randX, correctPosition.y + randY, 0);
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked) return;

        // 拖动时稍微变透明，且不再阻挡射线（方便穿透）
        canvasGroup.alpha = 0.8f;
        canvasGroup.blocksRaycasts = false;

        // 提到最上层，防止被其他拼图遮挡
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked) return;

        rectTransform.anchoredPosition += eventData.delta / GetComponentInParent<Canvas>().scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isLocked) return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        CheckPosition();
    }

    // --- 核心判定 ---

    private void CheckPosition()
    {
        // 计算当前位置和正确位置的距离
        float distance = Vector3.Distance(rectTransform.localPosition, correctPosition);

        if (distance <= snapDistance)
        {
            // 吸附
            rectTransform.localPosition = correctPosition;
            isLocked = true;

            // 锁定后不可交互，防止误触
            canvasGroup.blocksRaycasts = false;

            // 播放音效（可选）
            // AudioManager.Play("SnapSound");

            // 通知管理器
            JigsawManager.Instance.OnPieceLocked();
        }
    }
}