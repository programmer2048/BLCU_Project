using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [Header("--- 面板引用 ---")]
    public GameObject settingsPanel;

    [Header("--- BGM 设置组件 ---")]
    public Toggle bgmToggle;      // 开关
    public Slider bgmSlider;      // 滑动条
    public TextMeshProUGUI bgmVolumeText; // 显示 BGM 数值

    [Header("--- SFX 设置组件 ---")]
    public Toggle sfxToggle;
    public Slider sfxSlider;
    public TextMeshProUGUI sfxVolumeText; // 显示 SFX 数值

    [Header("--- 其他 ---")]
    public Button closeButton;
    public Button resetSaveButton;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        settingsPanel.SetActive(false);

    }

    private void Start()
    {
        // 绑定事件
        // 使用 OnValueChanged 监听用户操作
        bgmSlider.onValueChanged.AddListener(OnBGMSliderChanged);
        bgmToggle.onValueChanged.AddListener(OnBGMToggleChanged);

        sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        sfxToggle.onValueChanged.AddListener(OnSFXToggleChanged);

        OnBGMToggleChanged(BGMManager.Instance.bgm.volume != 0);
        OnSFXToggleChanged(sfxVolumeText.text!="0");

        closeButton.onClick.AddListener(ToggleSettings);
        if (resetSaveButton) resetSaveButton.onClick.AddListener(OnResetButtonClicked);
    }

    public void ToggleSettings()
    {
        bool isActive = !settingsPanel.activeSelf;
        settingsPanel.SetActive(isActive);
        if (isActive)
        {
            SyncUI();
        }
        else
        {
            SaveManager.Instance.SaveGlobalSettings();

        }
    }

    // --- 核心同步逻辑：将数据应用到 UI 上 ---
    private void SyncUI()
    {
        var config = SaveManager.Instance.GlobalConfig;
        bgmSlider.SetValueWithoutNotify(config.musicVolume);
        bgmToggle.SetIsOnWithoutNotify(config.isMusicOn);
        bgmSlider.interactable = config.isMusicOn;
        UpdateVolumeText(bgmVolumeText, config.musicVolume);
        sfxSlider.SetValueWithoutNotify(config.sfxVolume);
        sfxToggle.SetIsOnWithoutNotify(config.isSfxOn);
        sfxSlider.interactable = config.isSfxOn;
        UpdateVolumeText(sfxVolumeText, config.sfxVolume);
        ApplyVolume();
    }

    // 用户点击开关
    private void OnBGMToggleChanged(bool isOn)
    {
        var config = SaveManager.Instance.GlobalConfig;
        config.isMusicOn = isOn;

        // 关掉开关时，让 Slider 变灰（不可用）
        bgmSlider.interactable = isOn;

        // 如果你想在关闭开关时让文字变灰，可以在这里操作 textMesh.color
        // bgmVolumeText.alpha = isOn ? 1f : 0.5f;

        ApplyVolume();
    }

    // 用户拖动滑块
    private void OnBGMSliderChanged(float value)
    {
        var config = SaveManager.Instance.GlobalConfig;
        config.musicVolume = value;

        // 实时更新文字
        UpdateVolumeText(bgmVolumeText, value);

        // 如果用户把音量拖到0，自动关闭开关
        if (value <= 0.001f && config.isMusicOn)
        {
            config.isMusicOn = false;
            bgmToggle.SetIsOnWithoutNotify(false);
        }
        // 如果用户从0往上拖，自动打开开关
        else if (value > 0.001f && !config.isMusicOn)
        {
            config.isMusicOn = true;
            bgmToggle.SetIsOnWithoutNotify(true);
            bgmSlider.interactable = true; // 确保滑块恢复可交互状态
        }

        ApplyVolume();
    }

    private void OnSFXToggleChanged(bool isOn)
    {
        var config = SaveManager.Instance.GlobalConfig;
        config.isSfxOn = isOn;
        sfxSlider.interactable = isOn;

        // sfxVolumeText.alpha = isOn ? 1f : 0.5f;

        ApplyVolume();
    }

    private void OnSFXSliderChanged(float value)
    {
        var config = SaveManager.Instance.GlobalConfig;
        config.sfxVolume = value;

        UpdateVolumeText(sfxVolumeText, value);

        if (value <= 0.001f && config.isSfxOn)
        {
            config.isSfxOn = false;
            sfxToggle.SetIsOnWithoutNotify(false);
        }
        else if (value > 0.001f && !config.isSfxOn)
        {
            config.isSfxOn = true;
            sfxToggle.SetIsOnWithoutNotify(true);
            sfxSlider.interactable = true;
        }
        ApplyVolume();
    }

    private void UpdateVolumeText(TextMeshProUGUI textComp, float value)
    {
        if (textComp != null)
        {
            // 将 0.0 - 1.0 转换为 0 - 100 的整数
            int intValue = Mathf.RoundToInt(value * 100);
            textComp.text = intValue.ToString();
        }
    }

    // --- 应用音量到游戏引擎 ---
    public void ApplyVolume()
    {
        var config = SaveManager.Instance.GlobalConfig;

        // 最终音量 = 开关状态 ? 设定音量 : 0
        float finalBGM = config.isMusicOn ? config.musicVolume : 0f;
        float finalSFX = config.isSfxOn ? config.sfxVolume : 0f;

        // 设置 BGM
        if (BGMManager.Instance != null)
        {
            BGMManager.Instance.bgm.volume = finalBGM;
        }

        // 设置 SFX
        // AudioManager.Instance.SetSFXVolume(finalSFX);
    }

    private void OnResetButtonClicked()
    {
        SaveManager.Instance.ResetAllSaves();
        settingsPanel.SetActive(false);
    }
}