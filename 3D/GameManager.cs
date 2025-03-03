using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // シングルトンインスタンス
    public static GameManager Instance { get; private set; }

    [Header("Game References")]
    [SerializeField] private TennisBall ballPrefab;
    [SerializeField] private Transform[] servePositions;
    [SerializeField] private Transform courtCenter;
    [SerializeField] private GameObject netObject;

    [Header("Players")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TennisAI aiController;

    [Header("Game Settings")]
    [SerializeField] private float serveDelay = 3f;
    [SerializeField] private float pointDelay = 2f;
    [SerializeField] private TennisBall.CourtType courtType = TennisBall.CourtType.Hard;
    
    // 現在のボール
    private TennisBall currentBall;
    
    // ゲーム状態
    public enum GameState { MainMenu, SetupMatch, Serving, Rally, PointScored, GameOver }
    private GameState currentState = GameState.MainMenu;
    
    // 試合情報
    private bool isPlayerServing = true;
    private int servingSide = 0; // 0=右、1=左
    private int currentSet = 0;
    
    // 関連マネージャー
    private MatchManager matchManager;
    private CameraManager cameraManager;
    private UIManager uiManager;
    
    // イベント
    public delegate void GameStateChangedHandler(GameState newState);
    public event GameStateChangedHandler OnGameStateChanged;

    private void Awake()
    {
        // シングルトンの設定
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 関連マネージャーの取得
        matchManager = GetComponent<MatchManager>();
        
        // Nullチェックと警告
        if (matchManager == null)
        {
            Debug.LogError("MatchManager component not found on GameManager object!");
            matchManager = gameObject.AddComponent<MatchManager>();
        }
    }
    
    private void Start()
    {
        // カメラマネージャーとUIマネージャーの参照を取得
        cameraManager = FindObjectOfType<CameraManager>();
        uiManager = FindObjectOfType<UIManager>();
        
        // Nullチェック
        if (cameraManager == null)
            Debug.LogWarning("CameraManager not found in scene!");
            
        if (uiManager == null)
            Debug.LogWarning("UIManager not found in scene!");
        
        // コンポーネント参照のNullチェック
        if (ballPrefab == null)
            Debug.LogError("Ball prefab not assigned to GameManager!");
            
        if (servePositions == null || servePositions.Length < 2)
            Debug.LogError("Serve positions not properly assigned to GameManager!");
            
        if (playerController == null)
            Debug.LogError("PlayerController not assigned to GameManager!");
            
        if (aiController == null)
            Debug.LogError("TennisAI not assigned to GameManager!");
        
        // ゲーム開始時はメインメニューから
        ChangeState(GameState.MainMenu);
    }
    
    // メインメニューからマッチ開始
    public void StartNewMatch(bool playerServesFirst = true, TennisBall.CourtType selectedCourtType = TennisBall.CourtType.Hard)
    {
        isPlayerServing = playerServesFirst;
        servingSide = 0;
        currentSet = 0;
        courtType = selectedCourtType;
        
        // マッチ情報をリセット
        matchManager.ResetMatch();
        
        // マッチセットアップ状態へ
        ChangeState(GameState.SetupMatch);
    }
    
    // 状態遷移処理
    private void ChangeState(GameState newState)
    {
        // 古い状態の終了処理
        switch (currentState)
        {
            case GameState.MainMenu:
                if (uiManager != null) uiManager.HideMainMenu();
                break;
                
            case GameState.Rally:
                // ラリー終了時の処理
                break;
                
            case GameState.Serving:
                // 実行中のコルーチンを停止
                StopAllCoroutines();
                break;
        }
        
        // 新しい状態を設定
        GameState previousState = currentState;
        currentState = newState;
        
        // デバッグログ
        Debug.Log($"Game state changed from {previousState} to {currentState}");
        
        // 新しい状態の開始処理
        switch (currentState)
        {
            case GameState.MainMenu:
                if (uiManager != null) uiManager.ShowMainMenu();
                if (cameraManager != null) cameraManager.SwitchCameraMode(CameraManager.CameraMode.MenuView);
                break;
                
            case GameState.SetupMatch:
                // プレイヤーと環境の準備
                PrepareMatchEnvironment();
                // サービング状態へ
                StartCoroutine(DelayedStateChange(GameState.Serving, 1f));
                break;
                
            case GameState.Serving:
                PrepareForServe();
                break;
                
            case GameState.Rally:
                // ラリー中はAIをアクティブに
                if (aiController != null) aiController.SetActive(true);
                // カメラを試合用に設定
                if (cameraManager != null) cameraManager.SwitchCameraMode(CameraManager.CameraMode.FollowBall);
                break;
                
            case GameState.PointScored:
                // スコア更新
                ProcessPointScored();
                // 次のポイントへ
                StartCoroutine(DelayedStateChange(GameState.Serving, pointDelay));
                break;
                
            case GameState.GameOver:
                // 試合終了処理
                EndMatch();
                break;
        }
        
        // イベント発火
        OnGameStateChanged?.Invoke(currentState);
    }
    
    // 試合環境の準備
    private void PrepareMatchEnvironment()
    {
        // プレイヤーの初期位置を設定
        if (playerController != null && servePositions != null && servePositions.Length > 0)
        {
            playerController.transform.position = servePositions[isPlayerServing ? 0 : 1].position;
        }
        
        if (aiController != null && servePositions != null && servePositions.Length > 1)
        {
            aiController.transform.position = servePositions[isPlayerServing ? 1 : 0].position;
        }
        
        // UIの準備
        if (uiManager != null)
        {
            uiManager.UpdateScoreDisplay(matchManager.GetCurrentScore());
            uiManager.ShowGameUI();
        }
        
        // カメラをセットアップ
        if (cameraManager != null && playerController != null && aiController != null && courtCenter != null)
        {
            cameraManager.SetTargets(playerController.transform, aiController.transform, courtCenter);
        }
    }
    
    // サーブの準備
    private void PrepareForServe()
    {
        // ボールをインスタンス化
        if (currentBall == null && ballPrefab != null)
        {
            currentBall = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
            currentBall.SetCourtType(courtType);
        }
        else if (currentBall != null)
        {
            currentBall.Reset();
            currentBall.SetCourtType(courtType);
        }
        else
        {
            Debug.LogError("Cannot prepare serve: Ball prefab is null!");
            return;
        }
        
        // サーブを行うプレイヤーを特定
        Transform servePosition = null;
        if (servePositions != null && servePositions.Length > servingSide)
        {
            servePosition = servePositions[servingSide];
        }
        else
        {
            Debug.LogError("Cannot prepare serve: Invalid serve position index!");
            return;
        }
        
        // サーブ位置にボールを配置
        currentBall.transform.position = new Vector3(
            servePosition.position.x,
            servePosition.position.y + 1.5f, // 手の高さを考慮
            servePosition.position.z
        );
        
        // サーブカメラに切り替え
        if (cameraManager != null)
        {
            cameraManager.SwitchCameraMode(CameraManager.CameraMode.ServeCam);
            cameraManager.SetupServeCam(isPlayerServing ? playerController.transform : aiController.transform);
        }
        
        // プレイヤーがサーブする場合
        if (isPlayerServing && playerController != null)
        {
            playerController.PrepareForServe(currentBall);
            StartCoroutine(WaitForPlayerServe());
        }
        else if (!isPlayerServing && aiController != null)
        {
            // AIがサーブする場合
            aiController.PrepareForServe(currentBall);
            StartCoroutine(AIServeRoutine());
        }
        else
        {
            Debug.LogError("Cannot prepare serve: Player or AI controller is null!");
            return;
        }
        
        // UIにサーブ情報を表示
        if (uiManager != null)
        {
            uiManager.ShowServeInfo(isPlayerServing, servingSide, matchManager.GetCurrentScore());
        }
    }
    
    // プレイヤーのサーブを待つ
    private IEnumerator WaitForPlayerServe()
    {
        if (playerController == null)
        {
            Debug.LogError("Cannot wait for player serve: PlayerController is null!");
            yield break;
        }
        
        playerController.EnableServeControl(true);
        
        // プレイヤーがサーブするまで待機
        float timeoutCounter = 0f;
        float serveTimeout = 15f; // 15秒のタイムアウト
        
        while (!playerController.HasServed() && timeoutCounter < serveTimeout)
        {
            timeoutCounter += Time.deltaTime;
            yield return null;
        }
        
        playerController.EnableServeControl(false);
        
        // タイムアウトした場合、強制的にサーブ
        if (timeoutCounter >= serveTimeout)
        {
            Debug.LogWarning("Player serve timed out. Forcing serve.");
            playerController.OnServe();
            yield return new WaitForSeconds(1f);
        }
        
        StartRally();
    }
    
    // AIのサーブルーティン
    private IEnumerator AIServeRoutine()
    {
        if (aiController == null)
        {
            Debug.LogError("Cannot execute AI serve: TennisAI is null!");
            yield break;
        }
        
        // サーブ前のAIの準備動作
        yield return new WaitForSeconds(serveDelay);
        
        // AIにサーブを実行させる
        aiController.ExecuteServe();
        
        StartRally();
    }
    
    // ラリー開始
    private void StartRally()
    {
        ChangeState(GameState.Rally);
    }
    
    // アウト判定処理
    public void ProcessOutOfBounds(Vector3 bouncePosition)
    {
        if (currentState != GameState.Rally) return;
        
        // 誰がポイントを獲得したか判定（現在の実装では最後にボールを打った人が負け）
        bool playerScored = !CheckLastHitByPlayer();
        
        // デバッグ用
        Debug.Log("Ball out of bounds. Player scored: " + playerScored);
        
        // スコア更新のためにポイント終了処理
        ChangeState(GameState.PointScored);
    }
    
    // ネット判定処理
    public void ProcessNetHit()
    {
        if (currentState != GameState.Rally && currentState != GameState.Serving) return;
        
        // サーブでのネットタッチはレット（やり直し）、ラリー中はポイント終了
        if (currentState == GameState.Serving)
        {
            // レット処理
            Debug.Log("Let detected. Serve again.");
            PrepareForServe();
        }
        else
        {
            // ポイント獲得判定
            bool playerScored = !CheckLastHitByPlayer();
            Debug.Log("Net hit during rally. Player scored: " + playerScored);
            ChangeState(GameState.PointScored);
        }
    }
    
    // ボールが止まった場合の処理
    public void ProcessBallStopped(Vector3 position, bool lastHitByPlayer)
    {
        if (currentState != GameState.Rally) return;
        
        // 最後に打った人が負け
        bool playerScored = !lastHitByPlayer;
        
        Debug.Log("Ball stopped. Player scored: " + playerScored);
        ChangeState(GameState.PointScored);
    }
    
    // 最後にボールを打ったのがプレイヤーかどうか
    private bool CheckLastHitByPlayer()
    {
        // この実装はボールに最後にヒットした情報を保持する必要がある
        return currentBall != null && currentBall.LastHitByPlayer;
    }
    
    // ポイント獲得処理
    private void ProcessPointScored()
    {
        // スコア更新
        bool playerScored = !CheckLastHitByPlayer();
        matchManager.UpdateScore(playerScored);
        
        // UIスコア表示の更新
        if (uiManager != null)
        {
            uiManager.UpdateScoreDisplay(matchManager.GetCurrentScore());
        }
        
        // サービング情報の更新
        UpdateServeInfo();
        
        // 試合が終了したかチェック
        if (matchManager.IsMatchComplete())
        {
            ChangeState(GameState.GameOver);
        }
    }
    
    // サーブ情報の更新
    private void UpdateServeInfo()
    {
        // ポイント終了後のサーブ交代ルール
        if (matchManager.IsGameComplete())
        {
            // ゲーム終了時はサーブ権が交代
            isPlayerServing = !isPlayerServing;
            servingSide = 0;
        }
        else
        {
            // ポイント終了時はサーブ位置を交代（右/左）
            servingSide = (servingSide == 0) ? 1 : 0;
        }
    }
    
    // 試合終了処理
    private void EndMatch()
    {
        // 勝者を確認
        bool playerWon = matchManager.IsPlayerWinner();
        
        // 結果UIの表示
        if (uiManager != null)
        {
            uiManager.ShowMatchResults(playerWon, matchManager.GetFinalScore());
        }
        
        // クリーンアップ
        if (currentBall != null)
        {
            Destroy(currentBall.gameObject);
            currentBall = null;
        }
        
        // プレイヤーとAIを初期状態に戻す
        if (playerController != null) playerController.ResetPlayer();
        if (aiController != null) aiController.ResetAI();
        
        // カメラをメインメニュー表示に戻す
        if (cameraManager != null)
        {
            cameraManager.SwitchCameraMode(CameraManager.CameraMode.MenuView);
        }
        
        // BGMの変更などその他の終了処理
    }
    
    // ゲームを一時停止/再開
    public void TogglePause()
    {
        if (Time.timeScale > 0)
        {
            Time.timeScale = 0;
            if (uiManager != null) uiManager.ShowPauseMenu();
        }
        else
        {
            Time.timeScale = 1;
            if (uiManager != null) uiManager.HidePauseMenu();
        }
    }
    
    // メインメニューに戻る
    public void ReturnToMainMenu()
    {
        // 時間スケールをリセット
        Time.timeScale = 1;
        
        // もし試合中なら終了処理
        if (currentState != GameState.MainMenu)
        {
            // クリーンアップ
            if (currentBall != null)
            {
                Destroy(currentBall.gameObject);
                currentBall = null;
            }
        }
        
        // メインメニュー状態に戻る
        ChangeState(GameState.MainMenu);
    }
    
    // 再スタート
    public void RestartMatch()
    {
        // 現在のマッチをリセット
        if (currentBall != null)
        {
            Destroy(currentBall.gameObject);
            currentBall = null;
        }
        
        // プレイヤーとAIをリセット
        if (playerController != null) playerController.ResetPlayer();
        if (aiController != null) aiController.ResetAI();
        
        // 新しいマッチを開始
        StartNewMatch(true, courtType);
    }
    
    // コートタイプの変更
    public void SetCourtType(TennisBall.CourtType newCourtType)
    {
        courtType = newCourtType;
        
        // 現在のボールのコートタイプも更新
        if (currentBall != null)
        {
            currentBall.SetCourtType(courtType);
        }
    }
    
    // ゲームの状態を取得
    public GameState GetCurrentState()
    {
        return currentState;
    }
    
    // 遅延して状態を変更するためのコルーチン
    private IEnumerator DelayedStateChange(GameState newState, float delay)
    {
        yield return new WaitForSeconds(delay);
        ChangeState(newState);
    }
    
    // 全シーン/オブジェクトのリセット
    public void FullReset()
    {
        StopAllCoroutines();
        
        // 時間スケールをリセット
        Time.timeScale = 1;
        
        // ボールを削除
        if (currentBall != null)
        {
            Destroy(currentBall.gameObject);
            currentBall = null;
        }
        
        // プレイヤーとAIをリセット
        if (playerController != null) playerController.ResetPlayer();
        if (aiController != null) aiController.ResetAI();
        
        // マッチ情報をリセット
        if (matchManager != null) matchManager.ResetMatch();
        
        // メインメニューに戻る
        ReturnToMainMenu();
    }
    
    // シーン切り替え
    public void LoadScene(string sceneName)
    {
        // 時間スケールをリセット
        Time.timeScale = 1;
        
        // 非同期シーンロード
        StartCoroutine(LoadSceneAsync(sceneName));
    }
    
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // ローディング画面表示
        if (uiManager != null) uiManager.ShowLoading();
        
        // シーンのロード
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        
        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            
            // ローディング進捗の更新
            if (uiManager != null) uiManager.UpdateLoadingProgress(progress);
            
            yield return null;
        }
        
        // ローディング画面非表示
        if (uiManager != null) uiManager.HideLoading();
    }
    
    // アプリケーション終了
    public void QuitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}
