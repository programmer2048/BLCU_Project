using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public enum Match3Difficulty
{
    Easy,
    Medium,
    Hard
}

/// <summary>
/// 三消总控 —— 游戏流程控制
/// </summary>
public class Match3Controller : MonoBehaviour
{
    [Header("组件引用")]
    public Match3Grid grid;
    public Match3Judge judge;

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI resultText;
    public GameObject resultPanel;
    public Button backButton;
    public TextMeshProUGUI difficultyText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI progressText;
    public Image progressFill;
    public TextMeshProUGUI goalText;
    public TextMeshProUGUI toolText;

    [Header("游戏设置")]
    public float gameDuration = 60f;
    public float feeMultiplier = 0.5f;
    public float swapAnimDuration = 0.15f;
    public float clearAnimDuration = 0.2f;
    public float comboInterval = 0.3f;
    public float dragThresholdPixels = 15f;
    public Match3Difficulty difficulty = Match3Difficulty.Medium;
    public int baseFeePerRound = 60;
    public int progressRequiredPerRound = 1000;

    private float _timeRemaining;
    private int _score;
    private bool _isPlaying;
    private bool _isResolving;

    private int _dragStartX = -1;
    private int _dragStartY = -1;
    private int _dragHoverX = -1;
    private int _dragHoverY = -1;
    private Vector2 _dragStartScreenPos;
    private bool _isDragging;
    private int _combo;
    private int _workProgress;
    private int _completedRounds;
    private int _targetScore;
    private int _targetObstacleClear;
    private int _initialObstacleCount;
    private int _remainingShuffles;
    private float _nextHintReadyTime;
    private const float HINT_COOLDOWN = 15f;

    private DifficultyConfig _cfg;

    private struct DifficultyConfig
    {
        public float duration;
        public int pieceTypes;
        public float scoreMultiplier;
        public int targetScore;
        public int obstacleCount;
        public int chainCount;
        public int shuffleCount;
    }

    void Start()
    {
        if (grid == null) grid = GetComponentInChildren<Match3Grid>();
        if (judge == null) judge = GetComponentInChildren<Match3Judge>();

        if (backButton != null)
            backButton.onClick.AddListener(ReturnToMainUI);

        if (grid != null)
        {
            grid.OnPiecePointerDown += OnPiecePointerDown;
            grid.OnPiecePointerEnter += OnPiecePointerEnter;
            grid.OnPiecePointerUp += OnPiecePointerUp;
        }

        ApplyDifficulty(difficulty);
    }

    void OnDestroy()
    {
        if (grid != null)
        {
            grid.OnPiecePointerDown -= OnPiecePointerDown;
            grid.OnPiecePointerEnter -= OnPiecePointerEnter;
            grid.OnPiecePointerUp -= OnPiecePointerUp;
        }
    }

    void OnEnable()
    {
        EventBus.Subscribe(GameEvent.OnMatch3Start, StartGame);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe(GameEvent.OnMatch3Start, StartGame);
    }

    public void StartGame()
    {
        Debug.Log("[Match3] 游戏开始！");
        _score = 0;
        _combo = 0;
        _workProgress = 0;
        _completedRounds = 0;
        _targetScore = _cfg.targetScore;
        _targetObstacleClear = _cfg.obstacleCount + _cfg.chainCount;
        _remainingShuffles = _cfg.shuffleCount;
        _nextHintReadyTime = 0f;
        _timeRemaining = _cfg.duration;
        _isPlaying = true;
        _isResolving = false;
        _isDragging = false;
        _dragStartX = _dragStartY = _dragHoverX = _dragHoverY = -1;

        if (resultPanel != null) resultPanel.SetActive(false);
        UpdateUI();

        grid.activeTypeCount = _cfg.pieceTypes;
        grid.InitBoard();
        grid.GenerateObstacles(_cfg.obstacleCount, _cfg.chainCount, difficulty == Match3Difficulty.Hard ? 3 : 2, difficulty == Match3Difficulty.Hard ? 3 : 2);
        _initialObstacleCount = grid.CountObstacleCells();
        judge.Init(grid);

        StartCoroutine(BeginAfterInitialStabilize());
    }

    private IEnumerator BeginAfterInitialStabilize()
    {
        yield return ResolveBoardCascade(null);
        EnsureBoardHasMoves();
    }

    void Update()
    {
        if (!_isPlaying) return;

        _timeRemaining -= Time.deltaTime;
        if (timerText != null)
            timerText.text = $"时间: {Mathf.CeilToInt(_timeRemaining)}s";

        if (_timeRemaining <= 0)
        {
            EndGame();
        }

        if (!_isResolving)
        {
            if (Input.GetMouseButtonDown(1))
                UseShuffleTool();

            if (Input.GetKeyDown(KeyCode.Space))
                TryUseHintTool();
        }
    }

    private void OnPiecePointerDown(int x, int y, Vector2 screenPos)
    {
        if (!_isPlaying || _isResolving) return;
        if (grid.IsLocked(x, y)) return;
        _dragStartX = x;
        _dragStartY = y;
        _dragHoverX = x;
        _dragHoverY = y;
        _dragStartScreenPos = screenPos;
        _isDragging = true;
    }

    private void OnPiecePointerEnter(int x, int y, Vector2 screenPos)
    {
        if (!_isDragging || _isResolving || !_isPlaying) return;
        if (!judge.IsAdjacent(_dragStartX, _dragStartY, x, y)) return;
        _dragHoverX = x;
        _dragHoverY = y;
    }

    private void OnPiecePointerUp(int x, int y, Vector2 screenPos)
    {
        if (!_isDragging || _isResolving || !_isPlaying)
        {
            _isDragging = false;
            return;
        }

        float dragDistance = Vector2.Distance(_dragStartScreenPos, screenPos);
        bool validDrag = dragDistance >= dragThresholdPixels;

        if (!validDrag || !judge.IsAdjacent(_dragStartX, _dragStartY, _dragHoverX, _dragHoverY))
        {
            _isDragging = false;
            return;
        }

        StartCoroutine(TrySwapAndResolve(_dragStartX, _dragStartY, _dragHoverX, _dragHoverY));
        _isDragging = false;
    }

    private IEnumerator TrySwapAndResolve(int x1, int y1, int x2, int y2)
    {
        if (grid.IsLocked(x1, y1) || grid.IsLocked(x2, y2))
            yield break;

        _isResolving = true;
        grid.SwapPieces(x1, y1, x2, y2);
        yield return new WaitForSeconds(swapAnimDuration);

        var hasMatch = judge.FindMatches().Count > 0;
        var special1 = Match3Grid.GetSpecialType(grid.Board[x1, y1]);
        var special2 = Match3Grid.GetSpecialType(grid.Board[x2, y2]);
        bool isSpecialSwap = special1 != MatchSpecialType.None || special2 != MatchSpecialType.None;

        if (!hasMatch && !isSpecialSwap)
        {
            grid.SwapPieces(x1, y1, x2, y2);
            yield return new WaitForSeconds(swapAnimDuration);
            _combo = 0;
            UpdateUI();
            _isResolving = false;
            yield break;
        }

        if (isSpecialSwap)
            TriggerSpecialSwap(x1, y1, x2, y2);

        yield return ResolveBoardCascade(new Vector2Int(x2, y2));
        EnsureBoardHasMoves();
        _isResolving = false;
    }

    private IEnumerator ResolveBoardCascade(Vector2Int? preferredSpecialPos)
    {
        int comboCount = 0;

        while (true)
        {
            var groups = judge.FindMatchGroups();
            if (groups.Count == 0)
            {
                _combo = 0;
                UpdateUI();
                yield break;
            }

            comboCount++;
            _combo = comboCount;

            var clearCells = new HashSet<Vector2Int>();
            foreach (var group in groups)
            {
                foreach (var c in group.Cells) clearCells.Add(c);
            }

            var specialPlan = BuildSpecialCreationPlan(groups, preferredSpecialPos);
            foreach (var pos in specialPlan.Keys)
                clearCells.Remove(pos);

            ExpandClearByTriggeredSpecials(clearCells);
            ProcessObstacleDamage(clearCells);

            int gained = CalculateScore(groups, comboCount);
            _score += gained;
            _workProgress += gained;
            while (_workProgress >= progressRequiredPerRound)
            {
                _workProgress -= progressRequiredPerRound;
                _completedRounds++;
            }
            UpdateUI();

            judge.ClearMatches(new List<Vector2Int>(clearCells));
            yield return new WaitForSeconds(clearAnimDuration);

            judge.ApplyGravity();
            yield return new WaitForSeconds(clearAnimDuration);

            foreach (var kv in specialPlan)
            {
                int current = grid.Board[kv.Key.x, kv.Key.y];
                int baseType = Match3Grid.GetBaseType(current);
                if (baseType < 0) baseType = Random.Range(0, grid.activeTypeCount);
                grid.SetPieceValue(kv.Key.x, kv.Key.y, Match3Grid.EncodePiece(baseType, kv.Value));
            }

            yield return new WaitForSeconds(comboInterval);
            preferredSpecialPos = null;

            if (IsGoalReached())
            {
                EndGame();
                yield break;
            }
        }
    }

    private void ProcessObstacleDamage(HashSet<Vector2Int> clearCells)
    {
        var blockedByChain = new List<Vector2Int>();
        foreach (var cell in clearCells)
        {
            if (grid.ChainLayers[cell.x, cell.y] > 0)
            {
                grid.DamageChain(cell.x, cell.y, 1);
                blockedByChain.Add(cell);
            }
        }

        foreach (var blocked in blockedByChain)
            clearCells.Remove(blocked);

        var affected = new HashSet<Vector2Int>(clearCells);
        foreach (var cell in clearCells)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) != 1) continue;
                    int nx = cell.x + dx;
                    int ny = cell.y + dy;
                    if (nx >= 0 && nx < Match3Grid.WIDTH && ny >= 0 && ny < Match3Grid.HEIGHT)
                        affected.Add(new Vector2Int(nx, ny));
                }
            }
        }

        foreach (var cell in affected)
            grid.DamageIce(cell.x, cell.y, 1);
    }

    private Dictionary<Vector2Int, MatchSpecialType> BuildSpecialCreationPlan(List<MatchGroup> groups, Vector2Int? preferredPos)
    {
        var plan = new Dictionary<Vector2Int, MatchSpecialType>();
        foreach (var g in groups)
        {
            MatchSpecialType special = MatchSpecialType.None;
            int len = g.Cells.Count;

            if (g.Shape == MatchShapeType.LT && len >= 5)
                special = MatchSpecialType.Wrapped;
            else if (len >= 5)
                special = MatchSpecialType.ColorBomb;
            else if (len == 4)
                special = g.Horizontal ? MatchSpecialType.LineHorizontal : MatchSpecialType.LineVertical;

            if (special == MatchSpecialType.None) continue;

            Vector2Int createPos = preferredPos.HasValue && g.Cells.Contains(preferredPos.Value)
                ? preferredPos.Value
                : g.Cells[g.Cells.Count / 2];

            if (!plan.ContainsKey(createPos) || (int)plan[createPos] < (int)special)
                plan[createPos] = special;
        }
        return plan;
    }

    private void ExpandClearByTriggeredSpecials(HashSet<Vector2Int> clearCells)
    {
        var queue = new Queue<Vector2Int>(clearCells);
        while (queue.Count > 0)
        {
            var pos = queue.Dequeue();
            int piece = grid.Board[pos.x, pos.y];
            var special = Match3Grid.GetSpecialType(piece);
            if (special == MatchSpecialType.None) continue;

            var extra = GetSpecialEffectCells(pos, special, Match3Grid.GetBaseType(piece));
            foreach (var ex in extra)
            {
                if (clearCells.Add(ex)) queue.Enqueue(ex);
            }
        }
    }

    private List<Vector2Int> GetSpecialEffectCells(Vector2Int center, MatchSpecialType special, int baseType)
    {
        var result = new List<Vector2Int>();
        switch (special)
        {
            case MatchSpecialType.LineHorizontal:
                for (int x = 0; x < Match3Grid.WIDTH; x++) result.Add(new Vector2Int(x, center.y));
                break;
            case MatchSpecialType.LineVertical:
                for (int y = 0; y < Match3Grid.HEIGHT; y++) result.Add(new Vector2Int(center.x, y));
                break;
            case MatchSpecialType.Wrapped:
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int tx = center.x + dx;
                        int ty = center.y + dy;
                        if (tx >= 0 && tx < Match3Grid.WIDTH && ty >= 0 && ty < Match3Grid.HEIGHT)
                            result.Add(new Vector2Int(tx, ty));
                    }
                }
                break;
            case MatchSpecialType.ColorBomb:
                for (int x = 0; x < Match3Grid.WIDTH; x++)
                {
                    for (int y = 0; y < Match3Grid.HEIGHT; y++)
                    {
                        if (Match3Grid.GetBaseType(grid.Board[x, y]) == baseType)
                            result.Add(new Vector2Int(x, y));
                    }
                }
                break;
        }
        return result;
    }

    private void TriggerSpecialSwap(int x1, int y1, int x2, int y2)
    {
        var clear = new HashSet<Vector2Int>
        {
            new Vector2Int(x1, y1),
            new Vector2Int(x2, y2)
        };

        ExpandClearByTriggeredSpecials(clear);
        judge.ClearMatches(new List<Vector2Int>(clear));
        judge.ApplyGravity();
    }

    private int CalculateScore(List<MatchGroup> groups, int comboCount)
    {
        int total = 0;
        foreach (var g in groups)
        {
            int len = g.Cells.Count;
            int baseScore;
            if (g.Shape == MatchShapeType.LT && len >= 5) baseScore = 500;
            else if (len >= 5) baseScore = 1000;
            else if (len == 4) baseScore = 300;
            else baseScore = 100;

            int comboBonus = comboCount > 1 ? (comboCount - 1) * 50 : 0;
            total += baseScore + comboBonus;
        }

        return Mathf.RoundToInt(total * _cfg.scoreMultiplier);
    }

    private void EnsureBoardHasMoves()
    {
        if (!judge.HasAnyPossibleMove())
        {
            judge.ShuffleBoard();
            StartCoroutine(ResolveBoardCascade(null));
        }
    }

    private void UseShuffleTool()
    {
        if (_remainingShuffles <= 0 || _isResolving) return;
        _remainingShuffles--;
        judge.ShuffleBoard();
        StartCoroutine(ResolveBoardCascade(null));
        UpdateUI();
    }

    private void TryUseHintTool()
    {
        if (Time.time < _nextHintReadyTime || _isResolving) return;
        _nextHintReadyTime = Time.time + HINT_COOLDOWN;
        var hint = judge.FindBestMoveHint();
        if (hint.found)
            StartCoroutine(FlashHint(hint.from, hint.to));
        UpdateUI();
    }

    private IEnumerator FlashHint(Vector2Int a, Vector2Int b)
    {
        var imgA = grid.PieceObjects[a.x, a.y]?.GetComponent<Image>();
        var imgB = grid.PieceObjects[b.x, b.y]?.GetComponent<Image>();
        if (imgA == null || imgB == null) yield break;

        Color c1 = imgA.color;
        Color c2 = imgB.color;
        imgA.color = Color.Lerp(c1, Color.white, 0.5f);
        imgB.color = Color.Lerp(c2, Color.white, 0.5f);
        yield return new WaitForSeconds(0.4f);
        grid.UpdatePiecePosition(a.x, a.y);
        grid.UpdatePiecePosition(b.x, b.y);
    }

    private bool IsGoalReached()
    {
        if (difficulty == Match3Difficulty.Easy)
            return _score >= _targetScore;

        int clearedObstacle = _initialObstacleCount - grid.CountObstacleCells();
        if (difficulty == Match3Difficulty.Medium)
            return clearedObstacle >= _targetObstacleClear;

        return _score >= _targetScore && clearedObstacle >= _targetObstacleClear;
    }

    private void EndGame()
    {
        _isPlaying = false;
        int baseFee = _completedRounds * baseFeePerRound;
        int scoreBonusFee = Mathf.FloorToInt(_score * feeMultiplier);
        int earnedFee = baseFee + scoreBonusFee;
        int clearedObstacle = _initialObstacleCount - grid.CountObstacleCells();
        Debug.Log($"[Match3] 游戏结束! 得分: {_score}, 完成轮次: {_completedRounds}, 获得旅费: {earnedFee}");

        if (GameManager.Instance != null)
            GameManager.Instance.AddCurrency(earnedFee);

        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null)
            resultText.text = $"工作结束!\n得分: {_score}\n障碍清除: {clearedObstacle}/{_targetObstacleClear}\n完成轮次: {_completedRounds}\n基础旅费: {baseFee}\n分数加成: {scoreBonusFee}\n获得旅费: {earnedFee}";

        EventBus.Publish(GameEvent.OnMatch3End);
    }

    private void ReturnToMainUI()
    {
        grid.ClearBoard();
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToMainUI();
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = $"得分: {_score}";
        if (comboText != null)
            comboText.text = _combo > 1 ? $"连锁 x{_combo}" : string.Empty;
        if (difficultyText != null)
            difficultyText.text = $"难度: {GetDifficultyName(difficulty)}";
        if (progressText != null)
            progressText.text = $"进度: {_workProgress}/{progressRequiredPerRound} | 完成轮次: {_completedRounds}";
        if (progressFill != null)
            progressFill.fillAmount = progressRequiredPerRound > 0 ? Mathf.Clamp01((float)_workProgress / progressRequiredPerRound) : 0f;
        if (goalText != null)
        {
            int clearedObstacle = _initialObstacleCount - grid.CountObstacleCells();
            if (difficulty == Match3Difficulty.Easy)
                goalText.text = $"目标: 分数达到 {_targetScore}（当前 {_score}）";
            else if (difficulty == Match3Difficulty.Medium)
                goalText.text = $"目标: 清除障碍 {_targetObstacleClear}（当前 {clearedObstacle}）";
            else
                goalText.text = $"目标: 分数 {_targetScore}+障碍 {_targetObstacleClear}（{_score}/{clearedObstacle}）";
        }
        if (toolText != null)
        {
            float cd = Mathf.Max(0f, _nextHintReadyTime - Time.time);
            toolText.text = $"右键重排: {_remainingShuffles} 次 | 空格提示CD: {Mathf.CeilToInt(cd)}s";
        }
    }

    public void SetDifficulty(Match3Difficulty newDifficulty)
    {
        difficulty = newDifficulty;
        ApplyDifficulty(newDifficulty);
        UpdateUI();
    }

    private void ApplyDifficulty(Match3Difficulty diff)
    {
        switch (diff)
        {
            case Match3Difficulty.Easy:
                _cfg = new DifficultyConfig
                {
                    duration = 90f,
                    pieceTypes = 5,
                    scoreMultiplier = 1f,
                    targetScore = 3500,
                    obstacleCount = 6,
                    chainCount = 3,
                    shuffleCount = 3
                };
                break;
            case Match3Difficulty.Hard:
                _cfg = new DifficultyConfig
                {
                    duration = 45f,
                    pieceTypes = 6,
                    scoreMultiplier = 1.5f,
                    targetScore = 7000,
                    obstacleCount = 14,
                    chainCount = 10,
                    shuffleCount = 1
                };
                break;
            default:
                _cfg = new DifficultyConfig
                {
                    duration = 60f,
                    pieceTypes = 6,
                    scoreMultiplier = 1.2f,
                    targetScore = 5200,
                    obstacleCount = 10,
                    chainCount = 6,
                    shuffleCount = 2
                };
                break;
        }
        gameDuration = _cfg.duration;
    }

    private string GetDifficultyName(Match3Difficulty diff)
    {
        switch (diff)
        {
            case Match3Difficulty.Easy: return "简单";
            case Match3Difficulty.Hard: return "困难";
            default: return "中等";
        }
    }
}
