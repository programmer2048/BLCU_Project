using UnityEngine;

public class BlockInteract : MonoBehaviour
{
    private bool isDragging = false;
    private Vector3 offset;
    private Vector3 downPos;
    private float lastClickTime = 0f;
    private const float doubleClickThreshold = 0.3f;
    public bool isPhysicsActive = false;

    private void OnMouseDown()
    {
        if (isPhysicsActive) return;

        offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
        downPos = Input.mousePosition;
        isDragging = true;
    }

    private void OnMouseDrag()
    {
        if (isPhysicsActive || !isDragging) return;
        Vector3 newPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) + offset;
        newPosition.z = 0;
        transform.position = newPosition;
    }

    private void OnMouseUp()
    {
        if (isPhysicsActive) return;
        isDragging = false;

        // 如果鼠标按下和抬起的位置很近，判定为点击而不是拖拽
        if (Vector3.Distance(downPos, Input.mousePosition) < 5f)
        {
            float timeSinceLastClick = Time.time - lastClickTime;
            if (timeSinceLastClick <= doubleClickThreshold)
            {
                // 双击：恢复上一次单击导致的旋转，并执行X轴对称翻转
                transform.Rotate(0, 0, 90f);
                Vector3 scale = transform.localScale;
                scale.x *= -1;
                transform.localScale = scale;
                lastClickTime = 0f; // 重置连击
            }
            else
            {
                // 单击：顺时针旋转90度
                transform.Rotate(0, 0, -90f);
                lastClickTime = Time.time;
            }
        }
    }
}