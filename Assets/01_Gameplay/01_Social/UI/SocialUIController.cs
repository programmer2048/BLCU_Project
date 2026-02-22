using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 社交聊天界面控制器
/// </summary>
public class SocialUIController : MonoBehaviour
{
    [Header("UI引用")]
    public GameObject chatPanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI contentText;
    public TextMeshProUGUI emotionText;
    public Button choice1Button;
    public Button choice2Button;
    public TextMeshProUGUI choice1Text;
    public TextMeshProUGUI choice2Text;
    public Button closeButton;

    private bool _callbacksBound;

    void Start()
    {
        if (choice1Button != null)
            choice1Button.onClick.AddListener(() => OnChoiceClicked(0));
        if (choice2Button != null)
            choice2Button.onClick.AddListener(() => OnChoiceClicked(1));
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseChat);

        TryBindDialogueCallbacks();
        if (chatPanel != null) chatPanel.SetActive(false);
    }

    private void TryBindDialogueCallbacks()
    {
        if (_callbacksBound || DialogueManager.Instance == null) return;

        DialogueManager.Instance.OnDialogueShow += ShowDialogue;
        DialogueManager.Instance.OnDialogueComplete += OnDialogueEnd;
        _callbacksBound = true;
    }

    private void UnbindDialogueCallbacks()
    {
        if (!_callbacksBound || DialogueManager.Instance == null) return;

        DialogueManager.Instance.OnDialogueShow -= ShowDialogue;
        DialogueManager.Instance.OnDialogueComplete -= OnDialogueEnd;
        _callbacksBound = false;
    }

    void OnEnable()
    {
        EventBus.Subscribe(GameEvent.OnDialogueStart, OnDialogueStartEvent);
        TryBindDialogueCallbacks();
    }

    void OnDisable()
    {
        EventBus.Unsubscribe(GameEvent.OnDialogueStart, OnDialogueStartEvent);
        UnbindDialogueCallbacks();
    }

    private void OnDialogueStartEvent()
    {
        if (chatPanel != null) chatPanel.SetActive(true);
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue();
    }

    private void ShowDialogue(DialogueData data)
    {
        if (speakerText != null) speakerText.text = data.speaker;
        if (contentText != null) contentText.text = data.content;

        if (data.choices != null && data.choices.Length >= 2)
        {
            if (choice1Button != null) choice1Button.gameObject.SetActive(true);
            if (choice2Button != null) choice2Button.gameObject.SetActive(true);
            if (choice1Text != null) choice1Text.text = data.choices[0].text;
            if (choice2Text != null) choice2Text.text = data.choices[1].text;
        }
        else
        {
            if (choice1Button != null) choice1Button.gameObject.SetActive(false);
            if (choice2Button != null) choice2Button.gameObject.SetActive(false);
        }

        UpdateEmotionDisplay();
    }

    private void OnChoiceClicked(int index)
    {
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.MakeChoice(index);
        UpdateEmotionDisplay();
    }

    private void OnDialogueEnd()
    {
        Debug.Log("[SocialUI] 对话结束，返回主界面");
        if (chatPanel != null) chatPanel.SetActive(false);
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToMainUI();
    }

    private void CloseChat()
    {
        if (chatPanel != null) chatPanel.SetActive(false);
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToMainUI();
    }

    private void UpdateEmotionDisplay()
    {
        if (emotionText != null && GameManager.Instance != null)
            emotionText.text = $"好感度: {GameManager.Instance.Emotion}";
    }
}
