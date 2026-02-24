using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class ConveyorBelt : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("移动速度 (像素/秒)。正数表示向左流动（符合履带通常方向）。")]
    public float speedPixelsPerSec = 100f; // 默认设为 100 像素/秒

    private RawImage _rawImage;
    private RectTransform _rectTransform; // 新增：我们需要知道图片的实际宽度
    private Rect _uvRect;

    void Awake()
    {
        _rawImage = GetComponent<RawImage>();
        _rectTransform = GetComponent<RectTransform>(); // 获取 RectTransform 组件
        _uvRect = _rawImage.uvRect;
    }

    void Update()
    {
        float containerWidth = _rectTransform.rect.width;
        if (containerWidth <= 0) return;
        float textureRepeatCount = _rawImage.uvRect.width;
        float uvChange = (speedPixelsPerSec * Time.deltaTime / containerWidth) * textureRepeatCount;
        _uvRect.x += uvChange;
        // 5. 保持数值在 0-1 之间以防浮点数溢出
        if (_uvRect.x > 1f) _uvRect.x -= 1f;
        else if (_uvRect.x < -1f) _uvRect.x += 1f;
        _rawImage.uvRect = _uvRect;
    }

    // 供外部调用
    public void SetSpeed(float newPixelsPerSec)
    {
        speedPixelsPerSec = newPixelsPerSec;
    }
}