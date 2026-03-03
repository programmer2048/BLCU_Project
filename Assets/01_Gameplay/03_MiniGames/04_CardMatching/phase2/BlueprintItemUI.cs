using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // 推荐使用 DOTween 做变色动画
using TMPro;

public class BlueprintItemUI : MonoBehaviour
{
    [Header("Internal UI")]
    public Image displayImage;   // 显示蓝图或原图的 Image
    public Slider progressBar;   // 进度条
    public TextMeshProUGUI maidNameText;    // 侍女名字（可选）

    private MaidData data;
    private int currentProgress = 0;
    private int maxProgress = 3; // 消除三次完成

    public void Setup(MaidData maidData)
    {
        this.data = maidData;

        // 初始显示为蓝图（或者线稿）
        displayImage.sprite = data.blueprintSprite;
        // 如果需要蓝图有淡淡的蓝色调，可以设置颜色
        displayImage.color = new Color(0.5f, 0.7f, 1f, 1f);

        progressBar.value = 0;
        if (maidNameText) maidNameText.text = "???"; // 未完成前是问号
    }

    public void IncreaseProgress()
    {
        currentProgress++;
        float targetValue = (float)currentProgress / maxProgress;

        // 进度条平滑移动
        progressBar.DOValue(targetValue, 0.5f);

        if (currentProgress >= maxProgress)
        {
            CompleteBlueprint();
        }
    }

    private void CompleteBlueprint()
    {
        // 1. 切换为原图
        displayImage.sprite = data.sprite;

        // 2. 还原颜色（从蓝图色变回白色的真实色彩）
        displayImage.DOColor(Color.white, 1f);

        // 3. 简单的缩放特效
        transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), 0.5f);

        if (maidNameText) maidNameText.text = "已修复"; // 或者 data.maidName

        // 4. 通知总管
        MaidGameManager.Instance.OnPhase2BlueprintComplete(data.id);
    }
}