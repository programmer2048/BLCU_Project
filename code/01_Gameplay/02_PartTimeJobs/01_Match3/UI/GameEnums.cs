// GameEnums.cs
public enum M3_ItemType
{
    T0 = 0,
    T1 = 1,
    T2 = 2,
    T3 = 3,
    T4 = 4,
    T5 = 5,
    Obstacle = 100, // ÕÏ°­Îï (±ÈÈçÄ¾Ïä)
    Bomb = 200      // Õ¨µ¯
}

public enum M3_GameState
{
    Playing,
    Paused,
    GameOver
}

public enum BoardState
{
    Idle,
    Locked
}