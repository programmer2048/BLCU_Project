/// <summary>
/// 游戏事件类型枚举
/// </summary>
public enum GameEvent
{
    // 流程事件
    OnGameStateChanged,
    OnSceneLoaded,

    // 货币事件
    OnCurrencyChanged,
    OnCurrencyEarned,
    OnCurrencySpent,

    // 解锁事件
    OnSpotUnlocked,
    OnSpotExplored,
    OnAllSpotsUnlocked,

    // 社交/剧情事件
    OnDialogueStart,
    OnDialogueEnd,
    OnDialogueChoice,
    OnEmotionChanged,

    // 打工事件
    OnMatch3Start,
    OnMatch3End,
    OnRhythmStart,
    OnRhythmEnd,

    // 小游戏事件
    OnMiniGameStart,
    OnMiniGameEnd,
    OnMiniGameWin,

    // 图鉴事件
    OnAlbumItemUnlocked,

    // 结局事件
    OnEndingTriggered
}
