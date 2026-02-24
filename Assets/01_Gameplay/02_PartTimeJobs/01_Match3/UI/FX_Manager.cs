using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;

public class FX_Manager : MonoBehaviour
{
    public static FX_Manager Instance { get; private set; }

    [Header("References")]
    public Transform fxContainer;       // 之前那个全屏透明Panel
    public GameObject flyingIconPrefab; // 食材飞行Prefab
    public GameObject floatingTextPrefab; // 新增：飘字Prefab (TMP)
    public Sprite dollarSprite;         // 新增：美元符号图片

    void Awake()
    {
        Instance = this;
    }

    // ... 之前的 PlayFlyEffect 代码保持不变 ...
    public void PlayFlyEffect(Sprite sprite, Vector3 startPos, Vector3 endPos, Action onComplete)
    {
        GameObject icon = Instantiate(flyingIconPrefab, fxContainer);
        icon.SetActive(true);
        icon.transform.position = startPos;
        icon.transform.localScale = Vector3.one;

        Image img = icon.GetComponent<Image>();
        if (img) img.sprite = sprite;

        Sequence seq = DOTween.Sequence();
        seq.Append(icon.transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack));
        seq.Append(icon.transform.DOJump(endPos, 100f, 1, 0.6f)); // 抛物线
        seq.Join(icon.transform.DOScale(0.5f, 0.6f));

        seq.OnComplete(() =>
        {
            onComplete?.Invoke();
            Destroy(icon);
        });
    }

    // --- 新增：飘字特效 ("-1") ---
    public void PlayFloatingText(Vector3 position, string textContent)
    {
        if (floatingTextPrefab == null) return;

        GameObject go = Instantiate(floatingTextPrefab, fxContainer);
        go.transform.position = position + new Vector3(30, 30, 0); //稍微向右上偏移
        go.transform.localScale = Vector3.one;

        TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
        txt.text = textContent;
        txt.alpha = 1;

        // 动画：向上飘 + 淡出
        Sequence seq = DOTween.Sequence();
        seq.Append(go.transform.DOMoveY(position.y + 100f, 0.8f));
        seq.Join(txt.DOFade(0, 0.8f).SetEase(Ease.InQuad));
        seq.OnComplete(() => Destroy(go));
    }

    // --- 新增：金币/美元飞行特效 ---
    // 从订单位置飞向 总分/金钱 UI位置
    public void PlayCoinFly(Vector3 startPos, Vector3 targetPos, Action onArrive)
    {
        int coinCount = 5;        // 金币数量
        float interval = 0.1f;    // 间隔时间 (秒)

        // 我们不希望回调执行5次导致加5次分，
        // 所以只在“最后一个金币”到达时执行回调。

        for (int i = 0; i < coinCount; i++)
        {
            // 闭包捕获索引，防止i在循环结束后变成最大值
            int index = i;
            bool isLastCoin = (index == coinCount - 1);
            // 使用 DOTween 的延时调用，不会卡顿主线程
            DOVirtual.DelayedCall(index * interval, () =>
            {
                // 给起点加一点随机偏移，让金币看起来不是一条直线，稍微散开一点更自然
                Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-20f, 20f), UnityEngine.Random.Range(-20f, 20f), 0);

                PlayFlyEffect(
                    dollarSprite,
                    startPos + randomOffset,
                    targetPos,
                    // 只有最后一个金币到达时，才触发 onArrive (加钱)
                    isLastCoin ? onArrive : null
                );
            });
        }
    }
}