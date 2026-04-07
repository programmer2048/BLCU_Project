using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ItemObject : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int maidId;
    public ItemType type;
    public bool isRusted;

    [Header("Visual Config")]
    public Sprite normalFrame;
    public Sprite rustedFrame;
    public Sprite fragmentFrame;
    public Sprite removerFrame;

    [Header("Internal References")]
    public Image frameImage;
    public Image iconImage;
    public CanvasGroup canvasGroup; // 需要在Inspector添加该组件

    [HideInInspector] public bool isInSlot = false;
    private Vector3 originalPos;
    private Transform originalParent;

    private void Awake()
    {
        if (iconImage != null) iconImage.preserveAspect = true;
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void UpdateVisual(bool useAnimation)
    {
        if (frameImage == null) return;

        // 1. 根据类型和状态切换背景框
        if (isRusted)
        {
            frameImage.sprite = rustedFrame;
        }
        else
        {
            switch (type)
            {
                case ItemType.FullItem:
                    frameImage.sprite = normalFrame;
                    break;
                case ItemType.Fragment:
                    frameImage.sprite = fragmentFrame;
                    break;
                case ItemType.RustRemover:
                    frameImage.sprite = removerFrame;
                    break;
            }
        }

        // 刷新缩放
        if (useAnimation)
        {
            transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
        }

        // 处理碎片 icon 的特殊显示
        if (iconImage != null)
        {
            iconImage.color = isRusted ? new Color(0.5f, 0.5f, 0.5f, 1f) : Color.white;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isInSlot) return;

        // 如果生锈，不允许拖拽并抖动提示
        if (isRusted)
        {
            transform.DOShakePosition(0.4f, new Vector3(10, 0, 0));
            eventData.pointerDrag = null; // 强行结束拖拽
            return;
        }

        originalPos = transform.position;
        originalParent = transform.parent;

        // 提升层级：移动到 Canvas 的最底层（显示在最前）
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false; // 拖拽时忽略自身射线，否则无法检测到下方的物体
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isInSlot) return;

        // 这种方式兼容所有 Canvas 渲染模式（Overlay/Camera/World）
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);

        transform.localPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isInSlot) return;

        // 临时关闭射线阻挡，确保检测的是下方的 Slot
        canvasGroup.blocksRaycasts = true;

        // 检查鼠标位置下方的所有 UI 物体
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        bool droppedOnSlot = false;
        foreach (var result in results)
        {
            if (result.gameObject.GetComponent<SlotManager>() != null || result.gameObject.name == "SlotArea")
            {
                droppedOnSlot = true;
                break;
            }
        }

        if (!droppedOnSlot)
        {
            droppedOnSlot = SlotManager.Instance.IsPointerOverSlot(eventData.position);
        }

        if (droppedOnSlot)
        {
            if (SlotManager.Instance.AddItem(this))
            {
                isInSlot = true;
                // 进入槽位后，确保旋转归零，取消所有正在进行的动画
                transform.DOKill();
                transform.DORotate(Vector3.zero, 0.2f);
                return;
            }
        }

        // 如果没进入槽位，回弹到原始父物体的位置
        transform.SetParent(originalParent);
        transform.DOMove(originalPos, 0.3f).SetEase(Ease.OutBack);
    }

    public void RemoveRust()
    {
        if (!isRusted) return;
        isRusted = false;
        UpdateVisual(true);
        transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
    }
}