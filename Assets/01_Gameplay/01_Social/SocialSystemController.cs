using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SocialSystemController : MonoBehaviour
{
    // --- 定义一个特殊的ID用于标识朋友圈 ---
    private const string ID_MOMENTS = "System_Moments";

    [Header("--- 配置库 ---")]
    public List<ContactConfig> allContacts;
    public MomentsRepository momentsRepo;

    [Header("--- 特殊配置: 朋友圈入口信息 ---")]
    public string momentsDisplayName = "朋友圈";
    public Sprite momentsAvatar; // 朋友圈图标
    public Sprite selfAvatar; // 玩家自己的头像

    [Header("--- UI: 侧边栏 ---")]
    public Transform contactListContainer;
    public GameObject contactItemPrefab;

    [Header("--- UI: 侧边栏样式 ---")]
    public Color contactNormalColor = new Color(1f, 1f, 1f, 0f);
    public Color contactSelectedColor = new Color(0.9f, 0.9f, 0.9f, 1f);

    [Header("--- UI: 聊天窗口 ---")]
    public GameObject chatPanel;
    public Transform chatContentContainer;
    public ScrollRect chatScrollRect;
    public GameObject chatBubbleLeftPrefab;
    public GameObject chatBubbleRightPrefab;
    public GameObject chatBubbleSystemPrefab;
    public TextMeshProUGUI currentChatNameText;

    [Header("--- UI: 选项面板 ---")]
    public GameObject optionPanel;
    public Transform optionContainer;
    public GameObject optionButtonPrefab;

    [Header("--- UI: 朋友圈窗口 ---")]
    public GameObject momentsPanel;
    public Transform momentsContainer;
    public GameObject momentItemPrefab;
    [Tooltip("用于显示单条评论的纯文本Prefab")]
    public GameObject commentTextPrefab;

    [Header("--- 设置 ---")]
    public int maxPreviewLength = 12;

    private string currentActiveContactId = "";
    private GameData gameData;

    public AudioClip bgmClip;

    void Awake()
    {
        Debug.unityLogger.logEnabled = true;
        Debug.unityLogger.filterLogType = LogType.Log;
    }

    void Start()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.CurrentGameData != null)
            gameData = SaveManager.Instance.CurrentGameData;
        else
            gameData = new GameData("test");
        currentActiveContactId = "";

        RefreshContactList();

        if (chatPanel) chatPanel.SetActive(false);
        if (momentsPanel) momentsPanel.SetActive(false);
        if (optionPanel) optionPanel.SetActive(false);

        BGMManager.Instance.bgm.clip = bgmClip;
        BGMManager.Instance.bgm.Play();
    }

    public void RefreshContactList()
    {
        foreach (Transform child in contactListContainer) Destroy(child.gameObject);

        // 手动生成“朋友圈”入口
        GameObject momentItem = Instantiate(contactItemPrefab, contactListContainer);

        // 设置高亮
        var momentBg = momentItem.GetComponent<Image>();
        if (momentBg)
        {
            momentBg.color = (currentActiveContactId == ID_MOMENTS) ? contactSelectedColor : contactNormalColor;
        }

        // 设置图标和名字
        var momentName = momentItem.transform.Find("NameText");
        if (momentName) momentName.GetComponent<TextMeshProUGUI>().text = momentsDisplayName;

        var momentAva = momentItem.transform.Find("Avatar");
        if (momentAva) momentAva.GetComponent<Image>().sprite = momentsAvatar;

        // 设置点击事件
        momentItem.GetComponent<Button>().onClick.AddListener(OpenMoments);

        var momentPreview = momentItem.transform.Find("PreviewText");
        if (momentPreview) momentPreview.GetComponent<TextMeshProUGUI>().text = "";
        var momentRedDot = momentItem.transform.Find("RedDot");
        if (momentRedDot) momentRedDot.gameObject.SetActive(false); // 暂时隐藏


        // 生成普通联系人
        foreach (var contactId in gameData.unlockedContactIds)
        {
            ContactConfig config = allContacts.Find(c => c.contactId == contactId);
            if (config == null) continue;

            var history = gameData.GetOrCreateInfo(contactId);
            GameObject item = Instantiate(contactItemPrefab, contactListContainer);

            // 高亮逻辑
            var bgImage = item.GetComponent<Image>();
            if (bgImage)
            {
                bgImage.color = (contactId == currentActiveContactId) ? contactSelectedColor : contactNormalColor;
            }

            // 基础信息
            var nameTxt = item.transform.Find("NameText");
            if (nameTxt) nameTxt.GetComponent<TextMeshProUGUI>().text = config.displayName;

            var avatarImg = item.transform.Find("Avatar");
            if (avatarImg) avatarImg.GetComponent<Image>().sprite = config.avatar;

            // 预览文字
            string preview = "";
            if (history.chatLog.Count > 0)
            {
                var lastMsg = history.chatLog[history.chatLog.Count - 1];
                string rawContent = lastMsg.type == MessageType.Image ? "[图片]" : lastMsg.content;
                preview = rawContent.Length > maxPreviewLength ? rawContent.Substring(0, maxPreviewLength) + "..." : rawContent;
            }
            var previewTxt = item.transform.Find("PreviewText");
            if (previewTxt) previewTxt.GetComponent<TextMeshProUGUI>().text = preview;

            // 红点
            var redDot = item.transform.Find("RedDot");
            if (redDot) redDot.gameObject.SetActive(history.hasUnread);

            // 点击事件
            item.GetComponent<Button>().onClick.AddListener(() => OpenChat(contactId));
        }
    }

    public void OpenChat(string contactId)
    {
        currentActiveContactId = contactId;

        ContactConfig config = allContacts.Find(c => c.contactId == contactId);
        var history = gameData.GetOrCreateInfo(contactId);

        if (momentsPanel) momentsPanel.SetActive(false);
        if (chatPanel) chatPanel.SetActive(true);

        if (currentChatNameText) currentChatNameText.text = config.displayName;

        history.hasUnread = false;
        SaveManager.Instance.SaveCurrentGame();

        RefreshContactList(); // 刷新高亮

        RenderChatHistory(history.chatLog, config.avatar);
        CheckAndShowOptions(history);
    }

    private void RenderChatHistory(List<ChatMessage> logs, Sprite npcAvatar)
    {
        foreach (Transform child in chatContentContainer) Destroy(child.gameObject);

        foreach (var msg in logs)
        {
            GameObject bubble = null;
            if (msg.sender == SenderType.NPC)
            {
                if (chatBubbleLeftPrefab)
                {
                    bubble = Instantiate(chatBubbleLeftPrefab, chatContentContainer);
                    var ava = bubble.transform.Find("Avatar");
                    if (ava) ava.GetComponent<Image>().sprite = npcAvatar;
                }
            }
            else if (msg.sender == SenderType.Player)
            {
                if (chatBubbleRightPrefab) bubble = Instantiate(chatBubbleRightPrefab, chatContentContainer);
                var ava = bubble.transform.Find("Avatar");
                if(ava) ava.GetComponent<Image>().sprite = selfAvatar;
            }
            else
            {
                if (chatBubbleSystemPrefab) bubble = Instantiate(chatBubbleSystemPrefab, chatContentContainer);
            }

            if (bubble == null) continue;
            bubble.transform.localScale = Vector3.one;

            var contentTMP = bubble.transform.Find("ContentText")?.GetComponent<TextMeshProUGUI>();
            if (contentTMP == null) contentTMP = bubble.GetComponentInChildren<TextMeshProUGUI>();
            if (contentTMP != null) contentTMP.text = msg.content;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContentContainer.GetComponent<RectTransform>());
        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        if (chatScrollRect) chatScrollRect.verticalNormalizedPosition = 0f;
    }

    public void OpenMoments()
    {
        currentActiveContactId = ID_MOMENTS; // 标记当前为朋友圈

        if (chatPanel) chatPanel.SetActive(false);
        if (optionPanel) optionPanel.SetActive(false);
        if (momentsPanel) momentsPanel.SetActive(true);

        RefreshContactList(); // 刷新侧边栏高亮

        RenderMoments();
    }

    private void RenderMoments()
    {
        if (momentsRepo == null) return;
        // 清理旧数据
        foreach (Transform child in momentsContainer) Destroy(child.gameObject);
        float containerWidth = momentsContainer.GetComponent<RectTransform>().rect.width;
        if (containerWidth == 0) containerWidth = Screen.width;
        for (int i = gameData.unlockedMomentIds.Count - 1; i >= 0; i--)
        {
            string mId = gameData.unlockedMomentIds[i];
            SocialMomentData data = momentsRepo.GetMomentById(mId);
            if (data == null) continue;
            ContactConfig senderConfig = allContacts.Find(c => c.contactId == data.contactId);
            Sprite avatarSprite = senderConfig?.avatar;
            string senderName = senderConfig != null ? senderConfig.displayName : "未知用户";
            GameObject item = Instantiate(momentItemPrefab, momentsContainer);
            item.transform.localScale = Vector3.one;

            // 1. 头像
            var avaTrans = FindDeepChild(item.transform, "Avatar");
            if (avaTrans) avaTrans.GetComponent<Image>().sprite = avatarSprite;
            // 2. 名字
            var nameTrans = FindDeepChild(item.transform, "NameText");
            if (nameTrans) nameTrans.GetComponent<TextMeshProUGUI>().text = senderName;
            // 3. 正文
            var contentTrans = FindDeepChild(item.transform, "ContentText");
            if (contentTrans) contentTrans.GetComponent<TextMeshProUGUI>().text = data.content;
            // 4. 点赞数
            var likeTrans = FindDeepChild(item.transform, "LikesText");
            if (likeTrans) likeTrans.GetComponent<TextMeshProUGUI>().text = $"❤️ {data.likeCount}";
            // 图片处理逻辑
            var imgTrans = FindDeepChild(item.transform, "ContentImage");
            if (imgTrans)
            {
                if (data.image != null)
                {
                    imgTrans.gameObject.SetActive(true);
                    Image imgComp = imgTrans.GetComponent<Image>();
                    imgComp.sprite = data.image;
                    // 计算目标尺寸
                    float targetWidth = containerWidth * 0.3f; // 宽度为容器的一半
                    float spriteRatio = data.image.rect.width / data.image.rect.height;
                    float targetHeight = targetWidth; // 根据宽度反推高度
                    LayoutElement le = imgTrans.GetComponent<LayoutElement>();
                    if (le == null) le = imgTrans.gameObject.AddComponent<LayoutElement>();
                    le.preferredWidth = targetWidth;
                    le.minWidth = targetWidth;   // 强制最小宽度，防止被压缩
                    le.preferredHeight = targetHeight;
                    le.minHeight = targetHeight; // 强制最小高度
                    var arf = imgTrans.GetComponent<AspectRatioFitter>();
                    if (arf) arf.enabled = false;
                }
                else
                {
                    imgTrans.gameObject.SetActive(false);
                }
            }
            // 评论区逻辑
            var commentContainer = FindDeepChild(item.transform, "CommentContainer");
            if (commentContainer != null)
            {
                if (data.comments == null || data.comments.Count == 0)
                {
                    commentContainer.gameObject.SetActive(false);
                }
                else
                {
                    commentContainer.gameObject.SetActive(true);
                    foreach (Transform oldComment in commentContainer) Destroy(oldComment.gameObject);
                    foreach (string commentStr in data.comments)
                    {
                        if (commentTextPrefab != null)
                        {
                            GameObject cObj = Instantiate(commentTextPrefab, commentContainer);
                            cObj.transform.localScale = Vector3.one;
                            cObj.GetComponent<TextMeshProUGUI>().text = commentStr;
                        }
                    }
                }
            }
        }
        // 强制刷新 UI 布局，防止图片高度变化导致重叠
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(momentsContainer.GetComponent<RectTransform>());
    }
    private Transform FindDeepChild(Transform parent, string childName)
    {
        // 1. 先看直接子节点
        Transform result = parent.Find(childName);
        if (result != null) return result;
        // 2. 如果没找到，遍历所有子节点进行递归
        foreach (Transform child in parent)
        {
            result = FindDeepChild(child, childName);
            if (result != null) return result;
        }
        return null;
    }

    private void CheckAndShowOptions(ContactHistoryData history)
    {
        foreach (Transform child in optionContainer) Destroy(child.gameObject);
        if (history.pendingOptions == null || history.pendingOptions.Count == 0) { optionPanel.SetActive(false); return; }
        optionPanel.SetActive(true);
        foreach (var opt in history.pendingOptions)
        {
            GameObject btnObj = Instantiate(optionButtonPrefab, optionContainer);
            btnObj.transform.localScale = Vector3.one;
            var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText) btnText.text = opt.optionText;
            btnObj.GetComponent<Button>().onClick.AddListener(() => OnOptionSelected(opt));
        }
    }

    private void OnOptionSelected(ChatReplyOption option)
    {
        var history = gameData.GetOrCreateInfo(currentActiveContactId);
        history.chatLog.Add(new ChatMessage { sender = SenderType.Player, type = MessageType.Text, content = option.optionText, timeStamp = "Now" });
        if (option.affectionBonus != 0) gameData.affinity += option.affectionBonus;
        if (!string.IsNullOrEmpty(option.npcResponse)) history.chatLog.Add(new ChatMessage { sender = SenderType.NPC, type = MessageType.Text, content = option.npcResponse, timeStamp = "Now" });
        history.pendingOptions.Clear();
        SaveManager.Instance.SaveCurrentGame();
        ContactConfig config = allContacts.Find(c => c.contactId == currentActiveContactId);
        RenderChatHistory(history.chatLog, config.avatar);
        optionPanel.SetActive(false);
    }

    public void ProcessSocialUpdate(SocialUpdateBatch batch)
    {
        if (batch == null) return;
        if (batch.unlockMomentIds != null)
        {
            foreach (var mId in batch.unlockMomentIds) if (!gameData.unlockedMomentIds.Contains(mId)) gameData.unlockedMomentIds.Add(mId);
        }
        foreach (var conv in batch.conversations)
        {
            if (!gameData.unlockedContactIds.Contains(conv.contactId)) gameData.unlockedContactIds.Add(conv.contactId);
            var history = gameData.GetOrCreateInfo(conv.contactId);
            foreach (var msgText in conv.messages) history.chatLog.Add(new ChatMessage { sender = SenderType.NPC, type = MessageType.Text, content = msgText, timeStamp = "Now" });
            history.hasUnread = true;
            if (conv.replyOptions != null && conv.replyOptions.Count > 0) history.pendingOptions = new List<ChatReplyOption>(conv.replyOptions);
        }
        SaveManager.Instance.SaveCurrentGame();
        RefreshContactList();
    }
    public void ReceiveIncome(int amount, string sourceName)
    {
        gameData.money += amount;
        var bankHistory = gameData.GetOrCreateInfo("System_Bank");
        bankHistory.chatLog.Add(new ChatMessage { sender = SenderType.System, type = MessageType.SystemAlert, content = $"【{sourceName}】到账 ${amount}。", timeStamp = "Now" });
        bankHistory.hasUnread = true;
        RefreshContactList();
        SaveManager.Instance.SaveCurrentGame();
    }
    public void CloseChatWindow() { if (chatPanel) chatPanel.SetActive(false); currentActiveContactId = ""; }
    public void ExitToMap()
    {
        BGMManager.Instance.bgm.Stop();
        TransitionManager.Instance.SwitchScene("01_MainUI");
    }
}