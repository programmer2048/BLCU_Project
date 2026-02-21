/// <summary>
/// 游戏全局状态枚举 —— 由 GameFlowManager 驱动状态流转
/// </summary>
public enum GameState
{
    None,
    Boot,           // 启动初始化
    Prologue,       // 序章
    MainUI,         // 主界面（地图）
    Social,         // 社交聊天
    PartTimeJob,    // 打工选择
    Match3,         // 三消小游戏
    Rhythm,         // 音游小游戏
    TravelMap,      // 旅行地图
    Story,          // 剧情播放
    MiniGame,       // 章节小游戏
    Album,          // 相簿
    Ending          // 结局
}
