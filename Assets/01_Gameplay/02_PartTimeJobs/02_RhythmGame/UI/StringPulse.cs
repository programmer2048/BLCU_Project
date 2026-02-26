using UnityEngine;
using UnityEngine.UI;

public class StringPulse : MonoBehaviour
{
    [Header("配置")]
    public float speed = 2000f;     // 极快的速度
    public float stretchFactor = 0.5f; // 拉伸系数，越快越长

    private RectTransform rectTrans;
    private Image img;
    private float direction;
    private float boundaryX;        // 边界限制
    private bool isRunning = false;

    void Awake()
    {
        rectTrans = GetComponent<RectTransform>();
        img = GetComponent<Image>();
    }

    /// <summary>
    /// 初始化脉冲
    /// </summary>
    /// <param name="dir">方向: 1 或 -1</param>
    /// <param name="parentWidth">父物体(琴弦)的总宽度</param>
    public void Setup(float dir, float parentWidth)
    {
        direction = dir;
        boundaryX = parentWidth / 2f; // 假设中心点在0，边界就是宽度的一半
        isRunning = true;

        // 视觉优化：根据方向旋转或拉伸
        // 这里我们简单粗暴地修改 Scale 让它看起来像拖尾
        // 初始拉伸一点点
        Vector3 s = transform.localScale;
        s.x = (1f + speed * 0.001f * stretchFactor);
        transform.localScale = s;

        // 如果你的图片有方向性（比如箭头），可以在这里旋转
        if (dir < 0) transform.localRotation = Quaternion.Euler(0, 0, 180);
    }

    void Update()
    {
        if (!isRunning) return;

        // 1. 移动
        float moveStep = speed * direction * Time.deltaTime;
        rectTrans.anchoredPosition += new Vector2(moveStep, 0);

        // 2. 边界检查 (越界销毁)
        // 使用 anchoredPosition.x 的绝对值与边界比较
        if (Mathf.Abs(rectTrans.anchoredPosition.x) >= boundaryX)
        {
            // 可选：在销毁前产生一个微小的“消失特效”或“火花”
            Destroy(gameObject);
        }
    }
}