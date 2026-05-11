using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class BlueprintItemUI : MonoBehaviour
{
    [Header("Internal UI")]
    public Image displayImage;
    public Slider progressBar;
    public TextMeshProUGUI maidNameText;

    private MaidData data;
    private int currentProgress = 0;
    private int maxProgress = 3;

    public void Setup(MaidData maidData)
    {
        this.data = maidData;
        ResetState(); // 提取成独立的方法，方便重新开始时调用
    }

    // --- 新增：专门用于重置前端状态的方法 ---
    public void ResetState()
    {
        // 1. 【核心】杀死当前正在播放的 DOTween 动画，防止动画覆盖重置值
        progressBar.DOKill();
        displayImage.DOKill();
        transform.DOKill();

        // 2. 清空内部进度
        currentProgress = 0;

        // 3. 重置样式为蓝图状态
        displayImage.sprite = data.blueprintSprite;
        displayImage.color = new Color(0.5f, 0.7f, 1f, 1f);
        transform.localScale = Vector3.one; // 重置可能存在的 PunchScale 缩放
        progressBar.value = 0;

        if (maidNameText) maidNameText.text = "???";
    }

    public void IncreaseProgress()
    {
        currentProgress++;
        float targetValue = (float)currentProgress / maxProgress;

        progressBar.DOValue(targetValue, 0.5f);

        if (currentProgress >= maxProgress)
        {
            CompleteBlueprint();
        }
    }

    private void CompleteBlueprint()
    {
        displayImage.sprite = data.sprite;
        displayImage.DOColor(Color.white, 1f);
        transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), 0.5f);

        if (maidNameText) maidNameText.text = "已修复"; // 应该是 data.maidName ?

        MaidGameManager.Instance.OnPhase2BlueprintComplete(data.id);
    }
}