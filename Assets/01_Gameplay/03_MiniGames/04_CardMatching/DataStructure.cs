using UnityEngine;

[CreateAssetMenu(fileName = "NewMaidData", menuName = "Game/MaidData")]
public class MaidData : ScriptableObject
{
    public int id;
    [TextArea] public string note;

    public Sprite sprite;          // 原图
    public Sprite iconSprite;      // 图标（如：梳子）
    public Sprite lineArtSprite;   // 线稿
    public Sprite blueprintSprite; // 蓝图
    public Sprite emotionalSprite; // 动态图
}

public enum ItemType { Fragment, FullItem, RustRemover }