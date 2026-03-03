using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BookPage : MonoBehaviour, IDropHandler
{
    public int targetMaidId;
    public Image artDisplay;

    public void OnDrop(PointerEventData eventData)
    {
        InventoryItem draggedItem = eventData.pointerDrag.GetComponent<InventoryItem>();
        if (draggedItem != null && draggedItem.data.id == targetMaidId)
        {
            if (!MaidGameManager.Instance.repairedMaidIds.Contains(targetMaidId))
            {
                CompleteRepair(draggedItem.data);
                Destroy(draggedItem.gameObject);
            }
        }
    }

    private void CompleteRepair(MaidData data)
    {
        artDisplay.sprite = data.blueprintSprite;
        artDisplay.color = Color.white;
        MaidGameManager.Instance.OnPhase1MatchSuccess(targetMaidId);
    }

    private void OnEnable()
    {
        // 重新打开书本时刷新状态
        if (MaidGameManager.Instance.repairedMaidIds.Contains(targetMaidId))
        {
            var data = MaidGameManager.Instance.allMaids.Find(m => m.id == targetMaidId);
            if (data) { artDisplay.sprite = data.blueprintSprite; artDisplay.color = Color.white; }
        }
    }
}