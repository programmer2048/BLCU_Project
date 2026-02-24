using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JetBrains.Annotations;
using DG.Tweening;

public class TrayController : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI countText;
    public Slider progressSlider;
    public Image sliderFillImage; // ★ 必须在 Inspector 里把 Slider 的 Fill Area 下的 Image 拖进来

    [Header("Settings")]
    public float moveSpeed = 40f;
    public float killX = -500f; // 屏幕左侧销毁线
    private float startX;       // 出生时的 X 坐标

    // Data
    public M3_ItemType RequiredType { get; private set; }
    public int currentCount;
    public int requiredCount;
    public bool IsCompleted => currentCount >= requiredCount;

    // 防止重复触发完成逻辑
    private bool isFinishedProcessStarted = false;

    void Start()
    {
        // 记录初始位置用于计算进度百分比
        startX = GetComponent<RectTransform>().anchoredPosition.x;
    }

    public void Init(M3_ItemType type, Sprite sprite, int count)
    {
        RequiredType = type;
        requiredCount = count;
        currentCount = 0;
        if (iconImage != null) iconImage.sprite = sprite;
        UpdateUI();
    }

    void Update()
    {
        if (isFinishedProcessStarted) return; // 完成后停止移动

        // 1. 移动
        RectTransform rt = GetComponent<RectTransform>();
        rt.anchoredPosition += Vector2.left * moveSpeed * Time.deltaTime;
        float currentX = rt.anchoredPosition.x;

        // 2. 失败检测
        if (currentX < killX)
        {
            OnTrayFailed();
            return;
        }

        // 3. 计算 Patience 进度条 (根据距离左侧的距离)
        // 假设从 startX 到 killX 是全程
        float totalDistance = startX - killX;
        float currentDistance = currentX - killX;
        float progress = Mathf.Clamp01(currentDistance / totalDistance);

        if (progressSlider != null)
        {
            progressSlider.value = progress;

            // 4. 颜色渐变 (Green -> Red)
            if (sliderFillImage != null)
            {
                sliderFillImage.color = Color.Lerp(Color.red, Color.green, progress);
            }
        }
    }

    // 当食材飞到这里时调用
    public void AddProgress(int amount)
    {
        if (IsCompleted || isFinishedProcessStarted) return;

        currentCount += amount;
        UpdateUI();

        // ★ 播放 "-1" 飘字动画
        // 位置在图标的右上角
        for (int i = 1; i <= amount; i++)
        {
            Vector3 pos = iconImage.transform.position;
            pos.y += UnityEngine.Random.Range(-20f, 20f);
            pos.x += UnityEngine.Random.Range(-20f, 20f);
            DOVirtual.DelayedCall(i * 0.2f, () => FX_Manager.Instance.PlayFloatingText(pos, "-1"));
        }

        // 检查是否完成
        if (IsCompleted)
        {
            StartCoroutine(CompleteRoutine());
        }
    }

    // 完成后的协程：播放金币动画 -> 销毁
    System.Collections.IEnumerator CompleteRoutine()
    {
        isFinishedProcessStarted = true;

        // 1. 等待一小会儿让玩家看清 "-1" 和进度满了
        yield return new WaitForSeconds(0.2f);

        // 2. 获取金钱飞行的目标位置 (从 GameManager 获取 UI 位置)
        Vector3 revenuePos = M3_GameManager.Instance.GetRevenueUIPosition();

        // 3. 播放 "$" 飞行
        FX_Manager.Instance.PlayCoinFly(transform.position, revenuePos, () =>
        {
            // 4. 只有当 "$" 飞到目的地后，才真正加分
            M3_GameManager.Instance.AddScore(2);
        });

        // 4. 隐藏订单 (为了视觉上让位，但不立即 Destroy，防止回调报错)
        // 或者简单地做个缩小动画
        transform.localScale = Vector3.zero;

        // 从列表移除
        OrderManager.Instance.RemoveActiveOrder(this);

        yield return new WaitForSeconds(1.0f); // 等待金币飞完
        Destroy(gameObject);
    }

    void OnTrayFailed()
    {
        OrderManager.Instance.OnOrderFailed(this);
        Destroy(gameObject);
    }

    void UpdateUI()
    {
        if (countText != null)
            countText.text = $"{Mathf.Max(requiredCount - currentCount, 0)}";
    }

    public Vector3 GetIconPosition()
    {
        if (iconImage != null) return iconImage.transform.position;
        return transform.position;
    }
}