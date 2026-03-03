using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System;

public class MaidUIManager : MonoBehaviour
{
    public static MaidUIManager Instance;

    [Header("Root References")]
    public GameObject[] phaseRoots; // 索引0=第一阶段, 1=第二阶段, 2=第三阶段

    [Header("Shared Navigation")]
    public Button[] phaseButtons;
    public Image[] connections;
    public GameObject globalProgressPanel;

    [Header("Phase 1 UI")]
    public Transform inventoryContainer;
    public GameObject inventoryItemPrefab;
    public GameObject flyingIconPrefab;
    public GameObject bookRoot;

    [Header("Phase 2 UI (Placeholders)")]
    public GameObject slotManagerUI; // 卡槽视图
    public GameObject blueprintScrollView; // 右侧蓝图列表

    private void Awake() { Instance = this; }

    private void Start()
    {
        if (bookRoot) bookRoot.SetActive(false);
        SwitchPhaseUI(1);
        RefreshNavigationUI();
    }

    public void SwitchPhaseUI(int phaseIndex)
    {
        for (int i = 0; i < phaseRoots.Length; i++)
        {
            if (phaseRoots[i] != null)
                // phaseIndex 传入 1-4，数组索引 0-3
                phaseRoots[i].SetActive((i + 1) == phaseIndex);
        }
        // 特殊处理：如果是 Phase 4，初始化它的控制器
        if (phaseIndex == 4)
        {
            FinalPhaseController finalCtrl = phaseRoots[3].GetComponent<FinalPhaseController>();
            if (finalCtrl != null) finalCtrl.InitPhase();
        }
    }

    #region Phase 1 Animations & Inventory

    public void PlayCollectAnimation(MaidData data, Vector3 startPos)
    {
        StartCoroutine(FlyToSlotRoutine(data, startPos));
    }

    private IEnumerator FlyToSlotRoutine(MaidData data, Vector3 startPos)
    {
        GameObject flyObj = Instantiate(flyingIconPrefab, this.transform);
        flyObj.transform.position = startPos;
        Image img = flyObj.GetComponent<Image>();
        img.sprite = data.iconSprite;
        img.SetNativeSize();

        float elapsed = 0;
        float duration = 0.5f;
        while (elapsed < duration)
        {
            flyObj.transform.position = Vector3.Lerp(startPos, inventoryContainer.position, elapsed / duration);
            flyObj.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.4f, 0.4f, 0.4f), elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(flyObj);
        UpdateInventory();
    }

    public void UpdateInventory()
    {
        foreach (Transform child in inventoryContainer) Destroy(child.gameObject);
        foreach (int id in MaidGameManager.Instance.foundIconIds)
        {
            MaidData data = MaidGameManager.Instance.allMaids.Find(m => m.id == id);
            if (data != null)
            {
                GameObject go = Instantiate(inventoryItemPrefab, inventoryContainer);
                go.GetComponent<InventoryItem>().Setup(data);
            }
        }
    }

    public void ToggleBook()
    {
        bookRoot.SetActive(!bookRoot.activeSelf);
        if (globalProgressPanel) globalProgressPanel.SetActive(!bookRoot.activeSelf);
    }

    #endregion

    #region Navigation Updates

    public void RefreshNavigationUI()
    {
        for (int i = 0; i < phaseButtons.Length; i++)
        {
            phaseButtons[i].interactable = MaidGameManager.Instance.phaseUnlocked[i];
            if (i < connections.Length)
            {
                connections[i].color = MaidGameManager.Instance.phaseUnlocked[i + 1] ? Color.gold : Color.gray;
            }
        }
    }

    #endregion
}