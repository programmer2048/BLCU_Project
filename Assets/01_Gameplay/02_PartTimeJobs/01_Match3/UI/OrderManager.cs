using UnityEngine;
using System.Collections.Generic;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    [Header("References")]
    public GameObject trayPrefab;      // 你的托盘Prefab
    public Transform trayContainer;    // Canvas下用来放托盘的Panel

    [Header("Spawn Settings")]
    public float spawnInterval = 3f;
    private float _timer;

    // 管理所有活动的订单，方便查找
    private List<TrayController> activeOrders = new List<TrayController>();

    public M3_Board gameBoard;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (M3_GameManager.Instance.state != M3_GameState.Playing) return;

        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            _timer = 0;
            spawnInterval = UnityEngine.Random.Range(1f,5f);
            GenerateNewOrder();
        }
    }
    public void RemoveActiveOrder(TrayController tray)
    {
        if (activeOrders.Contains(tray))
        {
            activeOrders.Remove(tray);
        }
    }
    public void GenerateNewOrder()
    {
        if (gameBoard == null || gameBoard.itemSprites == null || gameBoard.itemSprites.Count == 0)
        {
            Debug.LogError("OrderManager: 缺少 GameBoard 引用或 Sprite 列表为空！");
            return;
        }
        GameObject go = Instantiate(trayPrefab, trayContainer);
        TrayController tray = go.GetComponent<TrayController>();
        // 1. 随机一个类型
        int typeIndex = Random.Range(0, gameBoard.itemSprites.Count);
        M3_ItemType randomType = (M3_ItemType)typeIndex;
        // 2. 获取对应的图片 (必须用同一个 Index)
        Sprite correctSprite = gameBoard.itemSprites[typeIndex];
        // 3. 随机数量
        int count = Random.Range(3, 6);
        // 4. 初始化 Tray
        tray.Init(randomType, correctSprite, count);

        activeOrders.Add(tray);
    }

    public TrayController GetTargetTray(M3_ItemType type)
    {
        foreach (var tray in activeOrders)
        {
            // 只有未完成且类型匹配的才返回
            if (!tray.IsCompleted && tray.RequiredType == type)
            {
                return tray;
            }
        }
        return null;
    }

    // 订单超时/失败时调用
    public void OnOrderFailed(TrayController tray)
    {
        M3_GameManager.Instance.ModifyHealth(-10); // 扣血
        if (activeOrders.Contains(tray))
        {
            activeOrders.Remove(tray);
        }
    }
}