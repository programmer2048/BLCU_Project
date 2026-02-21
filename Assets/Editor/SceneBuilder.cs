using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 编辑器工具 —— 自动搭建所有场景的GameObject层级和组件引用
/// 使用方法: Unity菜单 → Tools → Build All Scenes
/// </summary>
public class SceneBuilder
{
    [MenuItem("Tools/Build All Scenes")]
    public static void BuildAllScenes()
    {
        BuildBootScene();
        BuildMainUIScene();
        BuildPartTimeJobScene();
        BuildMiniGamesScene();
        BuildStoryScenesScene();
        BuildAlbumScene();
        
        // 添加所有场景到 Build Settings
        SetupBuildSettings();
        
        Debug.Log("=== 所有场景搭建完毕! ===");
    }

    [MenuItem("Tools/Build Scene/00_Boot")]
    public static void BuildBootScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/04_Scenes/00_Boot.unity");
        ClearScene(scene);

        // Camera
        var cam = CreateCamera();
        
        // EventSystem
        CreateEventSystem();

        // Canvas
        var canvas = CreateCanvas("Canvas");

        var bgPanel = CreatePanel(canvas.transform, "Background", new Color(0.08f, 0.06f, 0.12f, 1f));
        SetRectStretch(bgPanel);

        var titlePanel = CreatePanel(canvas.transform, "TitlePanel", new Color(0f, 0f, 0f, 0.2f));
        SetRectStretch(titlePanel);

        var titleText = CreateTMPText(titlePanel.transform, "TitleText", "山河故人归\n点击开始旅程", 54, TextAlignmentOptions.Center);
        SetRectAnchor(titleText, new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero);

        var startBtn = CreateButton(titlePanel.transform, "StartButton", "开始旅程", 28);
        SetRectAnchor(startBtn, new Vector2(0.3f, 0.25f), new Vector2(0.7f, 0.4f), Vector2.zero, Vector2.zero);
        SetButtonColor(startBtn, new Color(0.2f, 0.55f, 0.85f));

        var quitBtn = CreateButton(titlePanel.transform, "QuitButton", "退出", 24);
        SetRectAnchor(quitBtn, new Vector2(0.38f, 0.12f), new Vector2(0.62f, 0.22f), Vector2.zero, Vector2.zero);
        SetButtonColor(quitBtn, new Color(0.4f, 0.35f, 0.35f));
        
        // Boot Controller
        var bootCtrl = new GameObject("BootController");
        var bc = bootCtrl.AddComponent<BootSceneController>();
        bc.titlePanel = titlePanel;
        bc.titleText = titleText.GetComponent<TextMeshProUGUI>();
        bc.startButton = startBtn.GetComponent<Button>();
        bc.quitButton = quitBtn.GetComponent<Button>();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneBuilder] 00_Boot 搭建完成");
    }

    [MenuItem("Tools/Build Scene/01_MainUI")]
    public static void BuildMainUIScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/04_Scenes/01_MainUI.unity");
        ClearScene(scene);

        CreateCamera();
        CreateEventSystem();
        var canvas = CreateCanvas("Canvas");

        // ====== 顶部状态栏 ======
        var topBar = CreatePanel(canvas.transform, "TopBar", new Color(0.15f, 0.15f, 0.2f, 0.9f));
        SetRectAnchor(topBar, new Vector2(0, 0.92f), Vector2.one, Vector2.zero, Vector2.zero);

        var currencyText = CreateTMPText(topBar.transform, "CurrencyText", "旅费: 0", 28, TextAlignmentOptions.Left);
        SetRectAnchor(currencyText, new Vector2(0.02f, 0), new Vector2(0.4f, 1), Vector2.zero, Vector2.zero);

        var messageBtn = CreateButton(topBar.transform, "MessageButton", "消息", 24);
        SetRectAnchor(messageBtn, new Vector2(0.75f, 0.1f), new Vector2(0.98f, 0.9f), Vector2.zero, Vector2.zero);

        // ====== 中部地图区域 ======
        var mapArea = CreatePanel(canvas.transform, "MapArea", new Color(0.1f, 0.12f, 0.18f, 0.5f));
        SetRectAnchor(mapArea, new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.9f), Vector2.zero, Vector2.zero);

        var mapTitle = CreateTMPText(mapArea.transform, "MapTitle", "— 山西古建之旅 —", 32, TextAlignmentOptions.Top);
        SetRectAnchor(mapTitle, new Vector2(0.1f, 0.85f), new Vector2(0.9f, 1f), Vector2.zero, Vector2.zero);

        // 4个景点按钮
        Button[] spotButtons = new Button[4];
        TextMeshProUGUI[] spotLabels = new TextMeshProUGUI[4];
        Image[] spotImages = new Image[4];
        string[] spotNames = { "应县木塔 [锁]100", "佛光寺 [锁]200", "南禅寺 [锁]300", "晋祠 [锁]400" };
        
        for (int i = 0; i < 4; i++)
        {
            float yMin = 0.6f - i * 0.2f;
            float yMax = yMin + 0.18f;

            var spotPanel = CreatePanel(mapArea.transform, $"SpotPanel_{i}", Color.gray);
            SetRectAnchor(spotPanel, new Vector2(0.1f, yMin), new Vector2(0.9f, yMax), Vector2.zero, Vector2.zero);

            var spotBtn = spotPanel.AddComponent<Button>();
            spotButtons[i] = spotBtn;
            spotImages[i] = spotPanel.GetComponent<Image>();

            var label = CreateTMPText(spotPanel.transform, $"SpotLabel_{i}", spotNames[i], 24, TextAlignmentOptions.Center);
            SetRectStretch(label);
            spotLabels[i] = label.GetComponent<TextMeshProUGUI>();
        }

        // ====== 底部导航栏 ======
        var bottomBar = CreatePanel(canvas.transform, "BottomBar", new Color(0.15f, 0.15f, 0.2f, 0.9f));
        SetRectAnchor(bottomBar, Vector2.zero, new Vector2(1, 0.1f), Vector2.zero, Vector2.zero);

        var workBtn = CreateButton(bottomBar.transform, "WorkButton", "打工", 24);
        SetRectAnchor(workBtn, new Vector2(0.05f, 0.1f), new Vector2(0.3f, 0.9f), Vector2.zero, Vector2.zero);
        SetButtonColor(workBtn, new Color(0.2f, 0.5f, 0.8f));

        var albumBtn = CreateButton(bottomBar.transform, "AlbumButton", "相簿", 24);
        SetRectAnchor(albumBtn, new Vector2(0.35f, 0.1f), new Vector2(0.65f, 0.9f), Vector2.zero, Vector2.zero);
        SetButtonColor(albumBtn, new Color(0.6f, 0.4f, 0.7f));

        var endingBtn = CreateButton(bottomBar.transform, "EndingButton", "结局", 24);
        SetRectAnchor(endingBtn, new Vector2(0.7f, 0.1f), new Vector2(0.95f, 0.9f), Vector2.zero, Vector2.zero);
        SetButtonColor(endingBtn, new Color(0.8f, 0.3f, 0.3f));
        endingBtn.SetActive(false);

        // ====== 聊天面板（默认隐藏）======
        var chatPanel = CreatePanel(canvas.transform, "ChatPanel", new Color(0.1f, 0.1f, 0.15f, 0.95f));
        SetRectAnchor(chatPanel, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f), Vector2.zero, Vector2.zero);
        chatPanel.SetActive(false);

        var speakerText = CreateTMPText(chatPanel.transform, "SpeakerText", "说话人", 28, TextAlignmentOptions.TopLeft);
        SetRectAnchor(speakerText, new Vector2(0.05f, 0.85f), new Vector2(0.5f, 0.95f), Vector2.zero, Vector2.zero);

        var contentText = CreateTMPText(chatPanel.transform, "ContentText", "对话内容...", 22, TextAlignmentOptions.TopLeft);
        SetRectAnchor(contentText, new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.83f), Vector2.zero, Vector2.zero);

        var emotionText = CreateTMPText(chatPanel.transform, "EmotionText", "好感度: 0", 20, TextAlignmentOptions.TopRight);
        SetRectAnchor(emotionText, new Vector2(0.6f, 0.85f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);

        var choice1Btn = CreateButton(chatPanel.transform, "Choice1Button", "选项A", 20);
        SetRectAnchor(choice1Btn, new Vector2(0.05f, 0.2f), new Vector2(0.48f, 0.38f), Vector2.zero, Vector2.zero);

        var choice2Btn = CreateButton(chatPanel.transform, "Choice2Button", "选项B", 20);
        SetRectAnchor(choice2Btn, new Vector2(0.52f, 0.2f), new Vector2(0.95f, 0.38f), Vector2.zero, Vector2.zero);

        var closeBtn = CreateButton(chatPanel.transform, "CloseButton", "关闭", 20);
        SetRectAnchor(closeBtn, new Vector2(0.35f, 0.05f), new Vector2(0.65f, 0.18f), Vector2.zero, Vector2.zero);

        // ====== 管理器对象 ======
        var socialUIObj = new GameObject("SocialUIManager");
        var socialUI = socialUIObj.AddComponent<SocialUIController>();
        socialUI.chatPanel = chatPanel;
        socialUI.speakerText = speakerText.GetComponentInChildren<TextMeshProUGUI>();
        socialUI.contentText = contentText.GetComponentInChildren<TextMeshProUGUI>();
        socialUI.emotionText = emotionText.GetComponentInChildren<TextMeshProUGUI>();
        socialUI.choice1Button = choice1Btn.GetComponent<Button>();
        socialUI.choice2Button = choice2Btn.GetComponent<Button>();
        socialUI.choice1Text = choice1Btn.GetComponentInChildren<TextMeshProUGUI>();
        socialUI.choice2Text = choice2Btn.GetComponentInChildren<TextMeshProUGUI>();
        socialUI.closeButton = closeBtn.GetComponent<Button>();

        var dialogueMgrObj = new GameObject("DialogueManager");
        dialogueMgrObj.AddComponent<DialogueManager>();

        var emotionSysObj = new GameObject("EmotionSystem");
        emotionSysObj.AddComponent<EmotionSystem>();

        // ====== 主控制器 ======
        var mainCtrl = new GameObject("MainUIController");
        var ctrl = mainCtrl.AddComponent<MainUISceneController>();
        ctrl.currencyText = currencyText.GetComponent<TextMeshProUGUI>();
        ctrl.messageButton = messageBtn.GetComponent<Button>();
        ctrl.workButton = workBtn.GetComponent<Button>();
        ctrl.albumButton = albumBtn.GetComponent<Button>();
        ctrl.spotButtons = spotButtons;
        ctrl.spotLabels = spotLabels;
        ctrl.spotImages = spotImages;
        ctrl.chatPanel = chatPanel;
        ctrl.socialUI = socialUI;
        ctrl.endingButton = endingBtn.GetComponent<Button>();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneBuilder] 01_MainUI 搭建完成");
    }

    [MenuItem("Tools/Build Scene/02_PartTimeJobs")]
    public static void BuildPartTimeJobScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/04_Scenes/02_PartTimeJobs.unity");
        ClearScene(scene);

        CreateCamera();
        CreateEventSystem();
        var canvas = CreateCanvas("Canvas");

        // ====== 选择面板 ======
        var selPanel = CreatePanel(canvas.transform, "SelectionPanel", new Color(0.1f, 0.12f, 0.18f, 0.9f));
        SetRectStretch(selPanel);

        var selTitle = CreateTMPText(selPanel.transform, "SelectionTitle", "选择打工地点", 36, TextAlignmentOptions.Top);
        SetRectAnchor(selTitle, new Vector2(0.1f, 0.75f), new Vector2(0.9f, 0.95f), Vector2.zero, Vector2.zero);

        var match3Btn = CreateButton(selPanel.transform, "Match3Button", "拾光餐厅\n(三消游戏)", 24);
        SetRectAnchor(match3Btn, new Vector2(0.1f, 0.4f), new Vector2(0.45f, 0.7f), Vector2.zero, Vector2.zero);
        SetButtonColor(match3Btn, new Color(0.3f, 0.6f, 0.4f));

        var rhythmBtn = CreateButton(selPanel.transform, "RhythmButton", "远山民宿\n(音游)", 24);
        SetRectAnchor(rhythmBtn, new Vector2(0.55f, 0.4f), new Vector2(0.9f, 0.7f), Vector2.zero, Vector2.zero);
        SetButtonColor(rhythmBtn, new Color(0.5f, 0.3f, 0.7f));

        var backBtn = CreateButton(selPanel.transform, "BackButton", "返回地图", 22);
        SetRectAnchor(backBtn, new Vector2(0.3f, 0.1f), new Vector2(0.7f, 0.25f), Vector2.zero, Vector2.zero);

        // ====== Match3 面板 (默认隐藏) ======
        var match3Panel = CreatePanel(canvas.transform, "Match3Panel", new Color(0.08f, 0.1f, 0.15f, 0.95f));
        SetRectStretch(match3Panel);
        match3Panel.SetActive(false);

        var m3Title = CreateTMPText(match3Panel.transform, "M3Title", "拾光餐厅 — 三消", 28, TextAlignmentOptions.Top);
        SetRectAnchor(m3Title, new Vector2(0.05f, 0.92f), new Vector2(0.6f, 1f), Vector2.zero, Vector2.zero);

        var m3TimerText = CreateTMPText(match3Panel.transform, "M3TimerText", "时间: 60", 22, TextAlignmentOptions.TopRight);
        SetRectAnchor(m3TimerText, new Vector2(0.6f, 0.92f), new Vector2(0.95f, 1f), Vector2.zero, Vector2.zero);

        var m3ScoreText = CreateTMPText(match3Panel.transform, "M3ScoreText", "分数: 0", 22, TextAlignmentOptions.TopLeft);
        SetRectAnchor(m3ScoreText, new Vector2(0.05f, 0.85f), new Vector2(0.5f, 0.92f), Vector2.zero, Vector2.zero);

        var m3GoalText = CreateTMPText(match3Panel.transform, "M3GoalText", "目标: -", 20, TextAlignmentOptions.TopLeft);
        SetRectAnchor(m3GoalText, new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.85f), Vector2.zero, Vector2.zero);

        var m3ToolText = CreateTMPText(match3Panel.transform, "M3ToolText", "右键重排/空格提示", 18, TextAlignmentOptions.TopLeft);
        SetRectAnchor(m3ToolText, new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.79f), Vector2.zero, Vector2.zero);

        var m3ProgressText = CreateTMPText(match3Panel.transform, "M3ProgressText", "进度: 0/1000 | 完成轮次: 0", 18, TextAlignmentOptions.TopLeft);
        SetRectAnchor(m3ProgressText, new Vector2(0.05f, 0.69f), new Vector2(0.95f, 0.74f), Vector2.zero, Vector2.zero);

        var progressBarBg = CreatePanel(match3Panel.transform, "M3ProgressBarBg", new Color(0.2f, 0.2f, 0.25f, 0.9f));
        SetRectAnchor(progressBarBg, new Vector2(0.05f, 0.655f), new Vector2(0.95f, 0.685f), Vector2.zero, Vector2.zero);
        var progressBarFill = CreatePanel(progressBarBg.transform, "M3ProgressBarFill", new Color(0.2f, 0.7f, 0.35f, 0.95f));
        SetRectAnchor(progressBarFill, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        var gridContainer = new GameObject("GridContainer");
        gridContainer.transform.SetParent(match3Panel.transform, false);
        var gridRect = gridContainer.AddComponent<RectTransform>();
        gridContainer.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.8f);
        SetRectAnchor(gridContainer, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.83f), Vector2.zero, Vector2.zero);

        var m3ResultPanel = CreatePanel(match3Panel.transform, "M3ResultPanel", new Color(0, 0, 0, 0.85f));
        SetRectStretch(m3ResultPanel);
        m3ResultPanel.SetActive(false);
        var m3ResultText = CreateTMPText(m3ResultPanel.transform, "M3ResultText", "结算中...", 36, TextAlignmentOptions.Center);
        SetRectStretch(m3ResultText);
        var m3BackBtn = CreateButton(m3ResultPanel.transform, "M3BackButton", "返回", 22);
        SetRectAnchor(m3BackBtn, new Vector2(0.3f, 0.1f), new Vector2(0.7f, 0.25f), Vector2.zero, Vector2.zero);

        // Match3 Controller 组件
        var match3CtrlObj = new GameObject("Match3System");
        match3CtrlObj.transform.SetParent(match3Panel.transform, false);
        var m3Grid = match3CtrlObj.AddComponent<Match3Grid>();
        m3Grid.gridContainer = gridRect;
        var m3Judge = match3CtrlObj.AddComponent<Match3Judge>();
        var m3Ctrl = match3CtrlObj.AddComponent<Match3Controller>();
        m3Ctrl.grid = m3Grid;
        m3Ctrl.judge = m3Judge;
        m3Ctrl.timerText = m3TimerText.GetComponent<TextMeshProUGUI>();
        m3Ctrl.scoreText = m3ScoreText.GetComponent<TextMeshProUGUI>();
        m3Ctrl.goalText = m3GoalText.GetComponent<TextMeshProUGUI>();
        m3Ctrl.toolText = m3ToolText.GetComponent<TextMeshProUGUI>();
        m3Ctrl.progressText = m3ProgressText.GetComponent<TextMeshProUGUI>();
        m3Ctrl.progressFill = progressBarFill.GetComponent<Image>();
        m3Ctrl.resultText = m3ResultText.GetComponent<TextMeshProUGUI>();
        m3Ctrl.resultPanel = m3ResultPanel;
        m3Ctrl.backButton = m3BackBtn.GetComponent<Button>();

        // ====== Rhythm 面板 (默认隐藏) ======
        var rhythmPanel = CreatePanel(canvas.transform, "RhythmPanel", new Color(0.08f, 0.05f, 0.12f, 0.95f));
        SetRectStretch(rhythmPanel);
        rhythmPanel.SetActive(false);

        var rTitle = CreateTMPText(rhythmPanel.transform, "RTitle", "远山民宿 — 音游", 28, TextAlignmentOptions.Top);
        SetRectAnchor(rTitle, new Vector2(0.05f, 0.92f), new Vector2(0.6f, 1f), Vector2.zero, Vector2.zero);

        var rTimerText = CreateTMPText(rhythmPanel.transform, "RTimerText", "时间: 45", 22, TextAlignmentOptions.TopRight);
        SetRectAnchor(rTimerText, new Vector2(0.6f, 0.92f), new Vector2(0.95f, 1f), Vector2.zero, Vector2.zero);

        var rScoreText = CreateTMPText(rhythmPanel.transform, "RScoreText", "分数: 0", 22, TextAlignmentOptions.TopLeft);
        SetRectAnchor(rScoreText, new Vector2(0.05f, 0.85f), new Vector2(0.5f, 0.92f), Vector2.zero, Vector2.zero);

        var rJudgeText = CreateTMPText(rhythmPanel.transform, "RJudgeText", "", 30, TextAlignmentOptions.Center);
        SetRectAnchor(rJudgeText, new Vector2(0.3f, 0.6f), new Vector2(0.7f, 0.75f), Vector2.zero, Vector2.zero);

        // 音符下落区域
        var spawnArea = new GameObject("SpawnArea");
        spawnArea.transform.SetParent(rhythmPanel.transform, false);
        var spawnRect = spawnArea.AddComponent<RectTransform>();
        spawnArea.AddComponent<Image>().color = new Color(0.05f, 0.03f, 0.08f, 0.6f);
        SetRectAnchor(spawnArea, new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.83f), Vector2.zero, Vector2.zero);

        // 判定线
        var judgeLineObj = new GameObject("JudgeLine");
        judgeLineObj.transform.SetParent(rhythmPanel.transform, false);
        var judgeLineRect = judgeLineObj.AddComponent<RectTransform>();
        var judgeLineImg = judgeLineObj.AddComponent<Image>();
        judgeLineImg.color = new Color(1f, 0.8f, 0.2f, 0.8f);
        SetRectAnchor(judgeLineObj, new Vector2(0.1f, 0.22f), new Vector2(0.9f, 0.23f), Vector2.zero, Vector2.zero);

        // 4个轨道按钮
        Button[] trackButtons = new Button[4];
        string[] trackKeys = { "D", "F", "J", "K" };
        Color[] trackColors = { Color.red, Color.blue, Color.green, Color.yellow };
        for (int i = 0; i < 4; i++)
        {
            float xMin = 0.1f + i * 0.2f;
            var tBtn = CreateButton(rhythmPanel.transform, $"TrackButton_{i}", trackKeys[i], 28);
            SetRectAnchor(tBtn, new Vector2(xMin, 0.05f), new Vector2(xMin + 0.18f, 0.18f), Vector2.zero, Vector2.zero);
            SetButtonColor(tBtn, trackColors[i] * 0.6f);
            trackButtons[i] = tBtn.GetComponent<Button>();
        }

        var rResultPanel = CreatePanel(rhythmPanel.transform, "RResultPanel", new Color(0, 0, 0, 0.85f));
        SetRectStretch(rResultPanel);
        rResultPanel.SetActive(false);
        var rResultText = CreateTMPText(rResultPanel.transform, "RResultText", "结算中...", 36, TextAlignmentOptions.Center);
        SetRectStretch(rResultText);
        var rBackBtn = CreateButton(rResultPanel.transform, "RBackButton", "返回", 22);
        SetRectAnchor(rBackBtn, new Vector2(0.3f, 0.1f), new Vector2(0.7f, 0.25f), Vector2.zero, Vector2.zero);

        // Rhythm Controller 组件
        var rhythmCtrlObj = new GameObject("RhythmSystem");
        rhythmCtrlObj.transform.SetParent(rhythmPanel.transform, false);
        var rSpawner = rhythmCtrlObj.AddComponent<RhythmSpawner>();
        rSpawner.spawnArea = spawnRect;
        var rJudge = rhythmCtrlObj.AddComponent<RhythmJudge>();
        var rCtrl = rhythmCtrlObj.AddComponent<RhythmController>();
        rCtrl.spawner = rSpawner;
        rCtrl.judge = rJudge;
        rCtrl.scoreText = rScoreText.GetComponent<TextMeshProUGUI>();
        rCtrl.judgeText = rJudgeText.GetComponent<TextMeshProUGUI>();
        rCtrl.timerText = rTimerText.GetComponent<TextMeshProUGUI>();
        rCtrl.resultText = rResultText.GetComponent<TextMeshProUGUI>();
        rCtrl.resultPanel = rResultPanel;
        rCtrl.trackButtons = trackButtons;
        rCtrl.backButton = rBackBtn.GetComponent<Button>();
        rCtrl.judgeLine = judgeLineImg;

        // ====== 场景控制器 ======
        var ptjCtrl = new GameObject("PartTimeJobController");
        var ptj = ptjCtrl.AddComponent<PartTimeJobSceneController>();
        ptj.selectionPanel = selPanel;
        ptj.match3Button = match3Btn.GetComponent<Button>();
        ptj.rhythmButton = rhythmBtn.GetComponent<Button>();
        ptj.backButton = backBtn.GetComponent<Button>();
        ptj.match3Panel = match3Panel;
        ptj.rhythmPanel = rhythmPanel;
        ptj.match3Controller = m3Ctrl;
        ptj.rhythmController = rCtrl;

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneBuilder] 02_PartTimeJobs 搭建完成");
    }

    [MenuItem("Tools/Build Scene/03_MiniGames")]
    public static void BuildMiniGamesScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/04_Scenes/03_MiniGames.unity");
        ClearScene(scene);

        CreateCamera();
        CreateEventSystem();
        var canvas = CreateCanvas("Canvas");

        // 通用标题
        var titleText = CreateTMPText(canvas.transform, "GameTitle", "小游戏", 32, TextAlignmentOptions.Top);
        SetRectAnchor(titleText, new Vector2(0.05f, 0.93f), new Vector2(0.95f, 1f), Vector2.zero, Vector2.zero);

        var commonBackBtn = CreateButton(canvas.transform, "CommonBackButton", "返回地图", 20);
        SetRectAnchor(commonBackBtn, new Vector2(0.02f, 0.93f), new Vector2(0.18f, 1f), Vector2.zero, Vector2.zero);

        // ====== MortiseTenon 面板 ======
        var mtPanel = CreateMiniGamePanel(canvas.transform, "MortiseTenonPanel", "榫卯大师", out var mtTimer, out var mtStatus, out var mtResultPanel, out var mtResultText, out var mtBackBtn, out var mtPlayArea);
        var mtObj = new GameObject("MortiseTenonSystem");
        mtObj.transform.SetParent(mtPanel.transform, false);
        var mt = mtObj.AddComponent<MortiseTenon>();
        mt.playArea = mtPlayArea.GetComponent<RectTransform>();
        mt.timerText = mtTimer.GetComponent<TextMeshProUGUI>();
        mt.statusText = mtStatus.GetComponent<TextMeshProUGUI>();
        mt.resultPanel = mtResultPanel;
        mt.resultText = mtResultText.GetComponent<TextMeshProUGUI>();
        mt.backButton = mtBackBtn.GetComponent<Button>();

        // ====== FindDifferences 面板 ======
        var fdPanel = CreateMiniGamePanel(canvas.transform, "FindDifferencesPanel", "时光寻宝", out var fdTimer, out var fdStatus, out var fdResultPanel, out var fdResultText, out var fdBackBtn, out var fdPlayArea);
        var fdObj = new GameObject("FindDifferencesSystem");
        fdObj.transform.SetParent(fdPanel.transform, false);
        var fd = fdObj.AddComponent<FindDifferences>();
        fd.playArea = fdPlayArea.GetComponent<RectTransform>();
        fd.timerText = fdTimer.GetComponent<TextMeshProUGUI>();
        fd.statusText = fdStatus.GetComponent<TextMeshProUGUI>();
        fd.resultPanel = fdResultPanel;
        fd.resultText = fdResultText.GetComponent<TextMeshProUGUI>();
        fd.backButton = fdBackBtn.GetComponent<Button>();

        // ====== TimeRestorer 面板 ======
        var trPanel = CreateMiniGamePanel(canvas.transform, "TimeRestorerPanel", "时光修复师", out var trTimer, out var trStatus, out var trResultPanel, out var trResultText, out var trBackBtn, out var trPlayArea);
        var trObj = new GameObject("TimeRestorerSystem");
        trObj.transform.SetParent(trPanel.transform, false);
        var tr = trObj.AddComponent<TimeRestorer>();
        tr.playArea = trPlayArea.GetComponent<RectTransform>();
        tr.statusText = trStatus.GetComponent<TextMeshProUGUI>();
        tr.stepText = trTimer.GetComponent<TextMeshProUGUI>(); // reuse timer slot for step text
        tr.resultPanel = trResultPanel;
        tr.resultText = trResultText.GetComponent<TextMeshProUGUI>();
        tr.backButton = trBackBtn.GetComponent<Button>();

        // ====== CardMatching 面板 ======
        var cmPanel = CreateMiniGamePanel(canvas.transform, "CardMatchingPanel", "侍女心语", out var cmTimer, out var cmStatus, out var cmResultPanel, out var cmResultText, out var cmBackBtn, out var cmPlayArea);
        var cmObj = new GameObject("CardMatchingSystem");
        cmObj.transform.SetParent(cmPanel.transform, false);
        var cm = cmObj.AddComponent<CardMatching>();
        cm.playArea = cmPlayArea.GetComponent<RectTransform>();
        cm.statusText = cmStatus.GetComponent<TextMeshProUGUI>();
        cm.attemptsText = cmTimer.GetComponent<TextMeshProUGUI>(); // reuse timer slot for attempts
        cm.resultPanel = cmResultPanel;
        cm.resultText = cmResultText.GetComponent<TextMeshProUGUI>();
        cm.backButton = cmBackBtn.GetComponent<Button>();

        // ====== 场景控制器 ======
        var mgCtrl = new GameObject("MiniGameController");
        var mgc = mgCtrl.AddComponent<MiniGameSceneController>();
        mgc.mortiseTenonPanel = mtPanel;
        mgc.findDifferencesPanel = fdPanel;
        mgc.timeRestorerPanel = trPanel;
        mgc.cardMatchingPanel = cmPanel;
        mgc.mortiseTenon = mt;
        mgc.findDifferences = fd;
        mgc.timeRestorer = tr;
        mgc.cardMatching = cm;
        mgc.titleText = titleText.GetComponent<TextMeshProUGUI>();
        mgc.backButton = commonBackBtn.GetComponent<Button>();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneBuilder] 03_MiniGames 搭建完成");
    }

    [MenuItem("Tools/Build Scene/04_StoryScenes")]
    public static void BuildStoryScenesScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/04_Scenes/04_StoryScenes.unity");
        ClearScene(scene);

        CreateCamera();
        CreateEventSystem();
        var canvas = CreateCanvas("Canvas");

        // 背景
        var bg = CreatePanel(canvas.transform, "Background", new Color(0.08f, 0.06f, 0.12f, 1f));
        SetRectStretch(bg);

        // 章节标题
        var chapterTitle = CreateTMPText(canvas.transform, "ChapterTitle", "第一章", 36, TextAlignmentOptions.Top);
        SetRectAnchor(chapterTitle, new Vector2(0.1f, 0.85f), new Vector2(0.9f, 0.95f), Vector2.zero, Vector2.zero);

        // 剧情文字
        var storyContent = CreateTMPText(canvas.transform, "StoryContent", "剧情内容...", 24, TextAlignmentOptions.TopLeft);
        SetRectAnchor(storyContent, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.83f), Vector2.zero, Vector2.zero);

        // 继续按钮
        var nextBtn = CreateButton(canvas.transform, "NextButton", "继续 ▶", 22);
        SetRectAnchor(nextBtn, new Vector2(0.55f, 0.08f), new Vector2(0.9f, 0.2f), Vector2.zero, Vector2.zero);

        // 跳过按钮
        var skipBtn = CreateButton(canvas.transform, "SkipButton", "跳过 ⏩", 22);
        SetRectAnchor(skipBtn, new Vector2(0.1f, 0.08f), new Vector2(0.45f, 0.2f), Vector2.zero, Vector2.zero);

        // 结局面板（默认隐藏）
        var endingPanel = CreatePanel(canvas.transform, "EndingPanel", new Color(0, 0, 0, 0.95f));
        SetRectStretch(endingPanel);
        endingPanel.SetActive(false);

        var endingTitle = CreateTMPText(endingPanel.transform, "EndingTitle", "结局", 42, TextAlignmentOptions.Center);
        SetRectAnchor(endingTitle, new Vector2(0.1f, 0.7f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero);

        var endingContent = CreateTMPText(endingPanel.transform, "EndingContent", "", 26, TextAlignmentOptions.Center);
        SetRectAnchor(endingContent, new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.68f), Vector2.zero, Vector2.zero);

        var restartBtn = CreateButton(endingPanel.transform, "RestartButton", "返回开始界面", 24);
        SetRectAnchor(restartBtn, new Vector2(0.25f, 0.08f), new Vector2(0.75f, 0.22f), Vector2.zero, Vector2.zero);

        // ====== 控制器 ======
        var storyCtrl = new GameObject("StoryController");
        var sc = storyCtrl.AddComponent<StorySceneController>();
        sc.chapterTitleText = chapterTitle.GetComponent<TextMeshProUGUI>();
        sc.storyContentText = storyContent.GetComponent<TextMeshProUGUI>();
        sc.nextButton = nextBtn.GetComponent<Button>();
        sc.skipButton = skipBtn.GetComponent<Button>();

        var endingCtrl = storyCtrl.AddComponent<EndingJudgeSystem>();
        endingCtrl.endingPanel = endingPanel;
        endingCtrl.endingTitleText = endingTitle.GetComponent<TextMeshProUGUI>();
        endingCtrl.endingContentText = endingContent.GetComponent<TextMeshProUGUI>();
        endingCtrl.restartButton = restartBtn.GetComponent<Button>();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneBuilder] 04_StoryScenes 搭建完成");
    }

    [MenuItem("Tools/Build Scene/05_Album")]
    public static void BuildAlbumScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/04_Scenes/05_Album.unity");
        ClearScene(scene);

        CreateCamera();
        CreateEventSystem();
        var canvas = CreateCanvas("Canvas");

        // 背景
        var bg = CreatePanel(canvas.transform, "Background", new Color(0.12f, 0.1f, 0.08f, 1f));
        SetRectStretch(bg);

        // 标题
        var albumTitle = CreateTMPText(canvas.transform, "AlbumTitle", "旅行相簿", 36, TextAlignmentOptions.Top);
        SetRectAnchor(albumTitle, new Vector2(0.1f, 0.9f), new Vector2(0.9f, 1f), Vector2.zero, Vector2.zero);

        // 收集数量
        var countText = CreateTMPText(canvas.transform, "CountText", "收集: 0/4", 22, TextAlignmentOptions.TopRight);
        SetRectAnchor(countText, new Vector2(0.6f, 0.9f), new Vector2(0.95f, 1f), Vector2.zero, Vector2.zero);

        // 相簿容器（带Vertical Layout）
        var container = new GameObject("AlbumContainer");
        container.transform.SetParent(canvas.transform, false);
        var containerRect = container.AddComponent<RectTransform>();
        SetRectAnchor(container, new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.88f), Vector2.zero, Vector2.zero);
        var vLayout = container.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 10;
        vLayout.padding = new RectOffset(10, 10, 10, 10);
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = false;

        // 返回按钮
        var backBtn = CreateButton(canvas.transform, "BackButton", "返回地图", 22);
        SetRectAnchor(backBtn, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.1f), Vector2.zero, Vector2.zero);

        // ====== 控制器 ======
        var albumCtrl = new GameObject("AlbumController");
        var ac = albumCtrl.AddComponent<AlbumSceneController>();
        ac.albumContainer = containerRect.transform;
        ac.titleText = albumTitle.GetComponent<TextMeshProUGUI>();
        ac.countText = countText.GetComponent<TextMeshProUGUI>();
        ac.backButton = backBtn.GetComponent<Button>();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneBuilder] 05_Album 搭建完成");
    }

    static void SetupBuildSettings()
    {
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/04_Scenes/00_Boot.unity", true),
            new EditorBuildSettingsScene("Assets/04_Scenes/01_MainUI.unity", true),
            new EditorBuildSettingsScene("Assets/04_Scenes/02_PartTimeJobs.unity", true),
            new EditorBuildSettingsScene("Assets/04_Scenes/03_MiniGames.unity", true),
            new EditorBuildSettingsScene("Assets/04_Scenes/04_StoryScenes.unity", true),
            new EditorBuildSettingsScene("Assets/04_Scenes/05_Album.unity", true),
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("[SceneBuilder] Build Settings 已更新，共 6 个场景");
    }

    // ============ 辅助方法 ============

    static void ClearScene(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        foreach (var go in roots)
        {
            Object.DestroyImmediate(go);
        }
    }

    static GameObject CreateCamera()
    {
        var cam = new GameObject("Main Camera");
        cam.tag = "MainCamera";
        var camera = cam.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        camera.orthographic = true;
        camera.orthographicSize = 5;
        cam.AddComponent<AudioListener>();
        return cam;
    }

    static GameObject CreateEventSystem()
    {
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        return es;
    }

    static GameObject CreateCanvas(string name)
    {
        var canvasObj = new GameObject(name);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        canvasObj.layer = 5; // UI layer
        return canvasObj;
    }

    static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        panel.layer = 5;
        var rect = panel.AddComponent<RectTransform>();
        var img = panel.AddComponent<Image>();
        img.color = color;
        return panel;
    }

    static GameObject CreateTMPText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment)
    {
        var textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        textObj.layer = 5;
        var rect = textObj.AddComponent<RectTransform>();
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.enableAutoSizing = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return textObj;
    }

    static GameObject CreateButton(Transform parent, string name, string label, float fontSize)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        btnObj.layer = 5;
        var rect = btnObj.AddComponent<RectTransform>();
        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.3f);
        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        var textObj = CreateTMPText(btnObj.transform, "Text", label, fontSize, TextAlignmentOptions.Center);
        SetRectStretch(textObj);
        return btnObj;
    }

    static void SetRectStretch(GameObject obj)
    {
        var rect = obj.GetComponent<RectTransform>();
        if (rect == null) rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void SetRectAnchor(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rect = obj.GetComponent<RectTransform>();
        if (rect == null) rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    static void SetButtonColor(GameObject btnObj, Color color)
    {
        var img = btnObj.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    static GameObject CreateMiniGamePanel(Transform parent, string name, string title,
        out GameObject timerText, out GameObject statusText,
        out GameObject resultPanel, out GameObject resultText, out GameObject backButton,
        out GameObject playArea)
    {
        var panel = CreatePanel(parent, name, new Color(0.1f, 0.1f, 0.15f, 0.95f));
        SetRectStretch(panel);
        panel.SetActive(false);

        var titleObj = CreateTMPText(panel.transform, "Title", title, 28, TextAlignmentOptions.TopLeft);
        SetRectAnchor(titleObj, new Vector2(0.05f, 0.92f), new Vector2(0.6f, 1f), Vector2.zero, Vector2.zero);

        timerText = CreateTMPText(panel.transform, "TimerText", "时间: --", 22, TextAlignmentOptions.TopRight);
        SetRectAnchor(timerText, new Vector2(0.6f, 0.92f), new Vector2(0.95f, 1f), Vector2.zero, Vector2.zero);

        statusText = CreateTMPText(panel.transform, "StatusText", "", 22, TextAlignmentOptions.TopLeft);
        SetRectAnchor(statusText, new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.92f), Vector2.zero, Vector2.zero);

        playArea = new GameObject("PlayArea");
        playArea.transform.SetParent(panel.transform, false);
        playArea.layer = 5;
        var paRect = playArea.AddComponent<RectTransform>();
        playArea.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.6f);
        SetRectAnchor(playArea, new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.83f), Vector2.zero, Vector2.zero);

        resultPanel = CreatePanel(panel.transform, "ResultPanel", new Color(0, 0, 0, 0.9f));
        SetRectStretch(resultPanel);
        resultPanel.SetActive(false);

        resultText = CreateTMPText(resultPanel.transform, "ResultText", "", 32, TextAlignmentOptions.Center);
        SetRectStretch(resultText);

        backButton = CreateButton(resultPanel.transform, "BackButton", "返回", 22);
        SetRectAnchor(backButton, new Vector2(0.3f, 0.08f), new Vector2(0.7f, 0.2f), Vector2.zero, Vector2.zero);

        return panel;
    }
}
