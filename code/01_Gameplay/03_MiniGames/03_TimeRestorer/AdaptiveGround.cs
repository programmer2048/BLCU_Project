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

    void AdjustGroundSize()
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        BoxCollider2D col = GetComponent<BoxCollider2D>();

        float worldScreenHeight = cam.orthographicSize * 2f;
        float worldScreenWidth = worldScreenHeight * cam.aspect; // aspect 是宽高比

        float targetWorldHeight = (fixedPixelHeight / Screen.height) * worldScreenHeight;

        Vector2 spriteSize = sr.sprite.bounds.size;

        float scaleX = worldScreenWidth / spriteSize.x;
        float scaleY = targetWorldHeight / spriteSize.y;

        // 应用缩放
        transform.localScale = new Vector3(scaleX, scaleY, 1f);

        col.size = spriteSize;

        float screenBottomY = cam.transform.position.y - cam.orthographicSize;
        transform.position = new Vector3(cam.transform.position.x, screenBottomY + (targetWorldHeight / 2f), 0f);
    }
}