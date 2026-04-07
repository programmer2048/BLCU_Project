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
    public void Setup(float dir, float parentWidth)
    {
        direction = dir;
        boundaryX = parentWidth / 2f;
        isRunning = true;

        Vector3 s = transform.localScale;
        s.x = (1f + speed * 0.001f * stretchFactor);
        transform.localScale = s;

        if (dir < 0) transform.localRotation = Quaternion.Euler(0, 0, 180);
    }

    void Update()
    {
        if (!isRunning) return;

        float moveStep = speed * direction * Time.deltaTime;
        rectTrans.anchoredPosition += new Vector2(moveStep, 0);

        if (Mathf.Abs(rectTrans.anchoredPosition.x) >= boundaryX)
        {
            Destroy(gameObject);
        }
    }
}