using UnityEngine;
using System.Collections.Generic;

public class BlueprintManager : MonoBehaviour
{
    public static BlueprintManager Instance;

    [Header("UI Structure")]
    public Transform contentParent; // Scroll View 的 Content
    public GameObject blueprintItemPrefab; // 蓝图 UI 的预制体

    // 使用字典方便通过 maidId 快速找到对应的 UI 实例
    private Dictionary<int, BlueprintItemUI> activeBlueprints = new Dictionary<int, BlueprintItemUI>();

    private void Awake() { Instance = this; }

    private void Start()
    {
        // 如果是直接从第二阶段开始，或者由 GameManager 调用
        // InitializeBlueprints();
    }

    public void InitializeBlueprints()
    {
        // 清理旧内容
        foreach (Transform child in contentParent) Destroy(child.gameObject);
        activeBlueprints.Clear();

        // 动态插入 8 个蓝图
        // 按照 MaidGameManager 中的 allMaids 列表顺序生成
        foreach (var maid in MaidGameManager.Instance.allMaids)
        {
            GameObject go = Instantiate(blueprintItemPrefab, contentParent);
            BlueprintItemUI uiScript = go.GetComponent<BlueprintItemUI>();

            // 初始化 UI 内容
            uiScript.Setup(maid);

            // 加入字典索引
            activeBlueprints.Add(maid.id, uiScript);
        }
    }

    // 供 SlotManager 调用：增加进度
    public void AddProgress(int maidId)
    {
        if (activeBlueprints.ContainsKey(maidId))
        {
            activeBlueprints[maidId].IncreaseProgress();
        }
    }
}