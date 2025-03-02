// GameManager.cs
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // シングルトンインスタンス
    public static GameManager Instance { get; private set; }

    [Header("Game References")]
    [SerializeField] private TennisBall ballPrefab;
    [SerializeField] private Transform[] servePositions;
    [SerializeField] private Transform courtCenter;

    [Header("Players")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TennisAI aiController;

    [Header("Game Settings")]
    [SerializeField] private float serveDelay = 3f;
    [SerializeField] private float pointDelay = 2f;
    
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
        cameraManager = FindObjectOfType<CameraManager>();
        uiManager = FindObjectOfType<UIManager>();
    }

    private void Start()
    {
        // ゲーム開始時はメインメニューから
        ChangeState(GameState.MainMenu);
    }
    
    // メインメニューからマッチ開始
    public void StartNewMatch(bool playerServesFirst = true)
    {
        isPlayerServing = playerServesFirst;
        servingSide = 0;
        currentSet = 0;
        
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
                uiManager.HideMainMenu();
                break;
            case GameState.Rally:
                // ラリー終了時の処理
                break;
        }
        
        // 新しい状態を設定
        currentState = newState;
        
        // 新しい状態の開始処理
        switch (currentState)
        {
            case GameState.MainMenu:
                uiManager.ShowMainMenu();
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
                aiController.SetActive(true);
                // カメラを試合用に設定
                cameraManager.SwitchCameraMode(CameraManager.CameraMode.FollowBall);
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
    }
    
    // 試合環境の準備
    private void PrepareMatchEnvironment()
    {
        // プレイヤーの初期位置を設定
        playerController.transform.position = servePositions[isPlayerServing ? 0 : 1].position;
        aiController.transform.position = servePositions[isPlayerServing ? 1 : 0].position;
        
        // UIの準備
        uiManager.UpdateScoreDisplay(matchManager.GetCurrentScore());
        uiManager.ShowGameUI();
        
        // カメラをセットアップ
        cameraManager.SetTargets(playerController.transform, aiController.transform, courtCenter);
    }
    
    // サーブの準備
    private void PrepareForServe()
    {
        // ボールをインスタンス化
        if (currentBall == null)
        {
            currentBall = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
        }
        else
        {
            currentBall.Reset();
        }
        
        // サーブを行うプレイヤーを特定
        Transform servePosition = servePositions[servingSide];
        
        // サーブ位置にボールを配置
        currentBall.transform.position = new Vector3(
            servePosition.position.x,
            servePosition.position.y + 1.5f, // 手の高さを考慮
            servePosition.position.z
        );
        
        // サーブカメラに切り替え
        cameraManager.SwitchCameraMode(CameraManager.CameraMode.ServeCam);
        
        // プレイヤーがサーブする場合
        if (isPlayerServing)
        {
            playerController.PrepareForServe(currentBall);
            StartCoroutine(WaitForPlayerServe());
        }
        else
        {
            // AIがサーブする場合
            aiController.PrepareForServe(currentBall);
            StartCoroutine(AIServeRoutine());
        }
        
        // UIにサーブ情報を表示
        uiManager.ShowServeInfo(isPlayerServing, servingSide, matchManager.GetCurrentScore());
    }
    
    // プレイヤーのサーブを待つ
    private IEnumerator WaitForPlayerServe()
    {
        playerController.EnableServeControl(true);
        
        // プレイヤーがサーブするまで待機
        while (!playerController.HasServed())
        {
            yield return null;
        }
        
        playerController.EnableServeControl(false);
        StartRally();
    }
    
    // AIのサーブルーティン
    private IEnumerator AIServeRoutine()
    {
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
        
        // 誰がポイントを獲得したか判定
        bool playerScored = !isPlayerServing; // サーブ側がミスした場合、相手がポイントを獲得
        
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
            PrepareForServe();
        }
        else
        {
            // ポイント獲得判定
            bool playerScored = !CheckLastHitByPlayer();
            ChangeState(GameState.PointScored);
        }
    }
    
    // 最後にボールを打ったのがプレイヤーかどうか
    private bool CheckLastHitByPlayer()
    {
        // この実装はボールに最後にヒットした情報を保持する必要がある
        return currentBall.LastHitByPlayer;
    }
    
    // ポイント獲得処理
    private void ProcessPointScored()
    {
        // スコア更新
        bool playerScored = !CheckLastHitByPlayer();
        matchManager.UpdateScore(playerScored);
        
        // UIスコア表示の更新
        uiManager.UpdateScoreDisplay(matchManager.GetCurrentScore());
        
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
        if (matchManager.
