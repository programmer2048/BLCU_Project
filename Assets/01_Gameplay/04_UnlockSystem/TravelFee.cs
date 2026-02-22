using UnityEngine;
using TMPro;

/// <summary>
/// 旅费UI组件 —— 挂载在UI上显示旅费
/// </summary>
public class TravelFee : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _feeText;

    void OnEnable()
    {
        EventBus.Subscribe<int>(GameEvent.OnCurrencyChanged, OnCurrencyChanged);
        RefreshDisplay();
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<int>(GameEvent.OnCurrencyChanged, OnCurrencyChanged);
    }

    private void OnCurrencyChanged(int newAmount)
    {
        if (_feeText != null)
            _feeText.text = $"旅费: {newAmount}";
    }

    public void RefreshDisplay()
    {
        if (_feeText != null && GameManager.Instance != null)
            _feeText.text = $"旅费: {GameManager.Instance.TravelFee}";
    }
}
