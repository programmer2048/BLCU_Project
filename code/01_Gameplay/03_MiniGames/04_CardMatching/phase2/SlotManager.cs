using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using DG.Tweening;

public class SlotManager : MonoBehaviour
{
    public static SlotManager Instance;
    private RectTransform rectTransform;

    public Transform[] slotAnchors; // 7个槽位的预留位置(Transform)
    public List<ItemObject> itemsInSlot = new List<ItemObject>();
    public int capacity = 7;

    [Header("Prefabs")]
    public GameObject fullItemPrefab; // 用于碎片合成后的饰品预设
    public GameObject usableRustRemoverPrefab; // 合成后的可用除锈工具预设
    public Transform toolSpawnPoint; // 除锈工具生成的存放点

    private bool isProcessing = false; // 防止逻辑重叠
    private void Awake()
    {
        Instance = this;
        // 确保一定能拿到 RectTransform
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("SlotManager 必须挂载在拥有 RectTransform 的 UI 物体上！");
        }
    }

    public bool IsPointerOverSlot(Vector2 screenPos)
    {
        // 自动获取 Canvas 上的相机（如果是 Overlay 模式，cam 会是 null，这正是函数需要的）
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

        // 关键：检查点是否在 Rect 范围内
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPos, cam);
    }
    public bool AddItem(ItemObject item)
    {
        if (itemsInSlot.Count >= capacity || isProcessing) return false;
        // 逻辑保持不变：寻找插入位置
        int insertIndex = itemsInSlot.FindLastIndex(i => i.maidId == item.maidId && i.type == item.type);
        if (insertIndex == -1) insertIndex = itemsInSlot.Count;
        else insertIndex++;
        itemsInSlot.Insert(insertIndex, item);

        // 将 Item 的父物体改为卡槽
        item.transform.SetParent(this.transform);

        UpdateSlotVisuals();
        StartCoroutine(LogicSequence());
        return true;
    }

    private void UpdateSlotVisuals()
    {
        for (int i = 0; i < itemsInSlot.Count; i++)
        {
            // 使用 DOTween 移动到对应的槽位锚点
            itemsInSlot[i].transform.DOMove(slotAnchors[i].position, 0.2f);
        }
    }

    private IEnumerator LogicSequence()
    {
        isProcessing = true;
        yield return new WaitForSeconds(0.3f);

        yield return ProcessMatchLogic();

        isProcessing = false;

        // 检查失败
        if (itemsInSlot.Count >= capacity)
        {
            Debug.Log("Game Over - 卡槽满了");
        }
    }
    public IEnumerator ProcessMatchLogic()
    {
        bool hasChanged = true;
        while (hasChanged)
        {
            hasChanged = false;

            // 除锈碎片合成工具 (3并1)
            var removers = itemsInSlot.Where(i => i.type == ItemType.RustRemover).ToList();
            if (removers.Count >= 3)
            {
                ClearItems(removers.Take(3).ToList());
                GenerateUsableTool();
                hasChanged = true;
                yield return new WaitForSeconds(0.2f);
            }

            // 碎片合成器具 (4并1)
            var fragGroup = itemsInSlot.Where(i => i.type == ItemType.Fragment)
                                       .GroupBy(i => i.maidId)
                                       .FirstOrDefault(g => g.Count() >= 4);
            if (fragGroup != null)
            {
                int mId = fragGroup.Key;
                ClearItems(fragGroup.Take(4).ToList());

                // 合成的是“器具”，通过 maidId 获取该侍女对应的器具图片
                SpawnApplianceInSlot(mId);

                hasChanged = true;
                yield return new WaitForSeconds(0.2f);
            }

            // 器具/饰品消除 (3并1)
            var matchGroup = itemsInSlot.Where(i => i.type == ItemType.FullItem && !i.isRusted)
                                        .GroupBy(i => i.maidId)
                                        .FirstOrDefault(g => g.Count() >= 3);
            if (matchGroup != null)
            {
                int mId = matchGroup.Key;
                ClearItems(matchGroup.Take(3).ToList());
                BlueprintManager.Instance.AddProgress(mId);
                hasChanged = true;
                yield return new WaitForSeconds(0.2f);
            }
            UpdateSlotVisuals();
        }
    }

    private void SpawnApplianceInSlot(int maidId)
    {
        GameObject go = Instantiate(fullItemPrefab, this.transform);
        ItemObject io = go.GetComponent<ItemObject>();
        io.maidId = maidId;
        io.type = ItemType.FullItem; // 合成后变为完整物品
        io.isRusted = false;
        io.isInSlot = true;

        // 假设 Maid 类里有一个 applianceSprite 字段
        var maidData = MaidGameManager.Instance.allMaids.Find(m => m.id == maidId);
        if (maidData != null)
        {
            io.iconImage.sprite = maidData.iconSprite;
        }

        io.UpdateVisual(false);
        itemsInSlot.Add(io);
    }

    private void ClearItems(List<ItemObject> targets)
    {
        foreach (var item in targets)
        {
            itemsInSlot.Remove(item);
            item.transform.DOScale(0, 0.2f).OnComplete(() => Destroy(item.gameObject));
        }
    }

    private void GenerateUsableTool()
    {
        // 实例化工具到指定的工具位
        GameObject toolGo = Instantiate(usableRustRemoverPrefab, toolSpawnPoint);
        toolGo.transform.localScale = Vector3.zero;
        toolGo.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
    }
    public void ResetSlot()
    {
        StopAllCoroutines();
        isProcessing = false;
        foreach (var item in itemsInSlot)
        {
            if (item != null) Destroy(item.gameObject);
        }
        itemsInSlot.Clear();
        Debug.Log("槽位已清空，准备重新开始");
    }
}