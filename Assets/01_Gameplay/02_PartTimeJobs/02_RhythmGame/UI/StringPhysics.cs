using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(RectTransform))] // 确保有 RectTransform
public class UIString : MonoBehaviour
{
    [Header("高频物理设置")]
    [Tooltip("分步模拟次数，高劲度系数必须配合高步数，否则会炸。建议 12-16")]
    public int simulationSubSteps = 12;

    [Tooltip("劲度系数：设得非常高，让波速极快，瞬间传导到两端。")]
    private float stiffness = 32000f;

    [Tooltip("阻尼：设高一点，让震动迅速停下来（回复快）。")]
    private float damping = 15f;

    [Tooltip("能量衰减：稍低一点，辅助阻尼快速消能。")]
    private float energyDecay = 0.997f;

    [Header("外观设置")]
    public int pointCount = 60; // 点多一点，波形更细腻
    private float lineWidth = 0.05f;
    public Color color = Color.cyan;
    public int sortingOrder = 20;

    // 物理数据
    private float[] heights;
    private float[] velocities;
    private float[] forces; // 缓存力数组，减少GC

    private LineRenderer lr;
    private RectTransform rectTrans;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        rectTrans = GetComponent<RectTransform>();

        heights = new float[pointCount];
        velocities = new float[pointCount];
        forces = new float[pointCount];

        InitLineRenderer();
    }

    void InitLineRenderer()
    {
        lr.useWorldSpace = false;
        lr.positionCount = pointCount;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        if (lr.material == null || lr.material.name.StartsWith("Default-Line"))
            lr.material = new Material(Shader.Find("Sprites/Default"));

        lr.sortingLayerName = "Default";
        lr.sortingOrder = sortingOrder;
        lr.startColor = color;
        lr.endColor = color;
    }

    void Update()
    {
        // 限制 dt 防止卡顿爆炸
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        float subStepDt = dt / simulationSubSteps;

        for (int step = 0; step < simulationSubSteps; step++)
        {
            UpdatePhysics(subStepDt);
        }

        DrawString();
    }

    void UpdatePhysics(float dt)
    {
        // 1. 计算受力
        for (int i = 1; i < pointCount - 1; i++)
        {
            // 波动方程: F = k * (x_left + x_right - 2*x)
            float force = stiffness * (heights[i - 1] + heights[i + 1] - 2 * heights[i]);
            // 阻尼
            force -= damping * velocities[i];
            forces[i] = force;
        }

        // 2. 应用速度与位移
        for (int i = 1; i < pointCount - 1; i++)
        {
            velocities[i] += forces[i] * dt;
            velocities[i] *= energyDecay;
            heights[i] += velocities[i] * dt;

            // 安全锁：限制最大振幅，如果因为物理BUG导致数值爆炸，强制归零
            if (float.IsNaN(heights[i]) || Mathf.Abs(heights[i]) > 500f)
            {
                heights[i] = 0f;
                velocities[i] = 0f;
            }
        }
    }

    void DrawString()
    {
        if (rectTrans == null) return;
        float width = rectTrans.rect.width;
        float startX = -width / 2f;
        float spacing = width / (pointCount - 1);

        for (int i = 0; i < pointCount; i++)
        {
            float x = startX + i * spacing;
            float y = heights[i];
            // 视觉限制：只限制渲染，不限制物理
            y = Mathf.Clamp(y, -100f, 100f);
            lr.SetPosition(i, new Vector3(x, y, -1f));
        }
    }

    /// <summary>
    /// 拨弦
    /// </summary>
    /// <param name="ratio">位置 0~1</param>
    /// <param name="power">力度</param>
    public void Pluck(float ratio, float power)
    {
        int centerIndex = Mathf.RoundToInt(ratio * (pointCount - 1));
        centerIndex = Mathf.Clamp(centerIndex, 2, pointCount - 3);

        // 稍微扩大一点受力范围，让波形不那么尖锐，传递更好
        int spread = 3;

        for (int i = -spread; i <= spread; i++)
        {
            int idx = centerIndex + i;
            if (idx > 0 && idx < pointCount - 1)
            {
                float falloff = 1.0f - Mathf.Abs(i) / (float)(spread + 1);
                // 直接加在速度上
                velocities[idx] += power * falloff;
            }
        }
    }
}