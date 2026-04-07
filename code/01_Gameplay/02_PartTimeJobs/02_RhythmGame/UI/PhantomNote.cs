using TMPro;
using UnityEngine;

public class PhantomNote : MonoBehaviour
{
    [Header("参数")]
    public float moveSpeed = 20f; 
    public float swayAmount = 25f;   
    public float swaySpeed = 1.5f;     
    public float lifeTime = 2.0f;     
    public Vector3 endScale = Vector3.one * 2.5f; 

    private TextMeshProUGUI tmp;
    private RectTransform rectTransform;
    private float timer = 0f;
    private Color targetColor;
    private Vector2 startAnchoredPos;
    private float randomPhase;

    void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(string text, Color color)
    {
        if (!tmp) tmp = GetComponent<TextMeshProUGUI>();

        if (tmp)
        {
            tmp.text = text;
            targetColor = color;
            tmp.color = color; // 初始可见
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

        // S 型飘动
        float yOffset = moveSpeed * timer;
        float xOffset = Mathf.Sin(timer * swaySpeed + randomPhase) * swayAmount;

        if (rectTransform)
        {
            rectTransform.anchoredPosition = startAnchoredPos + new Vector2(xOffset, yOffset);
        }

        // 缩放
        transform.localScale = Vector3.Lerp(Vector3.one, endScale, progress);

        // Alpha
        float alpha = 1f;
        if (progress > 0.3f)
        {
            alpha = 1f - ((progress - 0.3f) / 0.7f);
        }

        if (tmp)
        {
            tmp.color = new Color(targetColor.r, targetColor.g, targetColor.b, alpha * targetColor.a);
        }
    }
}