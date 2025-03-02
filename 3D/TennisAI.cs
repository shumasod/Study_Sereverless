// TennisAI.cs
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class TennisAI : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private Difficulty aiDifficulty = Difficulty.Intermediate;
    
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private Transform racketTransform;
    
    [Header("Movement Settings")]
    [SerializeField] private float baselinePosition = -10f;
    [SerializeField] private float netPosition = -2f;
    [SerializeField] private float sideBoundary = 4.5f;
    [SerializeField] private float positionTolerance = 0.5f;
    
    [Header("AI Behavior")]
    [SerializeField, Range(0, 1)] private float aggressionFactor = 0.5f;
    [SerializeField, Range(0, 1)] private float baselineTendency = 0.7f;
    [SerializeField, Range(0, 1)] private float shotAccuracy = 0.7f;
    [SerializeField, Range(0, 1)] private float reactionSpeed = 0.7f;
    
    // AI状態
    private enum AIState { Idle, Positioning, Approaching, PrepareShot, ExecuteShot, ServePrepare, ServeExecute }
    private AIState currentState = AIState.Idle;
    
    // 現在のターゲット
    private Vector3 targetPosition;
    private TennisBall targetBall;
    
    // アニメーション用ハッシュ値
    private int speedHash;
    private int directionHash;
    private int shotTypeHash;
    private int hitTriggerHash;
    private int chargeHash;
    
    // ショットの情報
    private int selectedShotType = 0; // 0=Flat, 1=Topspin, 2=Backspin, 3=Lob, 4=Smash
    private float shotPower = 10f;
    private float spinFactor = 0.2f;
    private bool isCharging = false;
    private float chargeAmount = 0f;
    
    // AIの難易度を表す列挙型
    public enum Difficulty { Beginner, Intermediate, Advanced, Pro }
    
    // プレイヤー側のコート座標（予測用）
    private float playerCourtZ = 10f; 

    private void Awake()
    {
        // コンポーネント参照の設定
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (navAgent == null) navAgent = GetComponent<NavMeshAgent>();
        
        // アニメーションパラメータのハッシュ値を取得
        speedHash = Animator.StringToHash("Speed");
        directionHash = Animator.StringToHash("Direction");
        shotTypeHash = Animator.StringToHash("ShotType");
        hitTriggerHash = Animator.StringToHash("Hit");
        chargeHash = Animator.StringToHash("ChargeAmount");
        
        // 難易度に基づいてAIパラメータを設定
        SetupAIDifficulty();
    }

    private void Start()
    {
        // 初期状態をアイドルに
        ChangeState(AIState.Idle);
    }

    private void Update()
    {
        // 現在の状態に応じた処理を実行
        switch (currentState)
        {
            case AIState.Idle:
                break;
            case AIState.Positioning:
                UpdatePositioning();
                break;
            case AIState.Approaching:
                UpdateApproaching();
                break;
            case AIState.PrepareShot:
                UpdatePrepareShot();
                break;
            case AIState.ExecuteShot:
                // 実行は主にアニメーションイベントで行われるため、処理なし
                break;
            case AIState.ServePrepare:
                // サーブ準備状態（ボールを投げ上げる前）
                break;
            case AIState.ServeExecute:
                // サーブ実行状態
                break;
        }
        
        // アニメーションの更新
        UpdateAnimations();
    }
    
    // 難易度に基づくAIパラメータの設定
    private void SetupAIDifficulty()
    {
        switch (aiDifficulty)
        {
            case Difficulty.Beginner:
                shotAccuracy = 0.5f;
                reactionSpeed = 0.4f;
                aggressionFactor = 0.3f;
                baselineTendency = 0.9f;
                navAgent.speed = 3f;
                break;
                
            case Difficulty.Intermediate:
                shotAccuracy = 0.7f;
                reactionSpeed = 0.6f;
                aggressionFactor = 0.5f;
                baselineTendency = 0.7f;
                navAgent.speed = 4f;
                break;
                
            case Difficulty.Advanced:
                shotAccuracy = 0.8f;
                reactionSpeed = 0.8f;
                aggressionFactor = 0.7f;
                baselineTendency = 0.6f;
                navAgent.speed = 5f;
                break;
                
            case Difficulty.Pro:
                shotAccuracy = 0.9f;
                reactionSpeed = 0.9f;
                aggressionFactor = 0.8f;
                baselineTendency = 0.5f;
                navAgent.speed = 6f;
                break;
        }
    }
    
    // AIの状態を変更
    private void ChangeState(AIState newState)
    {
        // 現在の状態の終了処理
        switch (currentState)
        {
            case AIState.PrepareShot:
                isCharging = false;
                chargeAmount = 0f;
                animator.SetFloat(chargeHash, 0f);
                break;
        }
        
        // 状態を変更
        currentState = newState;
        
        // 新しい状態の開始処理
        switch (currentState)
        {
            case AIState.Positioning:
                // ベースポジションに戻る
                SetPositioningTarget();
                break;
                
            case AIState.Approaching:
                // ボールに向かって移動
                break;
                
            case AIState.PrepareShot:
                // ショットの準備
                StartCoroutine(ChargeShot());
                break;
                
            case AIState.ExecuteShot:
                // ショットの実行
                ExecuteShot();
                break;
                
            case AIState.ServePrepare:
                // サーブの準備
                animator.SetTrigger("ServePrepare");
                break;
        }
    }
    
    // ポジショニング状態の更新
    private void UpdatePositioning()
    {
        // ターゲット位置に近づいたか
        if (Vector3.Distance(transform.position, targetPosition) < positionTolerance)
        {
            // ポジショニング完了
            navAgent.isStopped = true;
            
            // ボールを監視し続ける
            LookForBall();
        }
    }
    
    // 接近状態の更新
    private void UpdateApproaching()
    {
        if (targetBall == null)
        {
            // ボールが見つからない場合、ポジショニングに戻る
            ChangeState(AIState.Positioning);
            return;
        }
        
        // ボールの位置に移動
        Vector3 predictedPosition = PredictBallPosition();
        navAgent.SetDestination(predictedPosition);
        
        // ボールが打てる距離に入ったか
        if (Vector3.Distance(transform.position, targetBall.transform.position) < 2.0f)
        {
            // 打球準備状態へ
            navAgent.isStopped = true;
            ChangeState(AIState.PrepareShot);
        }
    }
    
    // ショット準備状態の更新
    private void UpdatePrepareShot()
    {
        if (targetBall == null)
        {
            // ボールが見つからない場合、ポジショニングに戻る
            ChangeState(AIState.Positioning);
            return;
        }
        
        // ボールの方向を向く
        Vector3 directionToBall = targetBall.transform.position - transform.position;
        directionToBall.y = 0;
        transform.rotation = Quaternion.LookRotation(directionToBall);
    }
    
    // ボールの監視
    private void LookForBall()
    {
        // AIのコート側にボールがあるか検出
        Collider[] colliders = Physics.OverlapSphere(transform.position, 15f);
        foreach (Collider collider in colliders)
        {
            TennisBall ball = collider.GetComponent<TennisBall>();
            if (ball != null)
            {
                // コートの自分側にあるか確認
                if (ball.transform.position.z < 0) // AIのコート側
                {
                    targetBall = ball;
                    
                    // リアクションスピードに基づいて動き出す
                    StartCoroutine(ReactToBall());
                    return;
                }
            }
        }
    }
    
    // ボールへの反応
    private IEnumerator ReactToBall()
    {
        // リアクションタイム（難易度に基づく）
        float reactionTime = Mathf.Lerp(0.5f, 0.1f, reactionSpeed);
        yield return new WaitForSeconds(reactionTime);
        
        if (targetBall != null)
        {
            // ボールに向かって移動
            ChangeState(AIState.Approaching);
        }
    }
    
    // ボール位置の予測
    private Vector3 PredictBallPosition()
    {
        if (targetBall == null) return transform.position;
        
        // 現在のボールの位置と速度
        Vector3 ballPosition = targetBall.transform.position;
        Vector3 ballVelocity = targetBall.GetComponent<Rigidbody>().velocity;
        
        // 簡易的な予測（実際にはより複雑な軌道計算が必要）
        float timeToIntercept = Mathf.Clamp(Vector3.Distance(transform.position, ballPosition) / navAgent.speed, 0.1f, 2f);
        Vector3 predictedPosition = ballPosition + ballVelocity * timeToIntercept;
        
        // コート内に収める
        predictedPosition.x = Mathf.Clamp(predictedPosition.x, -sideBoundary, sideBoundary);
        predictedPosition.z = Mathf.Clamp(predictedPosition.z, baselinePosition, netPosition);
        
        return predictedPosition;
    }
    
    // ポジショニングのターゲット設定
    private void SetPositioningTarget()
    {
        // ベースライン傾向に基づいて位置を決定
        float zPosition = Mathf.Lerp(netPosition, baselinePosition, baselineTendency);
        
        // x軸は中央付近に
        float xPosition = Random.Range(-2f, 2f);
        
        targetPosition = new Vector3(xPosition, 0, zPosition);
        navAgent.isStopped = false;
        navAgent.SetDestination(targetPosition);
    }
    
    // ショットの充電（パワー溜め）
    private IEnumerator ChargeShot()
    {
        // ショットタイプの選択
        SelectShotType();
        
        // パワーチャージ開始
        isCharging = true;
        chargeAmount = 0f;
        
        // 難易度に応じたチャージ速度
        float chargeSpeed = Mathf.Lerp(0.5f, 1.5f, aiDifficulty == Difficulty.Pro ? 1f : (
