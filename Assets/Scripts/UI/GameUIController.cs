using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 管理开始、游戏中和游戏结束三种 UI 状态，并同步分数与剩余生命。
/// </summary>
[DisallowMultipleComponent]
public class GameUIController : MonoBehaviour
{
    [Header("现有界面")]
    [SerializeField] private GameObject startCanvas;
    [SerializeField] private GameObject hudCanvas;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text lifeText;
    [SerializeField] private Button startButton;

    private GameController gameController;
    private GameObject endCanvas;
    private Text finalScoreText;
    private Button retryButton;

    /// <summary>
    /// 取得游戏控制器、建立结算画布并绑定按钮。
    /// </summary>
    private void Start()
    {
        gameController = GetComponent<GameController>();
        CreateEndCanvas();

        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(StartGame);
        }

        if (gameController != null)
        {
            gameController.ScoreChanged += UpdateScore;
            gameController.LivesChanged += UpdateLives;
            gameController.GameEnded += ShowGameEnd;
        }

        ShowStart();
    }

    /// <summary>
    /// 解除事件和按钮监听。
    /// </summary>
    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(StartGame);
        }

        if (gameController != null)
        {
            gameController.ScoreChanged -= UpdateScore;
            gameController.LivesChanged -= UpdateLives;
            gameController.GameEnded -= ShowGameEnd;
        }
    }

    /// <summary>
    /// 显示开始界面并隐藏游戏中和结算界面。
    /// </summary>
    private void ShowStart()
    {
        SetActive(startCanvas, true);
        SetActive(hudCanvas, false);
        SetActive(endCanvas, false);
        UpdateScore(0);
        UpdateLives(0);
    }

    /// <summary>
    /// 响应开始或再玩一次按钮，开始新的一局并切换到 HUD。
    /// </summary>
    private void StartGame()
    {
        if (gameController == null)
        {
            return;
        }

        SetActive(startCanvas, false);
        SetActive(endCanvas, false);
        SetActive(hudCanvas, true);
        gameController.StartGame();
    }

    /// <summary>
    /// 更新游戏中的分数文本。
    /// </summary>
    private void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    /// <summary>
    /// 更新游戏中的剩余生命文本。
    /// </summary>
    private void UpdateLives(int lives)
    {
        if (lifeText != null)
        {
            lifeText.text = lives.ToString();
        }
    }

    /// <summary>
    /// 游戏结束时显示最终分数和再玩一次按钮。
    /// </summary>
    private void ShowGameEnd()
    {
        SetActive(hudCanvas, false);
        SetActive(startCanvas, false);
        SetActive(endCanvas, true);
        if (finalScoreText != null)
        {
            finalScoreText.text = $"得分：{gameController.Score}";
        }
    }

    /// <summary>
    /// 创建独立的响应式结算 Canvas。
    /// </summary>
    private void CreateEndCanvas()
    {
        endCanvas = new GameObject("Canvas_GameEnd", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        endCanvas.transform.SetParent(transform.parent, false);

        Canvas canvas = endCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = endCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Font font = GetUIFont();
        GameObject panel = CreateImage("Panel", endCanvas.transform,
            new Color(0f, 0f, 0f, 0.72f));
        StretchToParent(panel.GetComponent<RectTransform>());

        Text title = CreateText("Text_GameEnd", panel.transform, font, "游戏结束", 110,
            TextAnchor.MiddleCenter);
        SetCenteredRect(title.rectTransform, new Vector2(0f, 150f), new Vector2(1500f, 180f));

        finalScoreText = CreateText("Text_FinalScore", panel.transform, font, "得分：0", 76,
            TextAnchor.MiddleCenter);
        SetCenteredRect(finalScoreText.rectTransform, new Vector2(0f, 0f), new Vector2(1400f, 140f));

        GameObject buttonObject = CreateImage("Button_Retry", panel.transform,
            new Color(0.08f, 0.08f, 0.08f, 0.9f));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.15f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.85f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -210f);
        buttonRect.sizeDelta = new Vector2(0f, 120f);
        retryButton = buttonObject.AddComponent<Button>();
        retryButton.targetGraphic = buttonObject.GetComponent<Image>();

        Text retryText = CreateText("Text_Retry", buttonObject.transform, font, "再玩一次", 64,
            TextAnchor.MiddleCenter);
        StretchToParent(retryText.rectTransform);
        endCanvas.SetActive(false);
    }

    /// <summary>
    /// 使用开始界面的字体，未配置时回退到 Unity 内置字体。
    /// </summary>
    private Font GetUIFont()
    {
        Text sourceText = startButton == null ? null : startButton.GetComponentInChildren<Text>();
        return sourceText != null && sourceText.font != null
            ? sourceText.font
            : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    /// <summary>
    /// 创建一个基础 UI 图片对象。
    /// </summary>
    private static GameObject CreateImage(string objectName, Transform parent, Color color)
    {
        var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        imageObject.GetComponent<Image>().color = color;
        return imageObject;
    }

    /// <summary>
    /// 创建一个基础 Legacy Text 对象。
    /// </summary>
    private static Text CreateText(string objectName, Transform parent, Font font, string content,
        int fontSize, TextAnchor alignment)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>
    /// 将 UI 矩形拉伸至父物体全部区域。
    /// </summary>
    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// 设置一个以画布中心为基准的 UI 矩形。
    /// </summary>
    private static void SetCenteredRect(RectTransform rectTransform, Vector2 position, Vector2 size)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
    }

    /// <summary>
    /// 安全切换对象的激活状态。
    /// </summary>
    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
