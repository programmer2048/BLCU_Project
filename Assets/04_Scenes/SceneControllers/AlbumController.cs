using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AlbumController : MonoBehaviour
{
    [Header("信物按钮 (按章节顺序1-4排列)")]
    public GameObject[] itemButtons;

    [Header("对应的书籍弹窗 (按章节顺序1-4排列)")]
    public GameObject[] bookPanels;

    [Header("背景遮罩层")]
    public Button maskButton;
    public Image bookBk;

    private void Start()
    {
        // 1. 初始化弹窗和遮罩（全关掉）
        CloseAllBooks();

        // 2. 绑定遮罩点击事件
        if (maskButton != null)
        {
            maskButton.onClick.AddListener(CloseAllBooks);
        }

        // 3. 刷新信物的显示状态
        RefreshItemsUnlockState();
    }

    public void Exit()
    {
        SceneManager.LoadScene("01_MainUI");
    }

    /// <summary>
    /// 根据存档进度，决定哪些信物要显示出来
    /// </summary>
    private void RefreshItemsUnlockState()
    {
        // 获取当前存档数据
        GameData data = SaveManager.Instance.CurrentGameData;
        if (data == null)
        {
            Debug.LogWarning("[Album] 没找到存档数据，可能是在测试模式。");
            return;
        }

        // 这里假设：已经打过（或到达）第 N 章，就解锁前 N 个信物
        // 如果你的逻辑是过关才解锁，可以用 data.currentChapter > i+1
        int currentChap = data.currentChapter;

        for (int i = 0; i < itemButtons.Length; i++)
        {
            // 假设信物索引0对应第1章，索引1对应第2章...
            int requiredChapter = i + 1;

            // 判断是否解锁：只要当前章节 >= 需要的章节，就显示信物
            bool isUnlocked = currentChap > requiredChapter;

            // 也可以用你的 collectedInfoIds 来判断：
            // bool isUnlocked = data.HasInfo("ChapterItem_" + requiredChapter);

            itemButtons[i].SetActive(isUnlocked);
        }
    }

    /// <summary>
    /// 打开指定的书籍弹窗（可以在 Button 的 OnClick 事件中绑定此方法）
    /// </summary>
    /// <param name="bookIndex">书籍索引（0, 1, 2, 3）</param>
    public void OpenBook(int bookIndex)
    {
        // 安全校验
        if (bookIndex < 0 || bookIndex >= bookPanels.Length) return;

        // 打开遮罩
        maskButton.gameObject.SetActive(true);
        bookBk.gameObject.SetActive(true);

        // 关掉所有书，只打开点选的那本
        for (int i = 0; i < bookPanels.Length; i++)
        {
            bookPanels[i].SetActive(i == bookIndex);
        }
    }

    /// <summary>
    /// 关闭所有书籍和遮罩
    /// </summary>
    public void CloseAllBooks()
    {
        maskButton.gameObject.SetActive(false);
        bookBk.gameObject.SetActive(false);
        foreach (var book in bookPanels)
        {
            if (book != null) book.SetActive(false);
        }
    }
}