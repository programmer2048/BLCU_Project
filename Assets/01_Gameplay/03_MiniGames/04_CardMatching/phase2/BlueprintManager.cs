using UnityEngine;
using System.Collections.Generic;

public class BlueprintManager : MonoBehaviour
{
    public static BlueprintManager Instance;

    [Header("UI Structure")]
    public Transform contentParent;
    public GameObject blueprintItemPrefab;

    private Dictionary<int, BlueprintItemUI> activeBlueprints = new Dictionary<int, BlueprintItemUI>();

    private void Awake() { Instance = this; }

    public void InitializeBlueprints()
    {
        // 【核心修改】：如果 UI 已经被实例化过，直接重置它们，不再执行销毁逻辑
        if (activeBlueprints.Count > 0)
        {
            foreach (var uiScript in activeBlueprints.Values)
            {
                uiScript.ResetState();
            }
            return; // 提前退出
        }

        // 初次进入第二阶段：动态生成 8 张蓝图
        foreach (var maid in MaidGameManager.Instance.allMaids)
        {
            GameObject go = Instantiate(blueprintItemPrefab, contentParent);
            BlueprintItemUI uiScript = go.GetComponent<BlueprintItemUI>();

            uiScript.Setup(maid);
            activeBlueprints.Add(maid.id, uiScript);
        }
    }

    public void AddProgress(int maidId)
    {
        if (activeBlueprints.ContainsKey(maidId))
        {
            activeBlueprints[maidId].IncreaseProgress();
        }
    }
}