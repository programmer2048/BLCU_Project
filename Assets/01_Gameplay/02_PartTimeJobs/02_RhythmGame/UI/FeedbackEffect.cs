using UnityEngine;
using TMPro;

public class FeedbackEffect : MonoBehaviour
{
    [Header("反馈参数 (快速)")]
    public float moveSpeed = 80f;       // [修改点] 速度快，迅速飞离
    public float swayAmount = 5f;       // [修改点] 摇摆幅度小
    public float swaySpeed = 3f;
    public float lifeTime = 0.8f;       // [修改点] 消失得快
    public Vector3 endScale = Vector3.one * 1.2f; // 稍微变大一点点即可

    private TextMeshProUGUI textMesh;
    private RectTransform rectTransform;
    private float timer = 0f;
    private Color targetColor;
    private Vector2 startAnchoredPos;
    private float randomPhase;

    void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(string text, Color color)
    {
        if (!textMesh) textMesh = GetComponent<TextMeshProUGUI>();

        if (textMesh)
        {
            textMesh.text = text;
            targetColor = color;
            textMesh.color = color;
        }

        if (rectTransform) startAnchoredPos = rectTransform.anchoredPosition;

        randomPhase = Random.Range(0f, 100f);
        timer = 0f;
        transform.localScale = Vector3.one;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / lifeTime;

        if (progress >= 1f) { Destroy(gameObject); return; }

        // 快速上升
        float yOffset = moveSpeed * timer;
        float xOffset = Mathf.Sin(timer * swaySpeed + randomPhase) * swayAmount;

        if (rectTransform)
        {
            rectTransform.anchoredPosition = startAnchoredPos + new Vector2(xOffset, yOffset);
        }

        transform.localScale = Vector3.Lerp(Vector3.one, endScale, progress);

        // 后半段快速淡出
        float alpha = 1f;
        if (progress > 0.5f)
        {
            alpha = 1f - ((progress - 0.5f) / 0.5f);
        }

        if (textMesh)
        {
            textMesh.color = new Color(targetColor.r, targetColor.g, targetColor.b, alpha * targetColor.a);
        }
    }
}