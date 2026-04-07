using UnityEngine;
using UnityEngine.UI;

public class DifferenceSpot : MonoBehaviour
{
    [Header("配置")]
    public GameObject markIcon; // 找到后显示的标记（比如红圈图片）
    public Button myButton;     // 自身的按钮组件

    private bool isFound = false;

    void Start()
    {
        // 确保标记一开始是隐藏的
        if (markIcon != null) markIcon.SetActive(false);

        // 自动获取按钮并绑定事件
        if (myButton == null) myButton = GetComponent<Button>();
        myButton.onClick.AddListener(OnFound);
    }

    void OnFound()
    {
        if (isFound) return;
        isFound = true;
        if (markIcon != null) markIcon.SetActive(true);
        myButton.interactable = false;
        DifferenceGameManager.Instance.OnDifferenceFound();
        // 播放音效 (可选)
        // AudioManager.Play("CorrectSound");
    }
}