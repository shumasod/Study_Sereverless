// UIManager.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button[] difficultyButtons;
    
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
    
    [Header("Settings")]
    [SerializeField] private string playerName = "Player";
    [SerializeField] private string opponentName = "Computer";
    
    // 参照
    private GameManager gameManager;
    
    private void Awake()
    {
        // GameManagerの参照を取得
        gameManager = FindObjectOfType<GameManager>();
        
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
        
        // 難易度ボタンの設定
        if (difficultyButtons != null)
        {
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                int difficultyIndex = i; // ローカル変数にコピー
                difficultyButtons[i].onClick.AddListener(() => OnDifficultySelected(difficultyIndex));
            }
        }
    }
    
    // メインメニューの表示
    public void ShowMainMenu()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }
    
    // メインメニューの非表示
    public void HideMainMenu()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
    }
    
    // ゲームUIの表示
    public void ShowGameUI()
    {
        if (gameUIPanel != null)
            gameUIPanel.SetActive(true);
        
        if (scorePanel != null)
            scorePanel.SetActive(true);
    }
    
    // ゲームUIの非表示
    public void HideGameUI()
    {
        if (gameUIPanel != null)
            gameUIPanel.SetActive(false);
    }
    
    // 一時停止メニューの表示
    public void ShowPauseMenu()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
    }
    
    // 一時停止メニューの非表示
    public void HidePauseMenu()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }
    
    // 試合結果の表示
    public void ShowMatchResults(bool playerWon, MatchManager.ScoreInfo finalScore)
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(true);
        
        // 勝者テキストの設定
        if (winnerText != null)
        {
            winnerText.text = playerWon ? $"{playerName} Wins!" : $"{opponentName} Wins!";
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
            resultsPanel.SetActive(false);
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
    
    // ローディング画面の進捗更新
    public void UpdateLoadingProgress(float progress)
    {
        if (loadingBar != null)
            loadingBar.value = progress;
    }
    
    // スコア表示の更新
    public void UpdateScoreDisplay(MatchManager.ScoreInfo score)
    {
        //
