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
    public Transform boardOrigin;

    [Header("Resources")]
    public GameObject piecePrefab;
    public RectTransform boardPanel;

    // ★ 分离资源引用，方便管理
    public List<Sprite> ingredientSprites; // T0, T1, T2, T3
    public Sprite obstacleSprite;          // 障碍物图片
    public Sprite bombSprite;              // 炸弹图片

    // 为了兼容 OrderManager，保留 itemSprites 属性，通过 Getter 返回
    public List<Sprite> itemSprites => ingredientSprites;

    private M3_Piece[,] allPieces;
    public BoardState currentState = BoardState.Idle;

    void Start()
    {
        if (boardPanel == null) boardPanel = GetComponent<RectTransform>();
        boardPanel.pivot = new Vector2(0.5f, 0.5f);
        allPieces = new M3_Piece[width, height];
        GenerateBoard();
    }

    public Vector2 GetAnchoredPosition(int x, int y)
    {
        float startX = -(width * cellSize) / 2f + cellSize / 2f;
        float startY = -(height * cellSize) / 2f + cellSize / 2f;
        return new Vector2(startX + x * cellSize, startY + y * cellSize);
    }

    void GenerateBoard()
    {
        foreach (Transform child in boardPanel) Destroy(child.gameObject);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // ★ 障碍物生成逻辑：例如 10% 概率生成障碍，且不生成在最上方几行(防止死局)
                if (Random.value < 0.1f && y < height - 2)
                {
                    SpawnPieceAt(x, y, M3_ItemType.Obstacle);
                }
                else
                {
                    // 普通生成逻辑
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
        // 障碍物不参与普通匹配检测
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
        Vector2 finalPos = GetAnchoredPosition(x, y);

        // ★ 根据类型选择图片
        Sprite s = null;
        if (type == M3_ItemType.Obstacle) s = obstacleSprite;
        else if (type == M3_ItemType.Bomb) s = bombSprite;
        else if ((int)type < ingredientSprites.Count) s = ingredientSprites[(int)type];

        piece.Init(x, y, this, type, s);
        allPieces[x, y] = piece;

        if (yOffset > 0)
        {
            rt.anchoredPosition = new Vector2(finalPos.x, finalPos.y + yOffset * cellSize);
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

        // ★ 如果交换的对象是障碍物，禁止交换
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

        // ★ 炸弹逻辑：如果交换的任意一个是炸弹，直接触发爆炸，不需要看是否三消
        if (p1.type == M3_ItemType.Bomb || p2.type == M3_ItemType.Bomb)
        {
            // 如果两个都是炸弹，可以做全屏清空（这里先不做，简单处理为两次爆炸）
            if (p1.type == M3_ItemType.Bomb) ExplodeBomb(p1.x, p1.y);
            if (p2.type == M3_ItemType.Bomb) ExplodeBomb(p2.x, p2.y);

            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(RefillBoard());
        }
        else
        {
            // 普通匹配逻辑
            List<M3_Piece> matches = FindMatches();
            if (matches.Count > 0) yield return StartCoroutine(ProcessMatches(matches));
            else
            {
                // 没匹配，换回来
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
        // 简化的匹配逻辑：忽略障碍和炸弹
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

    // 辅助：判断是否为普通食材
    bool IsNormalItem(M3_Piece p)
    {
        return p != null && p.type != M3_ItemType.Obstacle && p.type != M3_ItemType.Bomb;
    }

    // ★ 核心：处理消除，包含生成炸弹和消除障碍逻辑
    IEnumerator ProcessMatches(List<M3_Piece> matches)
    {
        // 1. 检测是否需要生成炸弹 (本次消除总数 >= 6)
        // 注意：这里简单的用总数判断，严格来说应该判断单次连通块大小，但为了手感爽快，总数>=6就给炸弹也可
        M3_Piece pieceToBecomeBomb = null;
        if (matches.Count >= 6)
        {
            // 选列表中第一个作为炸弹生成点（也可以算中心点）
            pieceToBecomeBomb = matches[Random.Range(0, matches.Count)];
        }

        // 2. 处理消除
        foreach (var piece in matches)
        {
            if (piece == null) continue;

            // 如果这个棋子被选中变成炸弹，先不销毁，只改变类型
            if (piece == pieceToBecomeBomb)
            {
                piece.type = M3_ItemType.Bomb;
                piece.GetComponent<Image>().sprite = bombSprite;
                piece.transform.DOPunchScale(Vector3.one * 0.5f, 0.3f); // 变身特效
                continue;
            }

            // ★ 障碍物消除逻辑：检查被消除物体的上下左右
            CheckAndDamageObstacle(piece.x + 1, piece.y);
            CheckAndDamageObstacle(piece.x - 1, piece.y);
            CheckAndDamageObstacle(piece.x, piece.y + 1);
            CheckAndDamageObstacle(piece.x, piece.y - 1);

            // 正常的消除与飞单逻辑
            TriggerPieceClearEffect(piece);
        }

        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(RefillBoard());
    }

    // 消除单个 Piece 的视觉表现和逻辑（飞向订单）
    void TriggerPieceClearEffect(M3_Piece piece)
    {
        allPieces[piece.x, piece.y] = null; // 逻辑清除

        TrayController targetTray = OrderManager.Instance.GetTargetTray(piece.type);
        if (targetTray != null)
        {
            piece.gameObject.SetActive(false); // 隐藏原图
            FX_Manager.Instance.PlayFlyEffect(
                ingredientSprites[(int)piece.type],
                piece.transform.position,
                targetTray.GetIconPosition(),
                () => { if (targetTray != null) targetTray.AddProgress(1); }
            );
            Destroy(piece.gameObject); // 这里的 Destroy 是销毁原 Piece 物体
        }
        else
        {
            piece.transform.DOScale(Vector3.zero, 0.2f).OnComplete(() => Destroy(piece.gameObject));
        }
    }

    // 检查是否有障碍物并消除
    void CheckAndDamageObstacle(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        M3_Piece p = allPieces[x, y];
        if (p != null && p.type == M3_ItemType.Obstacle)
        {
            // 播放碎裂特效
            p.transform.DOPunchRotation(new Vector3(0, 0, 90), 0.2f);
            p.transform.DOScale(0, 0.2f).OnComplete(() => Destroy(p.gameObject));
            allPieces[x, y] = null; // 消除数据

            // 可选：障碍物消除加分
            M3_GameManager.Instance.AddScore(50);
        }
    }

    // ★ 炸弹爆炸逻辑 (5x5)
    public void ExplodeBomb(int cx, int cy)
    {
        // 炸掉中心（炸弹自己）
        if (allPieces[cx, cy] != null)
        {
            Destroy(allPieces[cx, cy].gameObject);
            allPieces[cx, cy] = null;
        }

        // 播放一个大爆炸特效（需要你在FXManager里加，这里用简单的Log代替）
        // FX_Manager.Instance.PlayExplosion(GetWorldPosition(cx, cy));
        Debug.Log("BOOM!");

        // 遍历周围 5x5
        for (int x = cx - 2; x <= cx + 2; x++)
        {
            for (int y = cy - 2; y <= cy + 2; y++)
            {
                if (x < 0 || x >= width || y < 0 || y >= height) continue;

                M3_Piece p = allPieces[x, y];
                if (p != null)
                {
                    // 炸弹也能炸掉障碍物
                    if (p.type == M3_ItemType.Obstacle)
                    {
                        Destroy(p.gameObject);
                        allPieces[x, y] = null;
                    }
                    // 普通物体则触发飞单效果
                    else if (p.type != M3_ItemType.Bomb)
                    {
                        TriggerPieceClearEffect(p);
                    }
                }
            }
        }

        // 震动屏幕
        boardPanel.DOShakeAnchorPos(0.3f, 10f);
    }

    IEnumerator RefillBoard()
    {
        float speedTime = 0.3f;

        // 1. 下落
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allPieces[x, y] == null)
                {
                    // 向上找，注意：如果中间隔着障碍物，普通的下落逻辑可能会穿模
                    // 简单的处理是：障碍物下方的空位只能由障碍物上方的棋子掉下来
                    // 但为了代码简洁，这里假设障碍物也会像普通方块一样占据空间，上方有空位时障碍物不掉落（它是固定的）
                    // ★ 如果障碍物是“固定”的，我们不能让它上面的东西穿过它掉下来。
                    // 修正逻辑：只能找非固定的东西掉下来

                    for (int k = y + 1; k < height; k++)
                    {
                        M3_Piece pieceAbove = allPieces[x, k];
                        if (pieceAbove != null)
                        {
                            if (pieceAbove.type == M3_ItemType.Obstacle)
                            {
                                // 遇到障碍物，这列的上方掉落被阻断了！
                                // 除非我们允许斜向掉落（太复杂），否则这列目前只能空着
                                break;
                            }

                            // 找到了可移动的上方棋子
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

        // 2. 生成新棋子
        for (int x = 0; x < width; x++)
        {
            // 从最上面开始检查，如果有空位，说明需要生成
            // 但是如果有障碍物挡着，只能在障碍物上方生成
            // 简单处理：只扫描所有的 null 格子
            for (int y = 0; y < height; y++)
            {
                if (allPieces[x, y] == null)
                {
                    int typeIndex = (UnityEngine.Random.Range(0f,1f)>0.05f)?Random.Range(0, ingredientSprites.Count):(int)M3_ItemType.Obstacle; // 5%概率重新生成障碍物
                    SpawnPieceAt(x, y, (M3_ItemType)typeIndex, height);
                }
            }
        }

        yield return new WaitForSeconds(speedTime);

        // 递归检查匹配
        List<M3_Piece> newMatches = FindMatches();
        if (newMatches.Count > 0) yield return StartCoroutine(ProcessMatches(newMatches));
        else currentState = BoardState.Idle;
    }
}