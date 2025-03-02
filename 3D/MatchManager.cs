// MatchManager.cs
using System;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    // テニスのスコア表記（"0", "15", "30", "40", "Ad"）
    private static readonly string[] tennisPoints = { "0", "15", "30", "40", "Ad" };
    
    [Header("Match Settings")]
    [SerializeField] private int gamesPerSet = 6;
    [SerializeField] private int setsToWin = 2;
    [SerializeField] private bool tiebreakEnabled = true;
    [SerializeField] private int tiebreakPoints = 7;
    
    // スコア
    private int playerPoints = 0;
    private int opponentPoints = 0;
    private int playerGames = 0;
    private int opponentGames = 0;
    private int[] playerSets;
    private int[] opponentSets;
    private int currentSet = 0;
    
    // デュース状態
    private bool isDeuce = false;
    private bool playerAdvantage = false;
    private bool opponentAdvantage = false;
    
    // タイブレーク状態
    private bool isTiebreak = false;
    private int playerTiebreakPoints = 0;
    private int opponentTiebreakPoints = 0;
    
    // イベント
    public event Action<ScoreInfo> OnScoreChanged;
    public event Action<ScoreInfo> OnGameComplete;
    public event Action<ScoreInfo> OnSetComplete;
    public event Action<ScoreInfo, bool> OnMatchComplete;
    
    // スコア情報クラス
    [System.Serializable]
    public class ScoreInfo
    {
        public string playerPointsText;
        public string opponentPointsText;
        public int playerGames;
        public int opponentGames;
        public int[] playerSets;
        public int[] opponentSets;
        public int currentSet;
        public bool isTiebreak;
        public bool isDeuce;
        public bool playerAdvantage;
        public bool opponentAdvantage;
    }

    private void Start()
    {
        InitializeMatch();
    }
    
    // 試合の初期化
    public void InitializeMatch()
    {
        playerSets = new int[setsToWin * 2 - 1]; // 最大セット数
        opponentSets = new int[setsToWin * 2 - 1];
        ResetMatch();
    }
    
    // 試合のリセット
    public void ResetMatch()
    {
        // ポイントとゲームをリセット
        playerPoints = 0;
        opponentPoints = 0;
        playerGames = 0;
        opponentGames = 0;
        
        // セットをリセット
        for (int i = 0; i < playerSets.Length; i++)
        {
            playerSets[i] = 0;
            opponentSets[i] = 0;
        }
        
        currentSet = 0;
        
        // 状態をリセット
        isDeuce = false;
        playerAdvantage = false;
        opponentAdvantage = false;
        isTiebreak = false;
        playerTiebreakPoints = 0;
        opponentTiebreakPoints = 0;
        
        // スコア変更イベントを発行
        EmitScoreChangedEvent();
    }
    
    // スコアの更新
    public void UpdateScore(bool playerScored)
    {
        if (isTiebreak)
        {
            UpdateTiebreakScore(playerScored);
        }
        else
        {
            UpdateRegularScore(playerScored);
        }
        
        // スコア変更イベントを発行
        EmitScoreChangedEvent();
    }
    
    // 通常のスコア更新
    private void UpdateRegularScore(bool playerScored)
    {
        if (isDeuce)
        {
            UpdateDeuceScore(playerScored);
            return;
        }
        
        if (playerScored)
        {
            playerPoints++;
        }
        else
        {
            opponentPoints++;
        }
        
        // ポイント40-40でデュース
        if (playerPoints == 3 && opponentPoints == 3)
        {
            isDeuce = true;
            playerPoints = 3;
            opponentPoints = 3;
            return;
        }
        
        // ゲーム終了判定
        if (playerPoints >= 4 && playerPoints >= opponentPoints + 2)
        {
            // プレイヤーがゲーム獲得
            playerGames++;
            CheckForSetComplete();
            ResetPointsForNewGame();
        }
        else if (opponentPoints >= 4 && opponentPoints >= playerPoints + 2)
        {
            // 対戦相手がゲーム獲得
            opponentGames++;
            CheckForSetComplete();
            ResetPointsForNewGame();
        }
    }
    
    // デュース状態のスコア更新
    private void UpdateDeuceScore(bool playerScored)
    {
        if (playerScored)
        {
            if (opponentAdvantage)
            {
                // 相手のアドバンテージを相殺
                opponentAdvantage = false;
            }
            else if (playerAdvantage)
            {
                // プレイヤーがゲーム獲得
                playerGames++;
                CheckForSetComplete();
                ResetPointsForNewGame();
            }
            else
            {
                // プレイヤーがアドバンテージ獲得
                playerAdvantage = true;
            }
        }
        else
        {
            if (playerAdvantage)
            {
                // プレイヤーのアドバンテージを相殺
                playerAdvantage = false;
            }
            else if (opponentAdvantage)
            {
                // 対戦相手がゲーム獲得
                opponentGames++;
                CheckForSetComplete();
                ResetPointsForNewGame();
            }
            else
            {
                // 対戦相手がアドバンテージ獲得
                opponentAdvantage = true;
            }
        }
    }
    
    // タイブレークのスコア更新
    private void UpdateTiebreakScore(bool playerScored)
    {
        if (playerScored)
        {
            playerTiebreakPoints++;
        }
        else
        {
            opponentTiebreakPoints++;
        }
        
        // タイブレーク終了判定
        if (playerTiebreakPoints >= tiebreakPoints && playerTiebreakPoints >= opponentTiebreakPoints + 2)
        {
            // プレイヤーがタイブレーク（セット）獲得
            playerGames++;
            EndSet(true);
        }
        else if (opponentTiebreakPoints >= tiebreakPoints && opponentTiebreakPoints >= playerTiebreakPoints + 2)
        {
            // 対戦相手がタイブレーク（セット）獲得
            opponentGames++;
            EndSet(false);
        }
    }
    
    // 新しいゲームのためのポイントリセット
    private void ResetPointsForNewGame()
    {
        playerPoints = 0;
        opponentPoints = 0;
        isDeuce = false;
        playerAdvantage = false;
        opponentAdvantage = false;
        
        // ゲーム完了イベントを発行
        EmitGameCompleteEvent();
    }
    
    // セット完了チェック
    private void CheckForSetComplete()
    {
        // 通常のセット終了条件
        bool playerWinSet = playerGames >= gamesPerSet && playerGames >= opponentGames + 2;
        bool opponentWinSet = opponentGames >= gamesPerSet && opponentGames >= playerGames + 2;
        
        // タイブレーク条件（6-6）
        if (tiebreakEnabled && playerGames == gamesPerSet && opponentGames == gamesPerSet)
        {
            StartTiebreak();
            return;
        }
        
        // セット終了処理
        if (playerWinSet)
        {
            EndSet(true);
        }
        else if (opponentWinSet)
        {
            EndSet(false);
        }
    }
    
    // タイブレーク開始
    private void StartTiebreak()
    {
        isTiebreak = true;
        playerTiebreakPoints = 0;
        opponentTiebreakPoints = 0;
    }
    
    // セット終了処理
    private void EndSet(bool playerWon)
    {
        // セットスコアを更新
        if (playerWon)
        {
            playerSets[currentSet]++;
        }
        else
        {
            opponentSets[currentSet]++;
        }
        
        // セット完了イベントを発行
        EmitSetCompleteEvent();
        
        // 試合が終了したかチェック
        int playerSetsWon = 0;
        int opponentSetsWon = 0;
        
        for (int i = 0; i < playerSets.Length; i++)
        {
            if (playerSets[i] > opponentSets[i])
            {
                playerSetsWon++;
            }
            else if (opponentSets[i] > playerSets[i])
            {
                opponentSetsWon++;
            }
        }
        
        if (playerSetsWon >= setsToWin || opponentSetsWon >= setsToWin)
        {
            // 試合終了
            bool playerWonMatch = playerSetsWon >= setsToWin;
            EmitMatchCompleteEvent(playerWonMatch);
        }
        else
        {
            // 次のセットへ
            currentSet++;
            playerGames = 0;
            opponentGames = 0;
            ResetPointsForNewGame();
            isTiebreak = false;
        }
    }
    
    // 現在のスコア情報を取得
    public ScoreInfo GetCurrentScore()
    {
        ScoreInfo info = new ScoreInfo
        {
            playerGames = playerGames,
            opponentGames = opponentGames,
            playerSets = playerSets,
            opponentSets = opponentSets,
            currentSet = currentSet,
            isTiebreak = isTiebreak,
            isDeuce = isDeuce,
            playerAdvantage = playerAdvantage,
            opponentAdvantage = opponentAdvantage
        };
        
        // ポイントのテキスト表現
        if (isTiebreak)
        {
            info.playerPointsText = playerTiebreakPoints.ToString();
            info.opponentPointsText = opponentTiebreakPoints.ToString();
        }
        else
        {
            if (isDeuce)
            {
                if (playerAdvantage)
                {
                    info.playerPointsText = "Ad";
                    info.opponentPointsText = "40";
                }
                else if (opponentAdvantage)
                {
                    info.playerPointsText = "40";
                    info.opponentPointsText = "Ad";
                }
                else
                {
                    info.playerPointsText = "40";
                    info.opponentPointsText = "40";
                }
            }
            else
            {
                info.playerPointsText = tennisPoints[Mathf.Min(playerPoints, tennisPoints.Length - 1)];
                info.opponentPointsText = tennisPoints[Mathf.Min(opponentPoints, tennisPoints.Length - 1)];
            }
        }
        
        return info;
    }
    
    // 最終スコア情報を取得
    public ScoreInfo GetFinalScore()
    {
        return GetCurrentScore();
    }
    
    // ゲームが完了したかチェック
    public bool IsGameComplete()
    {
        return playerPoints == 0 && opponentPoints == 0 && !isDeuce;
    }
    
    // 試合が完了したかチェック
    public bool IsMatchComplete()
    {
        int playerSetsWon = 0;
        int opponentSetsWon = 0;
        
        for (int i = 0; i < playerSets.Length; i++)
        {
            if (playerSets[i] > opponentSets[i])
            {
                playerSetsWon++;
            }
            else if (opponentSets[i] > playerSets[i])
            {
                opponentSetsWon++;
            }
        }
        
        return playerSetsWon >= setsToWin || opponentSetsWon >= setsToWin;
    }
    
    // プレイヤーが勝者かどうかチェック
    public bool IsPlayerWinner()
    {
        int playerSetsWon = 0;
        int opponentSetsWon = 0;
        
        for (int i = 0; i < playerSets.Length; i++)
        {
            if (playerSets[i] > opponentSets[i])
            {
                playerSetsWon++;
            }
            else if (opponentSets[i] > playerSets[i])
            {
                opponentSetsWon++;
            }
        }
        
        return playerSetsWon >= setsToWin;
    }
    
    // スコア変更イベント発行
    private void EmitScoreChangedEvent()
    {
        OnScoreChanged?.Invoke(GetCurrentScore());
    }
    
    // ゲーム完了イベント発行
    private void EmitGameCompleteEvent()
    {
        OnGameComplete?.Invoke(GetCurrentScore());
    }
    
    // セット完了イベント発行
    private void EmitSetCompleteEvent()
    {
        OnSetComplete?.Invoke(GetCurrentScore());
    }
    
    // 試合完了イベント発行
    private void EmitMatchCompleteEvent(bool playerWon)
    {
        OnMatchComplete?.Invoke(GetCurrentScore(), playerWon);
    }
}
