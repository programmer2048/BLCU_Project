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

        float originalWorldWidth = sr.sprite.bounds.size.x;
        float originalWorldHeight = sr.sprite.bounds.size.y;

        if (originalWorldWidth == 0 || originalWorldHeight == 0) return;

        float scaleX = targetWidth / originalWorldWidth;
        float scaleY = targetHeight / originalWorldHeight;

        transform.localScale = new Vector3(scaleX, scaleY, 1f);

        BoxCollider2D boxCol = GetComponent<BoxCollider2D>();
        if (boxCol != null)
        {
            boxCol.size = new Vector2(originalWorldWidth, originalWorldHeight);
        }
    }
}