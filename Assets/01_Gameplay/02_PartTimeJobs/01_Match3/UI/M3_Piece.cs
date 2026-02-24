using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class M3_Piece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int x, y;
    public M3_ItemType type;
    private M3_Board board;
    private Image uiImage;
    private RectTransform rectTransform;
    private Vector2 startAnchoredPos;
    private bool isDragging = false;
    private Canvas rootCanvas;
    void Awake()
    {
        uiImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
    }

    public void Init(int _x, int _y, M3_Board _board, M3_ItemType _type, Sprite _sprite)
    {
        x = _x; y = _y; board = _board; type = _type;
        if (uiImage != null)
        {
            uiImage.sprite = _sprite;
            uiImage.color = Color.white;
        }
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity; // 重置旋转
        // 如果是障碍物，开始旋转，并且稍微变暗一点或者是特定样子
        if (type == M3_ItemType.Obstacle)transform.DORotate(new Vector3(0, 0, 360), 5f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1);// 持续旋转动画
        else transform.DOKill();// 确保普通物体没有旋转动画残留
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (board.currentState == BoardState.Locked || type == M3_ItemType.Obstacle) return;
        isDragging = true;
        startAnchoredPos = rectTransform.anchoredPosition;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || board.currentState == BoardState.Locked) return;
        if (rootCanvas != null)rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
        else rectTransform.anchoredPosition += eventData.delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        Vector2 currentPos = rectTransform.anchoredPosition;
        Vector2 dir = currentPos - startAnchoredPos;
        if (dir.magnitude < board.cellSize * 0.4f)
        {
            // 距离太短，弹回去
            rectTransform.DOAnchorPos(startAnchoredPos, 0.2f).SetEase(Ease.OutQuad);
            return;
        }

        // 判断方向
        dir.Normalize();
        int offsetX = 0; int offsetY = 0;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) offsetX = dir.x > 0 ? 1 : -1;
        else offsetY = dir.y > 0 ? 1 : -1;
        rectTransform.anchoredPosition = startAnchoredPos;
        // 调用 Board 处理交换
        board.OnPieceSwipe(this, offsetX, offsetY);
    }

    // 移动动画
    public void MoveTo(int newX, int newY, float duration = 0.3f)
    {
        x = newX; y = newY;
        Vector2 target = board.GetAnchoredPosition(x, y);
        rectTransform.DOAnchorPos(target, duration).SetEase(Ease.OutQuad);
    }
}