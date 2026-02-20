using UnityEngine;

public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance { get; private set; }
    void Awake() { if (Instance==null){Instance=this; DontDestroyOnLoad(gameObject);} else Destroy(gameObject); }
}
