using UnityEngine;

/// <summary>
/// 流程事件系统 —— 监听 GameState 变化，触发对应场景逻辑
/// 每个场景可挂载此组件来响应状态变化
/// </summary>
public class FlowEventSystem : MonoBehaviour
{
    void OnEnable()
    {
        EventBus.Subscribe<GameState>(GameEvent.OnGameStateChanged, OnStateChanged);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<GameState>(GameEvent.OnGameStateChanged, OnStateChanged);
    }

    private void OnStateChanged(GameState newState)
    {
        Debug.Log($"[FlowEvent] 场景收到状态变化: {newState}");
    }
}
