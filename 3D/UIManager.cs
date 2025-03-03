using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class UIManager : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button[] difficultyButtons;
    [SerializeField] private Button[] courtTypeButtons;
    
    [Header("Game UI")]
    [SerializeField] private GameObject gameUIPanel;
    [SerializeField] private GameObject scorePanel;
    [SerializeField] private GameObject serveInfoPanel;
    
    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI opponentNameText;
    [SerializeField] private TextMeshProUGUI[] playerSetTexts;
    [SerializeField] private TextMeshProUGUI[] opponentSetTexts;
    [SerializeField] private TextMeshProUGUI playerGameText;
    [SerializeField] private TextMeshProUGUI opponentGameText;
    [SerializeField] private TextMeshProUGUI playerPointText;
    [SerializeField] private TextMeshProUGUI opponentPointText;
    
    [Header("Serve Info")]
    [SerializeField] private TextMeshProUGUI serveInfoText;
    [SerializeField] private Image serveDirectionIndicator;
    
    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitToMenuButton;
    
    [Header("Match Results")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;
    
    [Header("Loading Screen")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TextMeshProUGUI loadingText;
    
    [Header("Options Menu")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Slider soundVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Button backButton;
    
    [Header("Settings")]
    [SerializeField] private string playerName = "Player";
    [SerializeField] private string opponentName = "AI";
    [SerializeField] private Color playerScoreColor = Color.blue;
    [SerializeField] private Color opponentScoreColor = Color.red;
    [SerializeField] private float uiFadeSpeed = 0.5f;
    
    // 参照
    private GameManager gameManager;
    private CanvasGroup mainMenuCanvasGroup;
    private CanvasGroup gameUICanvasGroup;
    private CanvasGroup pauseMenuCanvasGroup;
    private CanvasGroup resultsCanvasGroup;
    
    // コールバック
    public UnityEvent onDifficultySelected = new UnityEvent();
    public UnityEvent onCourtTypeSelected = new UnityEvent();
    
    // 選択されたオプション
    private TennisAI.Difficulty selectedDifficulty = TennisAI.Difficulty.Intermediate;
    private TennisBall.CourtType selectedCourtType = TennisBall.CourtType.Hard;
    
    private void Awake()
    {
        // GameManagerの参照を取得
        gameManager = FindObjectOfType<GameManager>();
        
        // CanvasGroupの取得または追加
        if (mainMenuPanel != null)
        {
            mainMenuCanvasGroup = mainMenuPanel.GetComponent<CanvasGroup>();
            if (mainMenuCanvasGroup == null)
                mainMenuCanvasGroup = mainMenuPanel.AddComponent<CanvasGroup>();
        }
        
        if (gameUIPanel != null)
        {
            gameUICanvasGroup = gameUIPanel.GetComponent<CanvasGroup>();
            if (gameUICanvasGroup == null)
                gameUICanvasGroup = gameUIPanel.AddComponent<CanvasGroup>();
        }
        
        if (pauseMenuPanel != null)
        {
            pauseMenuCanvasGroup = pauseMenuPanel.GetComponent<CanvasGroup>();
            if (pauseMenuCanvasGroup == null)
                pauseMenuCanvasGroup = pauseMenuPanel.AddComponent<CanvasGroup>();
        }
        
        if (resultsPanel != null)
        {
            resultsCanvasGroup = resultsPanel.GetComponent<CanvasGroup>();
            if (resultsCanvasGroup == null)
                resultsCanvasGroup = resultsPanel.AddComponent<CanvasGroup>();
        }
        
        // ボタンイベントの設定
        SetupButtonEvents();
    }
    
    private void Start()
    {
        // 初期UIの表示
        ShowMainMenu();
        HideGameUI();
        HidePauseMenu();
        HideResults();
        HideLoading();
        HideOptions();
        
        // プレイヤー名表示の初期化
        if (playerNameText != null)
            playerNameText.text = playerName;
            
        if (opponentNameText != null)
            opponentNameText.text = opponentName;
    }
    
    // ボタンイベントの設定
    private void SetupButtonEvents()
    {
        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGameClicked);
        
        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnOptionsClicked);
        
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);
        
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);
        
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
        
        if (quitToMenuButton != null)
            quitToMenuButton.onClick.AddListener(OnQuitToMenuClicked);
        
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        
        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);
        
        // 難易度ボタンの設定
        if (difficultyButtons != null)
        {
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                int difficultyIndex = i; // ローカル変数にコピー
                if (difficultyButtons[i] != null)
                    difficultyButtons[i].onClick.AddListener(() => OnDifficultySelected(difficultyIndex));
            }
        }
        
        // コートタイプボタンの設定
        if (courtTypeButtons != null)
        {
            for (int i = 0; i < courtTypeButtons.Length; i++)
            {
                int courtTypeIndex = i; // ローカル変数にコピー
                if (courtTypeButtons[i] != null)
                    courtTypeButtons[i].onClick.AddListener(() => OnCourtTypeSelected(courtTypeIndex));
            }
        }
        
        // スライダーイベントの設定
        if (soundVolumeSlider != null)
            soundVolumeSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
            
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            
        // トグルイベントの設定
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
            
        // ドロップダウンイベントの設定
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionDropdownChanged);
    }
    
    // メインメニューの表示
    public void ShowMainMenu()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            StartCoroutine(FadeCanvasGroup(mainMenuCanvasGroup, 0f, 1f, uiFadeSpeed));
        }
    }
    
    // メインメニューの非表示
    public void HideMainMenu()
    {
        if (mainMenuPanel != null)
        {
            StartCoroutine(FadeCanvasGroup(mainMenuCanvasGroup, 1f, 0f, uiFadeSpeed, () => {
                mainMenuPanel.SetActive(false);
            }));
        }
    }
    
    // ゲームUIの表示
    public void ShowGameUI()
    {
        if (gameUIPanel != null)
        {
            gameUIPanel.SetActive(true);
            StartCoroutine(FadeCanvasGroup(gameUICanvasGroup, 0f, 1f, uiFadeSpeed));
        }
        
        if (scorePanel != null)
            scorePanel.SetActive(true);
    }
    
    // ゲームUIの非表示
    public void HideGameUI()
    {
        if (gameUIPanel != null)
        {
            StartCoroutine(FadeCanvasGroup(gameUICanvasGroup, 1f, 0f, uiFadeSpeed, () => {
                gameUIPanel.SetActive(false);
            }));
        }
    }
    
    // 一時停止メニューの表示
    public void ShowPauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            StartCoroutine(FadeCanvasGroup(pauseMenuCanvasGroup, 0f, 1f, uiFadeSpeed));
        }
    }
    
    // 一時停止メニューの非表示
    public void HidePauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            StartCoroutine(FadeCanvasGroup(pauseMenuCanvasGroup, 1f, 0f, uiFadeSpeed, () => {
                pauseMenuPanel.SetActive(false);
            }));
        }
    }
    
    // 試合結果の表示
    public void ShowMatchResults(bool playerWon, MatchManager.ScoreInfo finalScore)
    {
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(true);
            StartCoroutine(FadeCanvasGroup(resultsCanvasGroup, 0f, 1f, uiFadeSpeed));
        }
        
        // 勝者テキストの設定
        if (winnerText != null)
        {
            winnerText.text = playerWon ? $"{playerName} Wins!" : $"{opponentName} Wins!";
            winnerText.color = playerWon ? playerScoreColor : opponentScoreColor;
        }
        
        // 最終スコアの設定
        if (finalScoreText != null)
        {
            finalScoreText.text = FormatFinalScore(finalScore);
        }
    }
    
    // 試合結果の非表示
    public void HideResults()
    {
        if (resultsPanel != null)
        {
            StartCoroutine(FadeCanvasGroup(resultsCanvasGroup, 1f, 0f, uiFadeSpeed, () => {
                resultsPanel.SetActive(false);
            }));
        }
    }
    
    // ローディング画面の表示
    public void ShowLoading()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(true);
    }
    
    // ローディング画面の非表示
    public void HideLoading()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }
    
    // オプションメニューの表示
    public void ShowOptions()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(true);
            
        // 現在の設定を反映
        UpdateOptionsUI();
    }
    
    // オプションメニューの非表示
    public void HideOptions()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }
    
    // サーブ情報の表示
    public void ShowServeInfo(bool isPlayerServing, int serveSide, MatchManager.ScoreInfo score)
    {
        if (serveInfoPanel != null)
            serveInfoPanel.SetActive(true);
            
        if (serveInfoText != null)
        {
            string serverName = isPlayerServing ? playerName : opponentName;
            string sideText = serveSide == 0 ? "Right" : "Left";
            serveInfoText.text = $"{serverName} Serving from {sideText}";
            serveInfoText.color = isPlayerServing ? playerScoreColor : opponentScoreColor;
        }
        
        if (serveDirectionIndicator != null)
        {
            // サーブ方向の矢印を設定
            serveDirectionIndicator.rectTransform.rotation = Quaternion.Euler(0, 0, isPlayerServing ? 0 : 180);
            serveDirectionIndicator.color = isPlayerServing ? playerScoreColor : opponentScoreColor;
        }
    }
    
    // サーブ情報の非表示
    public void HideServeInfo()
    {
        if (serveInfoPanel != null)
            serveInfoPanel.SetActive(false);
    }
    
    // ローディング画面の進捗更新
    public void UpdateLoadingProgress(float progress)
    {
        if (loadingBar != null)
            loadingBar.value = progress;
            
        if (loadingText != null)
            loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";
    }
    
    // 現在のオプション設定をUIに反映
    private void UpdateOptionsUI()
    {
        if (soundVolumeSlider != null)
            soundVolumeSlider.value = PlayerPrefs.GetFloat("SoundVolume", 0.75f);
            
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
            
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = Screen.fullScreen;
            
        if (resolutionDropdown != null)
        {
            // 現在の解像度を選択
            int currentWidth = Screen.width;
            int currentHeight = Screen.height;
            
            // ドロップダウンの選択肢を設定
            resolutionDropdown.ClearOptions();
            System.Collections.Generic.List<TMP_Dropdown.OptionData> options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
            
            int[] widths = { 1280, 1600, 1920, 2560, 3840 };
            int[] heights = { 720, 900, 1080, 1440, 2160 };
            
            int selectedIndex = 0;
            
            for (int i = 0; i < widths.Length; i++)
            {
                options.Add(new TMP_Dropdown.OptionData($"{widths[i]} x {heights[i]}"));
                
                if (widths[i] == currentWidth && heights[i] == currentHeight)
                    selectedIndex = i;
            }
            
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = selectedIndex;
        }
    }
    
    // スコア表示の更新
    public void UpdateScoreDisplay(MatchManager.ScoreInfo score)
    {
        // ポイント表示
        if (playerPointText != null)
            playerPointText.text = score.playerPointsText;
            
        if (opponentPointText != null)
            opponentPointText.text = score.opponentPointsText;
        
        // ゲーム表示
        if (playerGameText != null)
            playerGameText.text = score.playerGames.ToString();
            
        if (opponentGameText != null)
            opponentGameText.text = score.opponentGames.ToString();
        
        // セット表示
        if (playerSetTexts != null && opponentSetTexts != null)
        {
            int setsToShow = Mathf.Min(score.playerSets.Length, playerSetTexts.Length);
            
            for (int i = 0; i < setsToShow; i++)
            {
                if (i < playerSetTexts.Length)
                    playerSetTexts[i].text = score.playerSets[i].ToString();
                    
                if (i < opponentSetTexts.Length)
                    opponentSetTexts[i].text = score.opponentSets[i].ToString();
            }
        }
        
        // タイブレーク表示をハイライト
        if (score.isTiebreak)
        {
            // タイブレーク表示のスタイル変更
            if (playerPointText != null)
                playerPointText.fontStyle = FontStyles.Bold;
                
            if (opponentPointText != null)
                opponentPointText.fontStyle = FontStyles.Bold;
        }
        else
        {
            // 通常表示のスタイル
            if (playerPointText != null)
                playerPointText.fontStyle = FontStyles.Normal;
                
            if (opponentPointText != null)
                opponentPointText.fontStyle = FontStyles.Normal;
        }
        
        // デュース状態の表示
        if (score.isDeuce)
        {
            // デュース表示のスタイル変更
            if (playerPointText != null && opponentPointText != null)
            {
                if (score.playerAdvantage)
                {
                    playerPointText.fontStyle = FontStyles.Bold;
                    opponentPointText.fontStyle = FontStyles.Normal;
                }
                else if (score.opponentAdvantage)
                {
                    playerPointText.fontStyle = FontStyles.Normal;
                    opponentPointText.fontStyle = FontStyles.Bold;
                }
                else
                {
                    // 40-40のデュース
                    playerPointText.fontStyle = FontStyles.Italic;
                    opponentPointText.fontStyle = FontStyles.Italic;
                }
            }
        }
    }
    
    // 最終スコアのフォーマット
    private string FormatFinalScore(MatchManager.ScoreInfo score)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        // プレイヤー名
        sb.Append(playerName);
        sb.Append(" vs ");
        sb.Append(opponentName);
        sb.Append("\n\n");
        
        // セットスコア
        for (int i = 0; i < score.playerSets.Length; i++)
        {
            if (score.playerSets[i] > 0 || score.opponentSets[i] > 0)
            {
                sb.Append("Set ");
                sb.Append(i + 1);
                sb.Append(": ");
                sb.Append(score.playerSets[i]);
                sb.Append("-");
                sb.Append(score.opponentSets[i]);
                sb.Append("\n");
            }
        }
        
        return sb.ToString();
    }
    
    // CanvasGroupのフェード処理
    private IEnumerator FadeCanvasGroup(CanvasGroup group, float startAlpha, float endAlpha, float duration, System.Action onComplete = null)
    {
        if (group == null) yield break;
        
        float time = 0;
        float startTime = Time.unscaledTime;
        
        group.alpha = startAlpha;
        
        while (time < duration)
        {
            time = Time.unscaledTime - startTime;
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, time / duration);
            yield return null;
        }
        
        group.alpha = endAlpha;
        
        if (onComplete != null)
            onComplete();
    }
    
    #region Button Event Handlers
    
    private void OnNewGameClicked()
    {
        if (gameManager != null)
        {
            // 新しいゲームを開始
            gameManager.StartNewMatch(true, selectedCourtType);
        }
    }
    
    private void OnOptionsClicked()
    {
        HideMainMenu();
        ShowOptions();
    }
    
    private void OnExitClicked()
    {
        if (gameManager != null)
        {
            gameManager.QuitGame();
        }
    }
    
    private void OnResumeClicked()
    {
        if (gameManager != null)
        {
            gameManager.TogglePause();
        }
    }
    
    private void OnRestartClicked()
    {
        if (gameManager != null)
        {
            gameManager.RestartMatch();
            HidePauseMenu();
        }
    }
    
    private void OnQuitToMenuClicked()
    {
        if (gameManager != null)
        {
            gameManager.ReturnToMainMenu();
            HidePauseMenu();
        }
    }
    
    private void OnPlayAgainClicked()
    {
        if (gameManager != null)
        {
            HideResults();
            gameManager.RestartMatch();
        }
    }
    
    private void OnMainMenuClicked()
    {
        if (gameManager != null)
        {
            HideResults();
            gameManager.ReturnToMainMenu();
        }
    }
    
    private void OnBackButtonClicked()
    {
        HideOptions();
        ShowMainMenu();
    }
    
    private void OnDifficultySelected(int difficultyIndex)
    {
        // 難易度の設定
        selectedDifficulty = (TennisAI.Difficulty)difficultyIndex;
        
        // 選択されたボタンをハイライト
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            if (difficultyButtons[i] != null)
            {
                ColorBlock colors = difficultyButtons[i].colors;
                colors.normalColor = (i == difficultyIndex) ? Color.green : Color.white;
                difficultyButtons[i].colors = colors;
            }
        }
        
        // イベント発火
        onDifficultySelected.Invoke();
        
        Debug.Log($"Difficulty set to: {selectedDifficulty}");
    }
    
    private void OnCourtTypeSelected(int courtTypeIndex)
    {
        // コートタイプの設定
        selectedCourtType = (TennisBall.CourtType)courtTypeIndex;
        
        // 選択されたボタンをハイライト
        for (int i = 0; i < courtTypeButtons.Length; i++)
        {
            if (courtTypeButtons[i] != null)
            {
                ColorBlock colors = courtTypeButtons[i].colors;
                colors.normalColor = (i == courtTypeIndex) ? Color.green : Color.white;
                courtTypeButtons[i].colors = colors;
            }
        }
        
        // GameManagerにコートタイプを設定
        if (gameManager != null)
        {
            gameManager.SetCourtType(selectedCourtType);
        }
        
        // イベント発火
        onCourtTypeSelected.Invoke();
        
        Debug.Log($"Court type set to: {selectedCourtType}");
    }
    
    private void OnSoundVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SoundVolume", value);
        // AudioManagerにボリューム変更を通知する実装を追加
    }
    
    private void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        // AudioManagerにボリューム変更を通知する実装を追加
    }
    
    private void OnFullscreenToggleChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
    
    private void OnResolutionDropdownChanged(int index)
    {
        int[] widths = { 1280, 1600, 1920, 2560, 3840 };
        int[] heights = { 720, 900, 1080, 1440, 2160 };
        
        if (index >= 0 && index < widths.Length)
        {
            Screen.SetResolution(widths[index], heights[index], Screen.fullScreen);
        }
    }
    
    #endregion
    
    // 難易度の取得
    public TennisAI.Difficulty GetSelectedDifficulty()
    {
        return selectedDifficulty;
    }
    
    // コートタイプの取得
    public TennisBall.CourtType GetSelectedCourtType()
    {
        return selectedCourtType;
    }
    
    // プレイヤー名の設定
    public void SetPlayerName(string name)
    {
        playerName = name;
        if (playerNameText != null)
            playerNameText.text = playerName;
    }
    
    // 対戦相手名の設定
    public void SetOpponentName(string name)
    {
        opponentName = name;
        if (opponentNameText != null)
            opponentNameText.text = opponentName;
    }
}
