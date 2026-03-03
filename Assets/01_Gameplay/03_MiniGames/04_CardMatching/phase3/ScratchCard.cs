using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ScratchCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public RawImage topImage;
    public Image bottomImage;
    public Image completeMark; // 新增：勾选图片引用

    [Header("Settings")]
    public Material baseMaterial;
    public float completionThreshold = 0.6f; // 60% 阈值

    private RenderTexture maskRT;
    private Material instanceMaterial;
    private MaidData myData;
    private RectTransform rectTransform;
    private bool isHovering = false;
    private bool isFinished = false; // 是否已完成

    // 用于性能优化的采样纹理
    private Texture2D checkTex;
    private float lastCheckTime = 0f;

    public void Init(MaidData data, int textureSize)
    {
        myData = data;
        rectTransform = GetComponent<RectTransform>();

        // 1. 初始化 RT
        maskRT = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.R8);
        maskRT.Create();

        // 2. 初始化材质
        instanceMaterial = new Material(baseMaterial);
        topImage.texture = data.sprite.texture;
        instanceMaterial.SetTexture("_MainTex", data.sprite.texture);
        instanceMaterial.SetTexture("_MaskTex", maskRT);
        topImage.material = instanceMaterial;

        // 3. 初始化底图和勾选框
        bottomImage.sprite = data.emotionalSprite;
        bottomImage.preserveAspect = true;

        if (completeMark != null)
        {
            completeMark.gameObject.SetActive(false); // 初始隐藏勾选框
        }

        // 4. 初始化采样贴图 (32x32 足够计算百分比且不卡顿)
        checkTex = new Texture2D(32, 32, TextureFormat.R8, false);

        ClearMask();
    }

    public void DrawAt(Vector2 uv, float brushSize, Texture2D brushTex)
    {
        if (isFinished) return;

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = maskRT;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, maskRT.width, maskRT.height, 0);

        float x = uv.x * maskRT.width;
        float y = (1 - uv.y) * maskRT.height;
        Graphics.DrawTexture(new Rect(x - brushSize, y - brushSize, brushSize * 2, brushSize * 2), brushTex);

        GL.PopMatrix();
        RenderTexture.active = prev;

        // 5. 每 0.2 秒检查一次进度，避免性能浪费
        if (Time.time - lastCheckTime > 0.2f)
        {
            CheckProgress();
            lastCheckTime = Time.time;
        }
    }

    private void CheckProgress()
    {
        if (isFinished) return;

        // 将 RT 内容缩放到 32x32 的小图中读取
        RenderTexture temp = RenderTexture.GetTemporary(32, 32, 0, RenderTextureFormat.R8);
        Graphics.Blit(maskRT, temp);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = temp;
        checkTex.ReadPixels(new Rect(0, 0, 32, 32), 0, 0);
        checkTex.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(temp);

        // 计算非透明像素（R通道 > 0）
        Color32[] pixels = checkTex.GetPixels32();
        int scratchedCount = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].r > 0) scratchedCount++;
        }

        float progress = (float)scratchedCount / pixels.Length;
        if (progress >= completionThreshold)
        {
            FinishScratched();
        }
    }

    private void FinishScratched()
    {
        isFinished = true;

        // 1. 全干：将 MaskRT 填满白色（完全擦除）
        RenderTexture prev = RenderTexture.active;
        Graphics.SetRenderTarget(maskRT);
        GL.Clear(false, true, Color.white);
        Graphics.SetRenderTarget(prev);

        // 2. 视觉反馈
        if (completeMark != null) completeMark.gameObject.SetActive(true);

        // 3. 通知管理器增加计数
        FindObjectOfType<MultiScratchManager>().OnCardFinished(myData.id);
    }

    public void ClearMask()
    {
        RenderTexture prev = RenderTexture.active;
        Graphics.SetRenderTarget(maskRT);
        GL.Clear(false, true, Color.clear);
        Graphics.SetRenderTarget(prev);
    }

    public void OnPointerEnter(PointerEventData eventData) => isHovering = true;
    public void OnPointerExit(PointerEventData eventData) => isHovering = false;

    public bool GetHitUV(Vector2 screenPos, out Vector2 uv)
    {
        uv = Vector2.zero;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPos, null, out Vector2 localPoint))
        {
            float u = (localPoint.x - rectTransform.rect.x) / rectTransform.rect.width;
            float v = (localPoint.y - rectTransform.rect.y) / rectTransform.rect.height;
            uv = new Vector2(u, v);
            return (u >= 0 && u <= 1 && v >= 0 && v <= 1);
        }
        return false;
    }

    public bool IsMouseOver() => isHovering;
    public MaidData GetData() => myData;
    public bool IsFinished() => isFinished;
    public void ResetHover() => isHovering = false;
}