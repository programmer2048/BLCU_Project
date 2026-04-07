using UnityEngine;
using System.Collections.Generic;
using System.Linq; // 引入 Linq 用于处理列表

public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance;

    public GameObject itemPrefab;
    public RectTransform spawnArea;

    [Header("Level Config")]
    public float rustChance = 0.3f;
    public float fragmentProbability = 0.2f;

    [Header("Sprites")]
    public Sprite rustRemoverFragmentSprite;

    // 定义一个简单的内部类来存储待生成物品的信息
    private class SpawnData
    {
        public int maidId;
        public Sprite icon;
        public ItemType type;
        public bool isRusted;
    }

    private void Awake() => Instance = this;

    public void GenerateLevel()
    {
        foreach (Transform child in spawnArea) Destroy(child.gameObject);

        List<SpawnData> allItemsToSpawn = new List<SpawnData>();
        int totalRustedCount = 0;

        foreach (var maid in MaidGameManager.Instance.allMaids)
        {
            // 正常物品
            for (int i = 0; i < 8; i++)
            {
                bool isRusted = Random.value < rustChance;
                allItemsToSpawn.Add(new SpawnData
                {
                    maidId = maid.id,
                    icon = maid.iconSprite,
                    type = ItemType.FullItem,
                    isRusted = isRusted
                });
                if (isRusted) totalRustedCount++;
            }
            // 碎片
            for (int i = 0; i < 4; i++)
            {
                allItemsToSpawn.Add(new SpawnData
                {
                    maidId = maid.id,
                    icon = maid.iconSprite,
                    type = ItemType.Fragment,
                    isRusted = false
                });
            }
        }
        // 除锈剂碎片
        int toolFrags = totalRustedCount * 3;
        for (int i = 0; i < toolFrags; i++)
        {
            allItemsToSpawn.Add(new SpawnData
            {
                maidId = -1,
                icon = rustRemoverFragmentSprite,
                type = ItemType.RustRemover,
                isRusted = false
            });
        }
        Shuffle(allItemsToSpawn);
        foreach (var data in allItemsToSpawn)
        {
            CreateItem(data.maidId, data.icon, data.type, data.isRusted);
        }
    }

    // 洗牌
    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    private void CreateItem(int maidId, Sprite icon, ItemType type, bool isRusted)
    {
        GameObject go = Instantiate(itemPrefab, spawnArea);
        ItemObject item = go.GetComponent<ItemObject>();

        item.maidId = maidId;
        item.type = type;
        item.isRusted = isRusted;

        // 设置图标逻辑
        if (type == ItemType.Fragment)
        {
            var maidData = MaidGameManager.Instance.allMaids.Find(m => m.id == maidId);
            item.iconImage.sprite = (maidData != null) ? maidData.iconSprite : icon;
        }
        else if (type == ItemType.RustRemover)
        {
            item.iconImage.sprite = rustRemoverFragmentSprite;
        }
        else
        {
            item.iconImage.sprite = icon;
        }

        item.UpdateVisual(false);
        RandomizeTransform(go.transform);
    }

    private void RandomizeTransform(Transform t)
    {
        float x = Random.Range(-spawnArea.rect.width / 2 + 60, spawnArea.rect.width / 2 - 60);
        float y = Random.Range(-spawnArea.rect.height / 2 + 60, spawnArea.rect.height / 2 - 60);
        t.localPosition = new Vector3(x, y, 0);
        t.localRotation = Quaternion.Euler(0, 0, Random.Range(-30f, 30f));
    }
}