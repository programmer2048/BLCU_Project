using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ContactHistoryData
{
    public string contactId;
    public List<ChatMessage> chatLog = new List<ChatMessage>();
    public bool hasUnread;
    public List<ChatReplyOption> pendingOptions = new List<ChatReplyOption>();
}

[System.Serializable]
public class GameData
{
    public string saveId;
    public string lastSaveTime;

    public int money = 100;
    public int affinity = 0;
    public int currentChapter = 1;
    public int chapterSubState = 0;

    public List<string> unlockedContactIds = new List<string>();
    public List<ContactHistoryData> contactHistories = new List<ContactHistoryData>();
    public List<string> unlockedMomentIds = new List<string>();

    public GameData(string id)
    {
        this.saveId = id;
        this.lastSaveTime = System.DateTime.Now.ToString();
        InitPresetData();
    }

    public GameData() { }
    private void InitPresetData()
    {
        // 解锁联系人
        unlockedContactIds.Add("Work_Group");
        unlockedContactIds.Add("Mom");
        unlockedContactIds.Add("Dad");
        unlockedContactIds.Add("Boss");
        unlockedContactIds.Add("System_Bank");

        // 注入预设聊天记录
        // 群聊
        AddPresetChat("Work_Group", SenderType.NPC, "陈哥去山西了？听说那边古建挺多，拍点照片回来啊", "小王");
        AddPresetChat("Work_Group", SenderType.NPC, "年轻人出去走走挺好，回来继续画图[doge]", "周姐");
        AddPresetChat("Work_Group", SenderType.NPC, "回来还是一样改图，甲方永远不满意（裂开）", "大刘");

        // 妈妈
        AddPresetChat("Mom", SenderType.NPC, "儿子，到山西了？那边冷不冷啊？");
        AddPresetChat("Mom", SenderType.NPC, "你爷爷当年去山西，回来给我带了一包红枣，特别甜。你看到有卖的也买点尝尝");
        AddPresetChat("Mom", SenderType.NPC, "不过别买太多，拿不动");

        // 爸爸
        AddPresetChat("Dad", SenderType.NPC, "转发了一篇文章：《山西应县木塔：千年不倒的秘密》");
        AddPresetChat("Dad", SenderType.NPC, "你到那儿了，帮我拍张塔的全景，要正面的");
        AddPresetChat("Dad", SenderType.NPC, "你爷爷有张老照片，就是在塔前面拍的，黑白的那种。后来搬家找不着了");

        // 老板
        AddPresetChat("Boss", SenderType.NPC, "小陈，你这次停薪留职的申请，我跟上面磨了半天才批下来，下不为例。");
        AddPresetChat("Boss", SenderType.NPC, "对了，美术馆的项目甲方还在催，说方案还得改。我跟他们说你在外出差，回来再说。");
        AddPresetChat("Boss", SenderType.Player, "……麻烦您了。");
        AddPresetChat("Boss", SenderType.NPC, "没事。回来好好干活就行。");
    }

    // 辅助方法：添加单条记录
    private void AddPresetChat(string contactId, SenderType type, string content, string senderName = "")
    {
        var history = GetOrCreateInfo(contactId);
        string finalContent = string.IsNullOrEmpty(senderName) ? content : $"{senderName}：{content}";

        history.chatLog.Add(new ChatMessage
        {
            sender = type,
            type = MessageType.Text,
            content = finalContent,
            timeStamp = "昨天" // 也可以写具体时间
        });
        history.hasUnread = true; // 初始状态设为未读
    }

    public ContactHistoryData GetOrCreateInfo(string id)
    {
        var data = contactHistories.Find(x => x.contactId == id);
        if (data == null)
        {
            data = new ContactHistoryData { contactId = id, hasUnread = false };
            contactHistories.Add(data);
        }
        return data;
    }
}
