using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class AdaptiveGround : MonoBehaviour
{
    [Header("设置")]
    [Tooltip("期望的固定像素高度 (例如 100 像素)")]
    public float fixedPixelHeight = 100f;

    void Start()
    {
        AdjustGroundSize();
    }

    // 如果你的游戏支持屏幕旋转或窗口拉伸，可以把下面这行取消注释，放在 Update 里
    // void Update() { AdjustGroundSize(); }

    void AdjustGroundSize()
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        BoxCollider2D col = GetComponent<BoxCollider2D>();

        // 1. 计算屏幕在世界坐标系下的实际宽度和高度
        // 正交相机的 orthographicSize 等于屏幕高度的一半（世界单位）
        float worldScreenHeight = cam.orthographicSize * 2f;
        float worldScreenWidth = worldScreenHeight * cam.aspect; // aspect 是宽高比

        // 2. 将你想要的“固定像素高度”换算成“世界单位高度”
        // 公式：(目标像素高度 / 屏幕总像素高度) * 屏幕总世界高度
        float targetWorldHeight = (fixedPixelHeight / Screen.height) * worldScreenHeight;

        // 3. 获取当前 Sprite 素材本身的原始物理大小
        Vector2 spriteSize = sr.sprite.bounds.size;

        // 4. 计算需要的 Scale
        // X轴 Scale = 期望的世界宽度(撑满屏幕) / 素材原始宽度
        // Y轴 Scale = 期望的世界高度(固定像素换算后) / 素材原始高度
        float scaleX = worldScreenWidth / spriteSize.x;
        float scaleY = targetWorldHeight / spriteSize.y;

        // 应用缩放
        transform.localScale = new Vector3(scaleX, scaleY, 1f);

        // 5. 确保碰撞体大小与图片一致 (缩放 Transform 后，Collider 会自动跟随缩放)
        // 如果你的碰撞体之前被手动改过大小，可以重置它：
        col.size = spriteSize;

        // 6. （可选）将地面固定放置在屏幕最底端
        // 屏幕底部的世界 Y 坐标 = 相机 Y 坐标 - 相机正交高度
        float screenBottomY = cam.transform.position.y - cam.orthographicSize;
        // 地面的 Y 坐标 = 屏幕底部 Y + 地面自身高度的一半
        transform.position = new Vector3(cam.transform.position.x, screenBottomY + (targetWorldHeight / 2f), 0f);
    }
}