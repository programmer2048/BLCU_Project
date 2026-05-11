using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public MaidData data;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    // --- 状态记录 ---
    private Transform originalParent;
    private int originalSiblingIndex;
    private Transform dragLayerParent; // 专门用于拖拽的临时父节点
    private bool isConsumed = false;

    // 宽高比适配
    private Vector2 maxContainerSize;
    private bool sizeInitialized = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (rectTransform != null)
        {
            maxContainerSize = rectTransform.sizeDelta;
            sizeInitialized = true;
        }
    }

    public void Setup(MaidData d)
    {
        data = d;
        Image img = GetComponent<Image>();
        img.sprite = d.iconSprite;

        if (!rectTransform) rectTransform = GetComponent<RectTransform>();
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();

        if (!sizeInitialized)
        {
            maxContainerSize = rectTransform.sizeDelta;
            sizeInitialized = true;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        isConsumed = false;

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null && rootCanvas.rootCanvas != null)
        {
            dragLayerParent = rootCanvas.rootCanvas.transform;
        }
        else
        {
            dragLayerParent = MaidUIManager.Instance.transform;
        }

        transform.SetParent(dragLayerParent, true);
        transform.SetAsLastSibling();

        transform.localScale = Vector3.one;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 直接设置世界坐标
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            dragLayerParent as RectTransform, // 使用当前父节点的 Rect
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 globalMousePos))
        {
            rectTransform.position = globalMousePos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // 如果被消耗了（比如放进了正确的槽位）
        if (isConsumed)
        {
            Destroy(gameObject);
            return;
        }

        // 拖拽失败，退回原位
        if (originalParent != null)
        {
            // 【关键修改 1】：传 false！不要保持世界坐标，让 LayoutGroup 完全重新接管排版
            transform.SetParent(originalParent, false);

            // 恢复层级顺序
            transform.SetSiblingIndex(originalSiblingIndex);

            // 【关键修改 2】：强制恢复原本的 UI 尺寸，防止被 LayoutGroup 瞬间压扁成 0 导致无法被射线检测（点不到）
            if (sizeInitialized && rectTransform != null)
            {
                rectTransform.sizeDelta = maxContainerSize;
            }
            transform.localScale = Vector3.one;

            // 【关键修改 3】：通知父物体的布局组立刻刷新排版，不要等下一帧
            UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(originalParent as RectTransform);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void MarkAsConsumed()
    {
        isConsumed = true;
        canvasGroup.alpha = 0f;
    }
}