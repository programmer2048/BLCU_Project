using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections.Generic;

public class RustTool : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector3 startPos;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPos = transform.position;
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false; // 穿透自身去检测下方的 Item
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // 射线检测鼠标下方的所有 UI
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            ItemObject item = result.gameObject.GetComponentInParent<ItemObject>();
            // 只有生锈的、且不在卡槽里的物品可以被除锈
            if (item != null && item.isRusted && !item.isInSlot)
            {
                item.RemoveRust();
                // 瓶子使用后的反馈动画
                transform.DOScale(0, 0.2f).OnComplete(() => Destroy(gameObject));
                return;
            }
        }

        // 没用到正确位置，弹回工具架
        transform.DOMove(startPos, 0.4f).SetEase(Ease.OutQuad);
    }
}