using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class M3_Board : MonoBehaviour
{
    [Header("Board Settings")]
    public int width = 6;
    public int height = 8;
    public float cellSize = 100f;
    [Tooltip("棋子之间的间隙")]
    public float spacing = 10f; // <--- 新增：间隙设置
    public Transform boardOrigin;

    [Header("Resources")]
    public GameObject piecePrefab;
    public RectTransform boardPanel;

    public List<Sprite> ingredientSprites; // T0, T1, T2, T3
    public Sprite obstacleSprite;          // 障碍物图片
    public Sprite bombSprite;              // 炸弹图片

    public List<Sprite> itemSprites => ingredientSprites;

    private M3_Piece[,] allPieces;
    public BoardState currentState = BoardState.Idle;

    // --- 辅助属性：计算带有间隙的实际步长 ---
    private float StepSize => cellSize + spacing;

    void Start()
    {
        if (boardPanel == null) boardPanel = GetComponent<RectTransform>();
        boardPanel.pivot = new Vector2(0.5f, 0.5f);
        allPieces = new M3_Piece[width, height];
        GenerateBoard();
    }

    /// <summary>
    /// 核心修改：重新计算带间隙的坐标，并保证整体棋盘完美居中
    /// </summary>
    public Vector2 GetAnchoredPosition(int x, int y)
    {
        // 棋盘占据的总宽度和总高度 (包含所有棋子和中间的间隙)
        float totalWidth = width * StepSize - spacing;
        float totalHeight = height * StepSize - spacing;

        // 以中心点(0,0)为基准，计算左下角第一个棋子(0,0)的中心点坐标
        float startX = -totalWidth / 2f + cellSize / 2f;
        float startY = -totalHeight / 2f + cellSize / 2f;

        return new Vector2(startX + x * StepSize, startY + y * StepSize);
    }

    void GenerateBoard()
    {
        foreach (Transform child in boardPanel) Destroy(child.gameObject);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (Random.value < 0.1f && y < height - 2)
                {
                    SpawnPieceAt(x, y, M3_ItemType.Obstacle);
                }
                else
                {
                    M3_ItemType type = M3_ItemType.T0;
                    int maxIterations = 100;
                    do
                    {
                        type = (M3_ItemType)Random.Range(0, ingredientSprites.Count);
                        maxIterations--;
                    }
                    while (HasMatchAt(x, y, type) && maxIterations > 0);
                    SpawnPieceAt(x, y, type);
                }
            }
        }
    }

    bool HasMatchAt(int x, int y, M3_ItemType type)
    {
        if (type == M3_ItemType.Obstacle || type == M3_ItemType.Bomb) return false;

        if (x >= 2 && CheckType(x - 1, y, type) && CheckType(x - 2, y, type)) return true;
        if (y >= 2 && CheckType(x, y - 1, type) && CheckType(x, y - 2, type)) return true;
        return false;
    }

    bool CheckType(int x, int y, M3_ItemType type)
    {
        if (allPieces[x, y] == null) return false;
        return allPieces[x, y].type == type;
    }

    void SpawnPieceAt(int x, int y, M3_ItemType type, int yOffset = 0)
    {
        GameObject go = Instantiate(piecePrefab, boardPanel);
        M3_Piece piece = go.GetComponent<M3_Piece>();
        RectTransform rt = go.GetComponent<RectTransform>();

        // 获取目标最终坐标
        Vector2 finalPos = GetAnchoredPosition(x, y);

        Sprite s = null;
        if (type == M3_ItemType.Obstacle) s = obstacleSprite;
        else if (type == M3_ItemType.Bomb) s = bombSprite;
        else if ((int)type < ingredientSprites.Count) s = ingredientSprites[(int)type];

        piece.Init(x, y, this, type, s);
        allPieces[x, y] = piece;

        // 核心修改：如果是从上方掉落（yOffset > 0），掉落的起始高度也要考虑间隙步长
        if (yOffset > 0)
        {
            rt.anchoredPosition = new Vector2(finalPos.x, finalPos.y + yOffset * StepSize);
            piece.MoveTo(x, y, 0.4f);
        }
        else
        {
            rt.anchoredPosition = finalPos;
        }
    }

    public void OnPieceSwipe(M3_Piece piece, int offsetX, int offsetY)
    {
        int targetX = piece.x + offsetX;
        int targetY = piece.y + offsetY;
        if (targetX < 0 || targetX >= width || targetY < 0 || targetY >= height) return;

        M3_Piece targetPiece = allPieces[targetX, targetY];

        if (targetPiece != null && targetPiece.type == M3_ItemType.Obstacle) return;

        StartCoroutine(SwapAndCheck(piece, targetPiece));
    }

    IEnumerator SwapAndCheck(M3_Piece p1, M3_Piece p2)
    {
        currentState = BoardState.Locked;
        SwapData(p1, p2);
        p1.MoveTo(p1.x, p1.y);
        p2.MoveTo(p2.x, p2.y);
        yield return new WaitForSeconds(0.35f);

        if (p1.type == M3_ItemType.Bomb || p2.type == M3_ItemType.Bomb)
        {
            if (p1.type == M3_ItemType.Bomb) ExplodeBomb(p1.x, p1.y);
            if (p2.type == M3_ItemType.Bomb) ExplodeBomb(p2.x, p2.y);

            yield return new WaitForSeconds(0.2f);
            yield return StartCoroutine(RefillBoard());
        }
        else
        {
            List<M3_Piece> matches = FindMatches();
            if (matches.Count > 0) yield return StartCoroutine(ProcessMatches(matches));
            else
            {
                SwapData(p1, p2);
                p1.MoveTo(p1.x, p1.y);
                p2.MoveTo(p2.x, p2.y);
                yield return new WaitForSeconds(0.35f);
                currentState = BoardState.Idle;
            }
        }
    }

    void SwapData(M3_Piece p1, M3_Piece p2)
    {
        allPieces[p1.x, p1.y] = p2; allPieces[p2.x, p2.y] = p1;
        int tx = p1.x; int ty = p1.y; p1.x = p2.x; p1.y = p2.y; p2.x = tx; p2.y = ty;
    }

    List<M3_Piece> FindMatches()
    {
        HashSet<M3_Piece> matchedSet = new HashSet<M3_Piece>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 2; x++)
            {
                M3_Piece p1 = allPieces[x, y], p2 = allPieces[x + 1, y], p3 = allPieces[x + 2, y];
                if (IsNormalItem(p1) && IsNormalItem(p2) && IsNormalItem(p3) && p1.type == p2.type && p2.type == p3.type)
                {
                    matchedSet.Add(p1); matchedSet.Add(p2); matchedSet.Add(p3);
                }
            }
        }
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 2; y++)
            {
                M3_Piece p1 = allPieces[x, y], p2 = allPieces[x, y + 1], p3 = allPieces[x, y + 2];
                if (IsNormalItem(p1) && IsNormalItem(p2) && IsNormalItem(p3) && p1.type == p2.type && p2.type == p3.type)
                {
                    matchedSet.Add(p1); matchedSet.Add(p2); matchedSet.Add(p3);
                }
            }
        }
        return matchedSet.ToList();
    }

    bool IsNormalItem(M3_Piece p)
    {
        return p != null && p.type != M3_ItemType.Obstacle && p.type != M3_ItemType.Bomb;
    }

    IEnumerator ProcessMatches(List<M3_Piece> matches)
    {
        M3_Piece pieceToBecomeBomb = null;
        if (matches.Count >= 7)
        {
            pieceToBecomeBomb = matches[Random.Range(0, matches.Count)];
        }

        foreach (var piece in matches)
        {
            if (piece == null) continue;

            if (piece == pieceToBecomeBomb)
            {
                piece.type = M3_ItemType.Bomb;
                piece.GetComponent<Image>().sprite = bombSprite;
                piece.transform.DOPunchScale(Vector3.one * 0.5f, 0.3f);
                continue;
            }

            CheckAndDamageObstacle(piece.x + 1, piece.y);
            CheckAndDamageObstacle(piece.x - 1, piece.y);
            CheckAndDamageObstacle(piece.x, piece.y + 1);
            CheckAndDamageObstacle(piece.x, piece.y - 1);

            TriggerPieceClearEffect(piece);
        }

        yield return StartCoroutine(RefillBoard());
    }

    void TriggerPieceClearEffect(M3_Piece piece)
    {
        allPieces[piece.x, piece.y] = null;

        TrayController targetTray = OrderManager.Instance.GetTargetTray(piece.type);
        if (targetTray != null)
        {
            piece.gameObject.SetActive(false);
            FX_Manager.Instance.PlayFlyEffect(
                ingredientSprites[(int)piece.type],
                piece.transform.position,
                targetTray.GetIconPosition(),
                () => { if (targetTray != null) targetTray.AddProgress(1); }
            );
            Destroy(piece.gameObject);
        }
        else
        {
            piece.transform.DOScale(Vector3.zero, 0.2f).OnComplete(() => Destroy(piece.gameObject));
        }
    }

    void CheckAndDamageObstacle(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        M3_Piece p = allPieces[x, y];
        if (p != null && p.type == M3_ItemType.Obstacle)
        {
            p.transform.DOPunchRotation(new Vector3(0, 0, 90), 0.2f);
            p.transform.DOScale(0, 0.2f).OnComplete(() => Destroy(p.gameObject));

            FX_Manager.Instance.PlayFlyEffect(
                FX_Manager.Instance.dollarSprite,
                allPieces[x, y].AnchoredPos, // 注意：如果这里报错，请确保 M3_Piece 包含 public Vector2 AnchoredPos 属性
                M3_GameManager.Instance.GetRevenueUIPosition(),
                () => M3_GameManager.Instance.AddScore(2)
            );
            allPieces[x, y] = null;
        }
    }

    public void ExplodeBomb(int cx, int cy)
    {
        if (allPieces[cx, cy] != null)
        {
            Destroy(allPieces[cx, cy].gameObject);
            allPieces[cx, cy] = null;
        }

        for (int x = cx - 2; x <= cx + 2; x++)
        {
            for (int y = cy - 2; y <= cy + 2; y++)
            {
                if (x < 0 || x >= width || y < 0 || y >= height) continue;

                M3_Piece p = allPieces[x, y];
                if (p != null)
                {
                    if (p.type == M3_ItemType.Obstacle)
                    {
                        Destroy(p.gameObject);
                        allPieces[x, y] = null;
                    }
                    else if (p.type != M3_ItemType.Bomb)
                    {
                        TriggerPieceClearEffect(p);
                    }
                }
            }
        }
        boardPanel.DOShakeAnchorPos(0.3f, 10f);
    }

    IEnumerator RefillBoard()
    {
        float speedTime = 0.3f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allPieces[x, y] == null)
                {
                    for (int k = y + 1; k < height; k++)
                    {
                        M3_Piece pieceAbove = allPieces[x, k];
                        if (pieceAbove != null)
                        {
                            if (pieceAbove.type == M3_ItemType.Obstacle) break;

                            allPieces[x, k] = null;
                            allPieces[x, y] = pieceAbove;
                            pieceAbove.MoveTo(x, y, speedTime);
                            break;
                        }
                    }
                }
            }
        }
        yield return new WaitForSeconds(speedTime);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allPieces[x, y] == null)
                {
                    int typeIndex = (UnityEngine.Random.Range(0f, 1f) > 0.05f) ? Random.Range(0, ingredientSprites.Count) : (int)M3_ItemType.Obstacle;
                    SpawnPieceAt(x, y, (M3_ItemType)typeIndex, height);
                }
            }
        }

        yield return new WaitForSeconds(speedTime);

        List<M3_Piece> newMatches = FindMatches();
        if (newMatches.Count > 0) yield return StartCoroutine(ProcessMatches(newMatches));
        else currentState = BoardState.Idle;
    }
}