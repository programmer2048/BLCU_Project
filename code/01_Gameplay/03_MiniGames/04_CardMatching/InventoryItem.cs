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

        // 如果被 BookPage 消耗掉了，就销毁自己
        if (isConsumed)
        {
            Destroy(gameObject);
            return;
        }
        if (originalParent != null)
        {
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);
            transform.localScale = Vector3.one;
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