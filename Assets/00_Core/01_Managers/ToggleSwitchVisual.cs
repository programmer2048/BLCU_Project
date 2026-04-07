using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ToggleSwitchVisual : MonoBehaviour
{
    public RectTransform handleRect;
    public Image backgroundImage;
    public Color offColor = Color.gray;
    public Color onColor = Color.green;
    private Toggle toggle;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnToggle);

        // 初始化状态
        OnToggle(toggle.isOn);
    }

    void OnToggle(bool isOn)
    {
        // 1. 改变背景颜色
        //backgroundImage.color = isOn ? onColor : offColor;
        float targetX = isOn ? 45f : 15f;

        // 简单位移
        handleRect.anchoredPosition = new Vector2(targetX, 0);

        // 如果想平滑移动 (需要写个简单的插值 Coroutine)
        // StartCoroutine(MoveHandle(targetX));
    }
}