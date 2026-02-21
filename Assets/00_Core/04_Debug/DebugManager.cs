using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
#endif

public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance { get; private set; }

    [SerializeField] private bool suppressAllocatorLeakWarnings = true;
    [SerializeField] private bool suppressGameplayLogs = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SuppressLeakWarningsEarly()
    {
        NativeLeakDetection.Mode = NativeLeakDetectionMode.Disabled;
        UnsafeUtility.SetLeakDetectionMode(NativeLeakDetectionMode.Disabled);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Debug.unityLogger.filterLogType = LogType.Error;
    }
#endif

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (suppressAllocatorLeakWarnings)
            {
                NativeLeakDetection.Mode = NativeLeakDetectionMode.Disabled;
                UnsafeUtility.SetLeakDetectionMode(NativeLeakDetectionMode.Disabled);
                Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            }

            if (suppressGameplayLogs)
                Debug.unityLogger.filterLogType = LogType.Error;
#endif
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
