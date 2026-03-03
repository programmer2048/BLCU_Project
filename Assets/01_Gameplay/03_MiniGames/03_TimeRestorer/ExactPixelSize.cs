using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[ExecuteInEditMode] // 允许在编辑器模式下实时预览大小，不需要运行游戏！
public class ExactPixelSize : MonoBehaviour
{
    [Header("期望的物理像素大小")]
    public float targetWidth = 100f;  // 例如：长2的方块填 100
    public float targetHeight = 100f; // 例如：宽2的方块填 100

    void Start()
    {
        ApplyExactSize();
    }

    // 当你在 Inspector 面板修改数值时，自动更新大小
    void OnValidate()
    {
        ApplyExactSize();
    }

    public void ApplyExactSize()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        // 1. 获取这个图片在当前 PPU 下的“原始世界单位尺寸”
        float originalWorldWidth = sr.sprite.bounds.size.x;
        float originalWorldHeight = sr.sprite.bounds.size.y;

        // 防止除以 0 的错误
        if (originalWorldWidth == 0 || originalWorldHeight == 0) return;

        // 2. 计算出达到目标像素尺寸所需要的精确 Scale 值
        // 前提：你已经使用了上一步的 PixelCamera，相机的 1 个世界单位 = 1 个像素
        float scaleX = targetWidth / originalWorldWidth;
        float scaleY = targetHeight / originalWorldHeight;

        // 3. 强行覆写 Transform 的 Scale
        transform.localScale = new Vector3(scaleX, scaleY, 1f);

        // 4. (可选) 如果挂载了 BoxCollider2D，我们重置它的 size 确保物理碰撞盒也精准
        BoxCollider2D boxCol = GetComponent<BoxCollider2D>();
        if (boxCol != null)
        {
            // 因为 Transform scale 已经缩放了，Collider 的自身 size 只需要保持和 sprite 的原始 size 一致即可完美贴合
            boxCol.size = new Vector2(originalWorldWidth, originalWorldHeight);
        }
    }
}