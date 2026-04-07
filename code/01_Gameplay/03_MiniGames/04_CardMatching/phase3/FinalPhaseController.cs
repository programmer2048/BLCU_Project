using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FinalPhaseController : MonoBehaviour
{
    [Header("--- 核心组件引用 ---")]
    public ScratchCard finalPostcard;
    public GameObject finishPromptGroup;
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
        if (finishPromptGroup) finishPromptGroup.SetActive(false);

        if (fullScreenExitButton)
        {
            fullScreenExitButton.gameObject.SetActive(false);
            fullScreenExitButton.onClick.RemoveAllListeners();
            fullScreenExitButton.onClick.AddListener(OnMaskClicked);
        }
        if (visualBrushTransform)
        {
            visualBrushImage = visualBrushTransform.GetComponent<Image>();
            if (visualBrushImage == null) visualBrushImage = visualBrushTransform.gameObject.AddComponent<Image>();
            if (cursorSprite != null)
            {
                visualBrushImage.sprite = cursorSprite;
                visualBrushImage.SetNativeSize();
            }
            visualBrushImage.raycastTarget = false;
            visualBrushTransform.gameObject.SetActive(false);
        }
    }
    public void InitPhase()
    {
        Debug.Log($"FinalPhaseController: InitPhase 被调用. Data: {(postcardData != null ? postcardData.name : "NULL")}");

        isPhaseActive = true;
        this.gameObject.SetActive(true);

        if (finalPostcard != null)
        {
            if (postcardData != null)
            {
                finalPostcard.Init(postcardData, 1024);
            }
            else
            {
                Debug.LogError("FinalPhaseController: postcardData 未赋值！TopImage 将无法显示。");
            }
        }
        if (visualBrushTransform != null)
        {
            visualBrushTransform.gameObject.SetActive(true);
            visualBrushTransform.SetAsLastSibling();
        }
    }

    void Update()
    {
        if (!isPhaseActive) return;
        if (visualBrushTransform != null)
        {
            visualBrushTransform.position = Input.mousePosition;
        }
        // 只有鼠标按下 + 有卡片 + 卡片没完成 时才运算
        if (Input.GetMouseButton(0) && finalPostcard != null && !finalPostcard.IsFinished())
        {
            if (finalPostcard.GetHitUV(Input.mousePosition, out Vector2 uv))
            {
                if (brushPattern != null)
                {
                    finalPostcard.DrawAt(uv, brushLogicSize, brushPattern);
                }
            }
        }
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
        Debug.Log("点击返回，通知 Manager 结束关卡。");
        if (MaidGameManager.Instance != null)
        {
            MaidGameManager.Instance.FinishLevelAndExit();
        }
    }
}