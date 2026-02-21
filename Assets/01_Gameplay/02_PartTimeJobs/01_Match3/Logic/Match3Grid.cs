using UnityEngine;
using UnityEngine.UI;

public enum MatchSpecialType
{
    None = 0,
    LineHorizontal = 1,
    LineVertical = 2,
    Wrapped = 3,
    ColorBomb = 4
}

/// <summary>
/// 8x8三消网格管理 —— 负责棋盘生成与棋子管理
/// </summary>
public class Match3Grid : MonoBehaviour
{
    public const int WIDTH = 8;
    public const int HEIGHT = 8;

    [Header("设置")]
    public float cellSize = 60f;
    public float spacing = 4f;
    public RectTransform gridContainer;
    [Range(3, 6)] public int activeTypeCount = 6;

    // 棋子类型（用颜色代替食物图标）
    public static readonly Color[] PieceColors = new Color[]
    {
        new Color(0.9f, 0.2f, 0.2f), // 烧鸡-红
        new Color(0.2f, 0.7f, 0.2f), // 青菜-绿
        new Color(0.2f, 0.4f, 0.9f), // 酒坛-蓝
        new Color(0.9f, 0.9f, 0.2f), // 饺子-黄
        new Color(0.8f, 0.4f, 0.1f), // 烤鸭-橙
        new Color(0.7f, 0.2f, 0.9f), // 汤圆-紫
    };

    // 棋盘数据
    public int[,] Board { get; private set; }
    public GameObject[,] PieceObjects { get; private set; }
    public int[,] IceLayers { get; private set; }
    public int[,] ChainLayers { get; private set; }

    public event System.Action<int, int, Vector2> OnPiecePointerDown;
    public event System.Action<int, int, Vector2> OnPiecePointerEnter;
    public event System.Action<int, int, Vector2> OnPiecePointerUp;

    private const int SPECIAL_MULTIPLIER = 10;

    void Awake()
    {
        Board = new int[WIDTH, HEIGHT];
        PieceObjects = new GameObject[WIDTH, HEIGHT];
        IceLayers = new int[WIDTH, HEIGHT];
        ChainLayers = new int[WIDTH, HEIGHT];
    }

    public static int EncodePiece(int baseType, MatchSpecialType special)
    {
        return baseType + ((int)special * SPECIAL_MULTIPLIER);
    }

    public static int GetBaseType(int pieceValue)
    {
        if (pieceValue < 0) return -1;
        return pieceValue % SPECIAL_MULTIPLIER;
    }

    public static MatchSpecialType GetSpecialType(int pieceValue)
    {
        if (pieceValue < 0) return MatchSpecialType.None;
        return (MatchSpecialType)(pieceValue / SPECIAL_MULTIPLIER);
    }

    /// <summary>初始化棋盘</summary>
    public void InitBoard()
    {
        ClearBoard();
        Board = new int[WIDTH, HEIGHT];
        PieceObjects = new GameObject[WIDTH, HEIGHT];
        IceLayers = new int[WIDTH, HEIGHT];
        ChainLayers = new int[WIDTH, HEIGHT];

        for (int x = 0; x < WIDTH; x++)
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                int type = GetRandomTypeNoMatch(x, y);
                Board[x, y] = EncodePiece(type, MatchSpecialType.None);
                CreatePieceObject(x, y, Board[x, y]);
            }
        }
    }

    /// <summary>获取不会立即形成三连的随机类型</summary>
    private int GetRandomTypeNoMatch(int x, int y)
    {
        int typeCount = Mathf.Clamp(activeTypeCount, 3, PieceColors.Length);
        int maxAttempts = 20;
        for (int i = 0; i < maxAttempts; i++)
        {
            int type = Random.Range(0, typeCount);
            // 检查水平
            if (x >= 2 && GetBaseType(Board[x - 1, y]) == type && GetBaseType(Board[x - 2, y]) == type)
                continue;
            // 检查垂直
            if (y >= 2 && GetBaseType(Board[x, y - 1]) == type && GetBaseType(Board[x, y - 2]) == type)
                continue;
            return type;
        }
        return Random.Range(0, typeCount);
    }

    /// <summary>创建棋子物体</summary>
    private void CreatePieceObject(int x, int y, int pieceValue)
    {
        GameObject piece = new GameObject($"Piece_{x}_{y}");
        RectTransform rt = piece.AddComponent<RectTransform>();

        if (gridContainer != null)
            rt.SetParent(gridContainer, false);
        else
            rt.SetParent(transform, false);

        float totalW = (cellSize + spacing) * WIDTH;
        float totalH = (cellSize + spacing) * HEIGHT;
        float posX = x * (cellSize + spacing) - totalW / 2f + cellSize / 2f;
        float posY = y * (cellSize + spacing) - totalH / 2f + cellSize / 2f;
        rt.anchoredPosition = new Vector2(posX, posY);
        rt.sizeDelta = new Vector2(cellSize, cellSize);

        Image img = piece.AddComponent<Image>();
        UpdatePieceVisual(img, pieceValue);

        var view = piece.AddComponent<Match3PieceView>();
        view.Init(this, x, y);

        PieceObjects[x, y] = piece;
    }

    private void UpdatePieceVisual(Image img, int pieceValue, int x = -1, int y = -1)
    {
        int baseType = Mathf.Clamp(GetBaseType(pieceValue), 0, PieceColors.Length - 1);
        var special = GetSpecialType(pieceValue);
        Color baseColor = PieceColors[baseType];

        switch (special)
        {
            case MatchSpecialType.LineHorizontal:
                img.color = Color.Lerp(baseColor, Color.white, 0.25f);
                break;
            case MatchSpecialType.LineVertical:
                img.color = Color.Lerp(baseColor, Color.cyan, 0.25f);
                break;
            case MatchSpecialType.Wrapped:
                img.color = Color.Lerp(baseColor, new Color(1f, 0.75f, 0.25f), 0.4f);
                break;
            case MatchSpecialType.ColorBomb:
                img.color = new Color(0.9f, 0.9f, 0.95f);
                break;
            default:
                img.color = baseColor;
                break;
        }

        if (x >= 0 && y >= 0)
        {
            if (IceLayers[x, y] > 0)
                img.color = Color.Lerp(img.color, new Color(0.75f, 0.9f, 1f), 0.35f);
            if (ChainLayers[x, y] > 0)
                img.color = Color.Lerp(img.color, new Color(0.25f, 0.25f, 0.25f), 0.4f);
        }
    }

    public void NotifyPointerDown(int x, int y, Vector2 screenPos)
    {
        OnPiecePointerDown?.Invoke(x, y, screenPos);
    }

    public void NotifyPointerEnter(int x, int y, Vector2 screenPos)
    {
        OnPiecePointerEnter?.Invoke(x, y, screenPos);
    }

    public void NotifyPointerUp(int x, int y, Vector2 screenPos)
    {
        OnPiecePointerUp?.Invoke(x, y, screenPos);
    }

    /// <summary>交换两个格子</summary>
    public void SwapPieces(int x1, int y1, int x2, int y2)
    {
        int temp = Board[x1, y1];
        Board[x1, y1] = Board[x2, y2];
        Board[x2, y2] = temp;

        // 交换视觉
        var tempObj = PieceObjects[x1, y1];
        PieceObjects[x1, y1] = PieceObjects[x2, y2];
        PieceObjects[x2, y2] = tempObj;

        UpdatePiecePosition(x1, y1);
        UpdatePiecePosition(x2, y2);
    }

    /// <summary>更新棋子位置</summary>
    public void UpdatePiecePosition(int x, int y)
    {
        if (PieceObjects[x, y] == null) return;
        var rt = PieceObjects[x, y].GetComponent<RectTransform>();
        float totalW = (cellSize + spacing) * WIDTH;
        float totalH = (cellSize + spacing) * HEIGHT;
        float posX = x * (cellSize + spacing) - totalW / 2f + cellSize / 2f;
        float posY = y * (cellSize + spacing) - totalH / 2f + cellSize / 2f;
        rt.anchoredPosition = new Vector2(posX, posY);
        PieceObjects[x, y].name = $"Piece_{x}_{y}";
        var view = PieceObjects[x, y].GetComponent<Match3PieceView>();
        if (view != null) view.SetCoordinates(x, y);

        var img = PieceObjects[x, y].GetComponent<Image>();
        if (img != null) UpdatePieceVisual(img, Board[x, y], x, y);
    }

    /// <summary>移除棋子</summary>
    public void RemovePiece(int x, int y)
    {
        if (PieceObjects[x, y] != null)
        {
            Destroy(PieceObjects[x, y]);
            PieceObjects[x, y] = null;
        }
        Board[x, y] = -1;
    }

    /// <summary>在指定位置填充新棋子</summary>
    public void FillPiece(int x, int y)
    {
        int typeCount = Mathf.Clamp(activeTypeCount, 3, PieceColors.Length);
        int type = Random.Range(0, typeCount);
        Board[x, y] = EncodePiece(type, MatchSpecialType.None);
        CreatePieceObject(x, y, Board[x, y]);
    }

    public void SetPieceValue(int x, int y, int pieceValue)
    {
        Board[x, y] = pieceValue;
        if (PieceObjects[x, y] == null)
        {
            CreatePieceObject(x, y, pieceValue);
        }
        else
        {
            var img = PieceObjects[x, y].GetComponent<Image>();
            if (img != null) UpdatePieceVisual(img, pieceValue, x, y);
            UpdatePiecePosition(x, y);
        }
    }

    public bool IsLocked(int x, int y)
    {
        return ChainLayers[x, y] > 0;
    }

    public int DamageChain(int x, int y, int amount = 1)
    {
        if (ChainLayers[x, y] <= 0) return 0;
        ChainLayers[x, y] = Mathf.Max(0, ChainLayers[x, y] - amount);
        UpdatePiecePosition(x, y);
        return ChainLayers[x, y];
    }

    public int DamageIce(int x, int y, int amount = 1)
    {
        if (IceLayers[x, y] <= 0) return 0;
        IceLayers[x, y] = Mathf.Max(0, IceLayers[x, y] - amount);
        UpdatePiecePosition(x, y);
        return IceLayers[x, y];
    }

    public int CountObstacleCells()
    {
        int count = 0;
        for (int x = 0; x < WIDTH; x++)
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                if (IceLayers[x, y] > 0 || ChainLayers[x, y] > 0) count++;
            }
        }
        return count;
    }

    public void GenerateObstacles(int iceCount, int chainCount, int maxIceLayer = 2, int maxChainLayer = 2)
    {
        PlaceObstacleLayers(IceLayers, iceCount, maxIceLayer);
        PlaceObstacleLayers(ChainLayers, chainCount, maxChainLayer);

        for (int x = 0; x < WIDTH; x++)
            for (int y = 0; y < HEIGHT; y++)
                UpdatePiecePosition(x, y);
    }

    private void PlaceObstacleLayers(int[,] target, int count, int maxLayer)
    {
        int placed = 0;
        int guard = 0;
        while (placed < count && guard < 500)
        {
            guard++;
            int x = Random.Range(0, WIDTH);
            int y = Random.Range(0, HEIGHT);
            if (target[x, y] > 0) continue;
            target[x, y] = Random.Range(1, Mathf.Max(2, maxLayer + 1));
            placed++;
        }
    }

    /// <summary>清空棋盘</summary>
    public void ClearBoard()
    {
        if (PieceObjects == null) return;
        for (int x = 0; x < WIDTH; x++)
            for (int y = 0; y < HEIGHT; y++)
                if (PieceObjects[x, y] != null)
                    Destroy(PieceObjects[x, y]);
    }
}
