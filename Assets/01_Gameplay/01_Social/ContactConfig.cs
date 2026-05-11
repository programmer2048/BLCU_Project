using UnityEngine;

[CreateAssetMenu(fileName = "NewContact", menuName = "Social System/Contact Profile")]
public class ContactConfig : ScriptableObject
{
    [Header("基础信息")]
    public string contactId;   // 唯一ID，例如 "LinXiao", "Mom", "Bank"
    public string displayName; // 显示名字
    public Sprite avatar;      // 头像

    [TextArea]
    public string signature;   // 个性签名 (可选)

    [Header("默认状态")]
    public bool isUnlockedByDefault = false; // 是否一开始就有
}