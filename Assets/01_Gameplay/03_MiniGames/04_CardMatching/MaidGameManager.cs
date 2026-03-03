using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class MaidGameManager : MonoBehaviour
{
    public static MaidGameManager Instance;

    [Header("Global State")]
    public int currentPhase = 1;
    public bool[] phaseUnlocked = new bool[4] { true, false, false, false };

    [Header("Data Reference")]
    public List<MaidData> allMaids; // 8位侍女的配置数据

    [Header("Phase 1 Progress")]
    public HashSet<int> foundIconIds = new HashSet<int>(); // 已收集的图标ID
    public HashSet<int> repairedMaidIds = new HashSet<int>(); // 已修复(Phase1完成)的ID

    [Header("Phase 2 Progress")]
    public Dictionary<int, float> maidBlueprintProgress = new Dictionary<int, float>(); // 侍女蓝图进度(0-1)

    public int thisChapterId = 4; // 当前是第几章 (在 Inspector 设置)
    public int nextChapterId = 5; // 下一章 ID

    private void Awake()
    {
        // 强制开启日志
        Debug.unityLogger.logEnabled = true;
        // 确保过滤器不过滤任何类型
        Debug.unityLogger.filterLogType = LogType.Log;
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }


    #region Phase Management (流程控制)

    public void ChangePhase(int targetPhase)
    {
        if (targetPhase > 0 && targetPhase <= 4 && phaseUnlocked[targetPhase - 1])
        {
            currentPhase = targetPhase;
            MaidUIManager.Instance.SwitchPhaseUI(targetPhase);

            // 阶段切换时的特殊初始化
            OnPhaseStarted(targetPhase);
            Debug.Log($"切换至阶段: {targetPhase}");
        }
    }

    private void OnPhaseStarted(int phase)
    {
        switch (phase)
        {
            case 2:
                // 初始化第二阶段
                BlueprintManager.Instance.InitializeBlueprints(); // 生成右侧蓝图列表
                ItemSpawner.Instance.GenerateLevel();           // 生成左侧堆叠物
                break;
            case 3:
                //Phase3Manager.StartScratchGame(0);
                // TODO: 初始化第三阶段
                break;
        }
    }

    public void UnlockNextPhase(int nextPhaseIndex)
    {
        if (nextPhaseIndex <= phaseUnlocked.Length)
        {
            phaseUnlocked[nextPhaseIndex - 1] = true;

            // 刷新导航栏按钮状态（让 Phase 3 按钮变亮）
            if (MaidUIManager.Instance != null)
            {
                MaidUIManager.Instance.RefreshNavigationUI();

                // 可选：弹出一个提示，或者自动切换到新阶段
                // MaidUIManager.Instance.ShowUnlockNotification(nextPhaseIndex);
            }

            Debug.Log($"阶段 {nextPhaseIndex} 已解锁!");
        }
    }

    #endregion

    #region Phase 1 Logic (收集与拼图)

    public void CollectIcon(int id)
    {
        if (!foundIconIds.Contains(id) && allMaids.Any(m => m.id == id))
        {
            foundIconIds.Add(id);
            MaidData data = allMaids.Find(m => m.id == id);
            MaidUIManager.Instance.PlayCollectAnimation(data, Input.mousePosition);
            Debug.Log($"已收集物品: {id}");
        }
    }

    public void OnPhase1MatchSuccess(int id)
    {
        if (!repairedMaidIds.Contains(id))
        {
            repairedMaidIds.Add(id);
            // 如果8个全部修复，解锁第二阶段
            if (repairedMaidIds.Count >= 8) UnlockNextPhase(2);
        }
    }

    #endregion

    // 在 MaidGameManager 类中添加/修改以下内容

    [Header("Phase 2 Progress")]
    public HashSet<int> completedBlueprintIds = new HashSet<int>(); // 新增：记录已完成蓝图的女仆ID

    #region Phase 2 Logic (蓝图合成与解锁)

    // 该方法由 BlueprintManager 调用，用来增加某个女仆的进度
    public void UpdateBlueprintProgress(int maidId, float progressDelta)
    {
        if (!maidBlueprintProgress.ContainsKey(maidId))
            maidBlueprintProgress[maidId] = 0;

        maidBlueprintProgress[maidId] += progressDelta;

        // 进度达到或超过 1.0 (100%)
        if (maidBlueprintProgress[maidId] >= 1.0f)
        {
            maidBlueprintProgress[maidId] = 1.0f; // 封顶
            OnPhase2BlueprintComplete(maidId);
        }
    }

    public void OnPhase2BlueprintComplete(int id)
    {
        if (!completedBlueprintIds.Contains(id))
        {
            completedBlueprintIds.Add(id);
            Debug.Log($"女仆 {id} 的蓝图已完全恢复！当前总进度: {completedBlueprintIds.Count}/{allMaids.Count}");

            // 【核心判定逻辑】：如果完成的数量等于所有女仆的数量
            if (completedBlueprintIds.Count >= allMaids.Count)
            {
                UnlockNextPhase(3); // 解锁第三阶段
                Debug.Log("<color=green>所有女仆蓝图修复完毕！第三阶段已开启！</color>");
            }
        }
    }
    public void RestartPhase2()
    {
        // 1. 清理槽位
        if (SlotManager.Instance != null)
        {
            SlotManager.Instance.ResetSlot();
        }
        // 2. 重新生成关卡物品
        if (ItemSpawner.Instance != null)
        {
            ItemSpawner.Instance.GenerateLevel();
        }
        // 3. (可选) 是否重置蓝图进度？
        // 如果你希望玩家“彻底重来”，就清空进度：
        // maidBlueprintProgress.Clear();
        // completedBlueprintIds.Clear();
        // BlueprintManager.Instance.InitializeBlueprints(); 
        Debug.Log("第二阶段已重新开始！");
    }

    #endregion

    #region Phase 3 Logic (预留)
    // TODO: 第三阶段核心逻辑执行入口
    #endregion

    public void FinishLevelAndExit()
    {   // 1. 更新存档：当前章节变成“下一章”
        // 1. 更新存档：当前章节变成“下一章”ce != null && SaveManager.Instance.CurrentGameData != null)
        if (SaveManager.Instance != null && SaveManager.Instance.CurrentGameData != null)
        {   // 防止重复玩旧章节导致进度倒退 (可选)
            // 防止重复玩旧章节导致进度倒退 (可选)ce.CurrentGameData.currentChapter <= thisChapterId)
            if (SaveManager.Instance.CurrentGameData.currentChapter <= thisChapterId)
            {
                SaveManager.Instance.CurrentGameData.currentChapter = nextChapterId;
                SaveManager.Instance.CurrentGameData.currentChapter = nextChapterId;
            }
            // 2. 保存到硬盘
            // 2. 保存到硬盘.Instance.SaveCurrentGame();
            SaveManager.Instance.SaveCurrentGame();
            // 3. 返回主菜单
            // 3. 返回主菜单r.LoadScene("01_MainUI");
            SceneManager.LoadScene("01_MainUI");
        }
    }
}