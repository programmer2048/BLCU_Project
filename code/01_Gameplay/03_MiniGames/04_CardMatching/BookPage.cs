using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BookPage : MonoBehaviour, IDropHandler
{
    public int targetMaidId;
    public Image artDisplay;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        InventoryItem draggedItem = eventData.pointerDrag.GetComponent<InventoryItem>();

        // 检查 ID 是否匹配
        if (draggedItem != null && draggedItem.data.id == targetMaidId)
        {
            if (!MaidGameManager.Instance.repairedMaidIds.Contains(targetMaidId))
            {
                CompleteRepair(draggedItem.data);
                draggedItem.MarkAsConsumed();
            }
        }
    }

    private void CompleteRepair(MaidData data)
    {
        if (artDisplay)
        {
            artDisplay.sprite = data.blueprintSprite;
            artDisplay.color = Color.white;
        }
        MaidGameManager.Instance.OnPhase1MatchSuccess(targetMaidId);
    }

    private void OnEnable()
    {
        if (MaidGameManager.Instance == null) return;

        // 重新翻页时刷新状态
        if (MaidGameManager.Instance.repairedMaidIds.Contains(targetMaidId))
        {
            var data = MaidGameManager.Instance.allMaids.Find(m => m.id == targetMaidId);
            if (data && artDisplay)
            {
                artDisplay.sprite = data.blueprintSprite;
                artDisplay.color = Color.white;
            }
        }
    }
}