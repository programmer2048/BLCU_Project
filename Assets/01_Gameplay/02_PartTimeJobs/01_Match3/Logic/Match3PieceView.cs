using UnityEngine;
using UnityEngine.EventSystems;

public class Match3PieceView : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
{
    private Match3Grid _grid;
    private int _x;
    private int _y;

    public void Init(Match3Grid grid, int x, int y)
    {
        _grid = grid;
        _x = x;
        _y = y;
    }

    public void SetCoordinates(int x, int y)
    {
        _x = x;
        _y = y;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _grid?.NotifyPointerDown(_x, _y, eventData.position);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _grid?.NotifyPointerEnter(_x, _y, eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _grid?.NotifyPointerUp(_x, _y, eventData.position);
    }
}