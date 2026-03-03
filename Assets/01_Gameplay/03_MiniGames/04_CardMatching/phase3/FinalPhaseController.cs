using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FinalPhaseController : MonoBehaviour
{
    [Header("--- 核心组件引用 ---")]
    [Tooltip("场景中不可修改的 ScratchCard 组件")]
    public ScratchCard finalPostcard;

    [Tooltip("包含提示文本的父物体 (例如显示: '解锁特殊剧情')")]
    public GameObject finishPromptGroup;

    [Tooltip("全屏透明按钮 (遮罩)，用于最后点击返回")]
    public Button fullScreenExitButton;

    [Header("--- 数据资源 ---")]
    [Tooltip("必须在此处指派数据，否则 TopImage 无法加载")]
    public MaidData postcardData;

    [Header("--- 笔刷设置 (独立于 MaidData) ---")]
    [Tooltip("鼠标跟随的 UI 图标 (Sprite)")]
    public Sprite cursorSprite;

    [Tooltip("用于运算的物理笔刷纹理 (Texture2D) - 必须勾选 Read/Write")]
    public Texture2D brushPattern;

    [Tooltip("笔刷逻辑半径")]
    public float brushLogicSize = 45f;

    [Header("--- 视觉控制 ---")]
    [Tooltip("UI 中跟随鼠标移动的 Image 物体")]
    public RectTransform visualBrushTransform;

    private bool isPhaseActive = false;
    private Image visualBrushImage;

    private void Awake()
    {
        // 强制开启日志
        Debug.unityLogger.logEnabled = true;
        // 确保过滤器不过滤任何类型
        Debug.unityLogger.filterLogType = LogType.Log;
    }

    void Start()
    {
        // 1. 强制重置 UI 状态
        if (finishPromptGroup) finishPromptGroup.SetActive(false);

        if (fullScreenExitButton)
        {
            fullScreenExitButton.gameObject.SetActive(false);
            fullScreenExitButton.onClick.RemoveAllListeners();
            fullScreenExitButton.onClick.AddListener(OnMaskClicked);
        }

        // 2. 初始化笔刷物体
        if (visualBrushTransform)
        {
            visualBrushImage = visualBrushTransform.GetComponent<Image>();
            if (visualBrushImage == null) visualBrushImage = visualBrushTransform.gameObject.AddComponent<Image>();

            // 设置 UI 显示用的 Sprite
            if (cursorSprite != null)
            {
                visualBrushImage.sprite = cursorSprite;
                visualBrushImage.SetNativeSize();
            }

            // 关键：取消射线检测，防止挡住鼠标点击下面的卡片
            visualBrushImage.raycastTarget = false;

            // 默认隐藏，等待 InitPhase 唤醒
            visualBrushTransform.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 【核心入口】必须由外部 Manager 显式调用！
    /// </summary>
    public void InitPhase()
    {
        Debug.Log($"FinalPhaseController: InitPhase 被调用. Data: {(postcardData != null ? postcardData.name : "NULL")}");

        isPhaseActive = true;
        this.gameObject.SetActive(true);

        // --- 修复 TopImage 未加载的问题 ---
        if (finalPostcard != null)
        {
            if (postcardData != null)
            {
                // 调用 ScratchCard 现有的 Init 方法
                // 确保 MaidData 里相关的 Sprite (如 sprite 或 blueprintSprite) 不为空
                finalPostcard.Init(postcardData, 1024);
            }
            else
            {
                Debug.LogError("FinalPhaseController: postcardData 未赋值！TopImage 将无法显示。");
            }
        }

        // --- 修复 Brush 被隐藏的问题 ---
        if (visualBrushTransform != null)
        {
            visualBrushTransform.gameObject.SetActive(true); // 强制显示
            visualBrushTransform.SetAsLastSibling(); // 确保渲染在最上层
        }
    }

    void Update()
    {
        // 如果未激活或完成了，不再执行
        if (!isPhaseActive) return;

        // --- 1. 笔刷跟随 ---
        if (visualBrushTransform != null)
        {
            visualBrushTransform.position = Input.mousePosition;
        }

        // --- 2. 涂抹逻辑 ---
        // 只有鼠标按下 + 有卡片 + 卡片没完成 时才运算
        if (Input.GetMouseButton(0) && finalPostcard != null && !finalPostcard.IsFinished())
        {
            if (finalPostcard.GetHitUV(Input.mousePosition, out Vector2 uv))
            {
                // 使用独立的 Texture2D 进行运算，而不是 MaidData 里的 Sprite
                if (brushPattern != null)
                {
                    finalPostcard.DrawAt(uv, brushLogicSize, brushPattern);
                }
            }
        }

        // --- 3. 完成检测 ---
        if (finalPostcard != null && finalPostcard.IsFinished())
        {
            if (finishPromptGroup != null && !finishPromptGroup.activeSelf)
            {
                OnPostcardCompleted();
            }
        }
    }

    void OnPostcardCompleted()
    {
        Debug.Log("FinalPhase: 刮卡完成，进入结算。");

        // 隐藏笔刷
        if (visualBrushTransform) visualBrushTransform.gameObject.SetActive(false);

        // 显示结算 UI
        if (finishPromptGroup) finishPromptGroup.SetActive(true);
        if (fullScreenExitButton) fullScreenExitButton.gameObject.SetActive(true);

        isPhaseActive = false; // 停止 Update 中的逻辑检测
    }

    void OnMaskClicked()
    {
        // 这里假设你有单例 Manager，请根据实际情况调整
        Debug.Log("点击返回，通知 Manager 结束关卡。");
        if (MaidGameManager.Instance != null)
        {
            MaidGameManager.Instance.FinishLevelAndExit();
        }
    }
}