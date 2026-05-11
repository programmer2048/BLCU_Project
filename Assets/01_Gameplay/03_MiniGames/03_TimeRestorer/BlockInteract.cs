using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BlockInteract : MonoBehaviour
{
    private bool isDragging = false;
    private Vector2 offset;
    private Vector3 downPos;
    private float lastClickTime = 0f;
    private const float doubleClickThreshold = 0.3f;
    public bool isPhysicsActive = false;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // 初始状态下设为 Kinematic，让方块在搭建阶段死死固定在空中
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void OnMouseDown()
    {
        if (isPhysicsActive) return;

        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset = (Vector2)transform.position - mouseWorldPos;
        downPos = Input.mousePosition;

        isDragging = true;

        // 【核心】拖拽时变成 Dynamic 刚体，这样遇到其他方块就会被物理法则真实挡住
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;               // 搭建时不掉落
        rb.freezeRotation = true;           // 拖拽时不因为碰撞而乱转
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 防止鼠标拖得太快导致穿模
    }

    private void FixedUpdate()
    {
        // 物理帧更新：利用速度去追赶鼠标
        if (!isPhysicsActive && isDragging)
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 targetPosition = mouseWorldPos + offset;

            // 计算位移差，并转换为速度。乘以 25f 是为了平滑跟随，数值可调
            Vector2 moveDir = (targetPosition - rb.position);
            rb.linearVelocity = moveDir * 25f;
        }
    }

    private void OnMouseUp()
    {
        if (isPhysicsActive) return;
        isDragging = false;

        // 【核心】松开鼠标时变回 Kinematic，再次固定在当前位置
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        // 点击事件：保留原有的双击和单击旋转逻辑
        if (Vector3.Distance(downPos, Input.mousePosition) < 5f)
        {
            float timeSinceLastClick = Time.time - lastClickTime;
            if (timeSinceLastClick <= doubleClickThreshold)
            {
                // 双击：翻转
                transform.Rotate(0, 0, 90f);
                Vector3 scale = transform.localScale;
                scale.x *= -1;
                transform.localScale = scale;
                lastClickTime = 0f;
            }
            else
            {
                // 单击：旋转
                transform.Rotate(0, 0, -90f);
                lastClickTime = Time.time;
            }
        }
    }
}