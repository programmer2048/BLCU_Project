using UnityEngine;
using System.Collections.Generic;

public enum MatchShapeType
{
    Line,
    LT
}

public class MatchGroup
{
    public List<Vector2Int> Cells = new List<Vector2Int>();
    public MatchShapeType Shape = MatchShapeType.Line;
    public bool Horizontal;
}

/// <summary>
/// 三消判定 —— 消除检测/重力下落/连锁判定
/// </summary>
public class Match3Judge : MonoBehaviour
{
    private Match3Grid _grid;

    public struct MoveHint
    {
        public bool found;
        public Vector2Int from;
        public Vector2Int to;
        public int score;
    }

    public void Init(Match3Grid grid)
    {
        _grid = grid;
    }

    public List<MatchGroup> FindMatchGroups()
    {
        var groups = new List<MatchGroup>();
        if (_grid == null) return groups;

        for (int y = 0; y < Match3Grid.HEIGHT; y++)
        {
            int x = 0;
            while (x < Match3Grid.WIDTH)
            {
                int piece = _grid.Board[x, y];
                int baseType = Match3Grid.GetBaseType(piece);
                if (baseType < 0)
                {
                    x++;
                    continue;
                }

                int runStart = x;
                int runLen = 1;
                while (x + runLen < Match3Grid.WIDTH && Match3Grid.GetBaseType(_grid.Board[x + runLen, y]) == baseType)
                    runLen++;

                if (runLen >= 3)
                {
                    var g = new MatchGroup { Horizontal = true };
                    for (int i = 0; i < runLen; i++) g.Cells.Add(new Vector2Int(runStart + i, y));
                    groups.Add(g);
                }
                x += runLen;
            }
        }

        for (int x = 0; x < Match3Grid.WIDTH; x++)
        {
            int y = 0;
            while (y < Match3Grid.HEIGHT)
            {
                int piece = _grid.Board[x, y];
                int baseType = Match3Grid.GetBaseType(piece);
                if (baseType < 0)
                {
                    y++;
                    continue;
                }

                int runStart = y;
                int runLen = 1;
                while (y + runLen < Match3Grid.HEIGHT && Match3Grid.GetBaseType(_grid.Board[x, y + runLen]) == baseType)
                    runLen++;

                if (runLen >= 3)
                {
                    var g = new MatchGroup { Horizontal = false };
                    for (int i = 0; i < runLen; i++) g.Cells.Add(new Vector2Int(x, runStart + i));
                    groups.Add(g);
                }
                y += runLen;
            }
        }

        var uniqueGroups = new List<MatchGroup>(groups);
        for (int i = 0; i < uniqueGroups.Count; i++)
        {
            for (int j = i + 1; j < uniqueGroups.Count; j++)
            {
                if (ShareAnyCell(uniqueGroups[i], uniqueGroups[j]))
                {
                    uniqueGroups[i].Shape = MatchShapeType.LT;
                    uniqueGroups[j].Shape = MatchShapeType.LT;
                }
            }
        }

        return uniqueGroups;
    }

    public List<Vector2Int> FindMatches()
    {
        var groups = FindMatchGroups();
        var set = new HashSet<Vector2Int>();
        foreach (var g in groups)
        {
            foreach (var c in g.Cells) set.Add(c);
        }
        return new List<Vector2Int>(set);
    }

    private bool ShareAnyCell(MatchGroup a, MatchGroup b)
    {
        var cellSet = new HashSet<Vector2Int>(a.Cells);
        foreach (var c in b.Cells)
        {
            if (cellSet.Contains(c)) return true;
        }
        return false;
    }

    /// <summary>消除匹配的棋子</summary>
    public int ClearMatches(List<Vector2Int> matches)
    {
        foreach (var pos in matches)
        {
            _grid.RemovePiece(pos.x, pos.y);
        }
        return matches.Count;
    }

    /// <summary>重力下落</summary>
    public void ApplyGravity()
    {
        for (int x = 0; x < Match3Grid.WIDTH; x++)
        {
            int emptyY = 0;
            for (int y = 0; y < Match3Grid.HEIGHT; y++)
            {
                if (_grid.Board[x, y] >= 0)
                {
                    if (emptyY != y)
                    {
                        _grid.Board[x, emptyY] = _grid.Board[x, y];
                        _grid.Board[x, y] = -1;

                        // 移动视觉对象
                        _grid.PieceObjects[x, emptyY] = _grid.PieceObjects[x, y];
                        _grid.PieceObjects[x, y] = null;
                        _grid.UpdatePiecePosition(x, emptyY);
                    }
                    emptyY++;
                }
            }

            // 从顶部填充新棋子
            for (int y = emptyY; y < Match3Grid.HEIGHT; y++)
            {
                _grid.FillPiece(x, y);
            }
        }
    }

    /// <summary>判断两个格子是否相邻</summary>
    public bool IsAdjacent(int x1, int y1, int x2, int y2)
    {
        return (Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2)) == 1;
    }

    public bool HasAnyPossibleMove()
    {
        if (_grid == null) return false;

        for (int x = 0; x < Match3Grid.WIDTH; x++)
        {
            for (int y = 0; y < Match3Grid.HEIGHT; y++)
            {
                if (x + 1 < Match3Grid.WIDTH && WouldCreateMatchBySwap(x, y, x + 1, y)) return true;
                if (y + 1 < Match3Grid.HEIGHT && WouldCreateMatchBySwap(x, y, x, y + 1)) return true;
            }
        }
        return false;
    }

    private bool WouldCreateMatchBySwap(int x1, int y1, int x2, int y2)
    {
        int a = _grid.Board[x1, y1];
        int b = _grid.Board[x2, y2];
        _grid.Board[x1, y1] = b;
        _grid.Board[x2, y2] = a;

        bool matched = FindMatches().Count > 0;

        _grid.Board[x1, y1] = a;
        _grid.Board[x2, y2] = b;
        return matched;
    }

    public void ShuffleBoard()
    {
        if (_grid == null) return;

        var pool = new List<int>();
        for (int x = 0; x < Match3Grid.WIDTH; x++)
        {
            for (int y = 0; y < Match3Grid.HEIGHT; y++)
            {
                int piece = _grid.Board[x, y];
                if (piece >= 0) pool.Add(Match3Grid.GetBaseType(piece));
            }
        }

        if (pool.Count == 0) return;

        int maxRetry = 30;
        for (int attempt = 0; attempt < maxRetry; attempt++)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                int r = Random.Range(i, pool.Count);
                (pool[i], pool[r]) = (pool[r], pool[i]);
            }

            int idx = 0;
            for (int x = 0; x < Match3Grid.WIDTH; x++)
            {
                for (int y = 0; y < Match3Grid.HEIGHT; y++)
                {
                    int value = Match3Grid.EncodePiece(pool[idx++], MatchSpecialType.None);
                    _grid.SetPieceValue(x, y, value);
                }
            }

            if (FindMatches().Count == 0 && HasAnyPossibleMove()) return;
        }
    }

    public MoveHint FindBestMoveHint()
    {
        var best = new MoveHint { found = false, score = -1 };
        if (_grid == null) return best;

        for (int x = 0; x < Match3Grid.WIDTH; x++)
        {
            for (int y = 0; y < Match3Grid.HEIGHT; y++)
            {
                if (_grid.IsLocked(x, y)) continue;

                EvaluateHintSwap(x, y, x + 1, y, ref best);
                EvaluateHintSwap(x, y, x, y + 1, ref best);
            }
        }

        return best;
    }

    private void EvaluateHintSwap(int x1, int y1, int x2, int y2, ref MoveHint best)
    {
        if (x2 < 0 || x2 >= Match3Grid.WIDTH || y2 < 0 || y2 >= Match3Grid.HEIGHT) return;
        if (_grid.IsLocked(x2, y2)) return;

        int a = _grid.Board[x1, y1];
        int b = _grid.Board[x2, y2];

        _grid.Board[x1, y1] = b;
        _grid.Board[x2, y2] = a;

        int groupScore = 0;
        var groups = FindMatchGroups();
        foreach (var g in groups) groupScore += g.Cells.Count;

        _grid.Board[x1, y1] = a;
        _grid.Board[x2, y2] = b;

        if (groupScore > best.score)
        {
            best = new MoveHint
            {
                found = groupScore > 0,
                from = new Vector2Int(x1, y1),
                to = new Vector2Int(x2, y2),
                score = groupScore
            };
        }
    }
}
