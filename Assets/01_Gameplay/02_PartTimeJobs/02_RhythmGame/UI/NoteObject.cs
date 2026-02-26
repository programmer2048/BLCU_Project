using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NoteObject : MonoBehaviour
{
    public RectTransform rectTrans;
    public Image noteImage;

    [HideInInspector] public float targetBeat;
    [HideInInspector] public int laneIndex;
    [HideInInspector] public R_NoteType type;
    [HideInInspector] public float holdDuration;

    public bool isHit = false;
    public bool isHolding = false;

    private RhythmGameManager manager;
    private float hitY;

    private void Awake()
    {
        rectTrans = GetComponent<RectTransform>();
        if (noteImage == null) noteImage = GetComponentInChildren<Image>();
    }

    public void Init(RhythmGameManager mgr, float beat, int lane, R_NoteType nType, float duration, float lineY)
    {
        manager = mgr;
        targetBeat = beat;
        laneIndex = lane;
        type = nType;
        holdDuration = duration;
        hitY = lineY;

        isHit = false;
        isHolding = false;

        // 设置 Pivot: Hold 底部对齐(0.5, 0)，Tap 中心对齐(0.5, 0.5)
        if (type == R_NoteType.Hold) rectTrans.pivot = new Vector2(0.5f, 0f);
        else rectTrans.pivot = new Vector2(0.5f, 0.5f);

        // 关键修复：重置锚点，确保居中
        rectTrans.anchorMin = new Vector2(0.5f, 0.5f);
        rectTrans.anchorMax = new Vector2(0.5f, 0.5f);
        rectTrans.anchoredPosition3D = Vector3.zero; // 重置所有轴
        transform.localScale = Vector3.one;

        // 颜色重置
        if (noteImage)
        {
            noteImage.color = Color.white;
            noteImage.canvasRenderer.SetAlpha(1f);
        }

        UpdatePosition();
    }

    void Update()
    {
        if (manager == null) return;
        UpdatePosition();
    }

    void UpdatePosition()
    {
        // 击中后的动画由协程控制，不再跟随时间更新位置
        if (isHit && type == R_NoteType.Tap) return;

        float currentBeat = manager.songPositionInBeats;
        float pixelsPerBeat = manager.pixelsPerBeat;

        // --- Hold Note ---
        if (type == R_NoteType.Hold)
        {
            float tailBeat = targetBeat + holdDuration;
            float distTail = (tailBeat - currentBeat) * pixelsPerBeat;

            if (isHolding)
            {
                // 结束检测
                if (currentBeat >= tailBeat)
                {
                    manager.OnHoldComplete(this);
                    return;
                }

                // 头部吸附在判定线，尾部随时间变短
                // 关键修复：X 轴强制为 0
                rectTrans.anchoredPosition = new Vector2(0, hitY);

                float h = Mathf.Max(0, distTail);
                rectTrans.sizeDelta = new Vector2(rectTrans.sizeDelta.x, h);

                if (noteImage) noteImage.color = new Color(1f, 1f, 0.6f);
            }
            else
            {
                // 整体下落
                float distHead = (targetBeat - currentBeat) * pixelsPerBeat;
                rectTrans.anchoredPosition = new Vector2(0, hitY + distHead);
                rectTrans.sizeDelta = new Vector2(rectTrans.sizeDelta.x, holdDuration * pixelsPerBeat);
            }
        }
        // --- Tap Note ---
        else
        {
            float dist = (targetBeat - currentBeat) * pixelsPerBeat;
            // 关键修复：X 轴强制为 0
            rectTrans.anchoredPosition = new Vector2(0, hitY + dist);
        }
    }

    public void TriggerHit()
    {
        if (isHit) return;
        isHit = true;
        StartCoroutine(HitAnimationRoutine());
    }

    public void StartHolding()
    {
        if (isHolding) return;
        isHolding = true;
        isHit = true;
    }

    IEnumerator HitAnimationRoutine()
    {
        // 瞬间高亮
        if (noteImage) noteImage.color = new Color(1f, 1f, 1f, 1f);

        float timer = 0f;
        float duration = 0.15f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = startScale * 1.6f; // 放大更明显一点

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            if (noteImage) noteImage.canvasRenderer.SetAlpha(1f - t);
            yield return null;
        }

        Destroy(gameObject);
    }
}