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
        float chargeSpeed = Mathf.Lerp(0.5f, 1.5f, aiDifficulty == Difficulty.Pro ? 1f : 
            (aiDifficulty == Difficulty.Advanced ? 0.8f : 
            (aiDifficulty == Difficulty.Intermediate ? 0.5f : 0.3f)));
        
        // 充電時間
        float chargeDuration = Random.Range(0.2f, 0.8f);
        float timer = 0f;
        
        while (timer < chargeDuration)
        {
            chargeAmount = Mathf.Min(timer / chargeDuration, 1f);
            animator.SetFloat(chargeHash, chargeAmount);
            
            timer += Time.deltaTime * chargeSpeed;
            yield return null;
        }
        
        // 充電完了、ショット実行
        isCharging = false;
        ChangeState(AIState.ExecuteShot);
    }
    
    // ショットタイプの選択
    private void SelectShotType()
    {
        // ボールの高さに基づくショット選択
        float ballHeight = targetBall != null ? targetBall.transform.position.y : 1f;
        
        if (ballHeight > 2.5f)
        {
            // 高いボールはスマッシュの可能性
            selectedShotType = Random.value < aggressionFactor ? 4 : 0; // スマッシュまたはフラット
        }
        else if (ballHeight < 0.5f)
        {
            // 低いボールはロブの可能性
            selectedShotType = Random.value < 0.4f ? 3 : 1; // ロブまたはトップスピン
        }
        else
        {
            // 通常の高さはバリエーションを付ける
            float shotSelection = Random.value;
            
            if (shotSelection < 0.4f)
                selectedShotType = 0; // フラット
            else if (shotSelection < 0.7f)
                selectedShotType = 1; // トップスピン
            else
                selectedShotType = 2; // バックスピン
        }
        
        // アニメーターにショットタイプを設定
        animator.SetInteger(shotTypeHash, selectedShotType);
    }
    
    // ショットの実行
    private void ExecuteShot()
    {
        if (targetBall == null) return;
        
        // ショットパワーの計算（チャージ量に基づく）
        shotPower = Mathf.Lerp(5f, 20f, chargeAmount);
        
        // スピン係数の決定
        switch (selectedShotType)
        {
            case 1: // トップスピン
                spinFactor = 0.8f;
                break;
            case 2: // バックスピン
                spinFactor = -0.5f;
                break;
            case 3: // ロブ
                spinFactor = 0.3f;
                shotPower *= 0.7f; // ロブは少しパワーダウン
                break;
            case 4: // スマッシュ
                spinFactor = 1.0f;
                shotPower *= 1.5f; // スマッシュはパワーアップ
                break;
            default: // フラット
                spinFactor = 0.2f;
                break;
        }
        
        // 目標方向の計算
        Vector3 targetDirection = CalculateTargetDirection();
        
        // ヒットアニメーションのトリガー
        animator.SetTrigger(hitTriggerHash);
        
        // 実際のヒットはアニメーションイベントから呼び出す
        // 本実装ではアニメーションイベントの代わりにコルーチンで処理
        StartCoroutine(DelayedHit(targetDirection));
    }
    
    // 遅延ヒット（アニメーションイベント用）
    private IEnumerator DelayedHit(Vector3 direction)
    {
        // アニメーションに合わせて遅延（約0.2秒）
        yield return new WaitForSeconds(0.2f);
        
        if (targetBall != null && IsInHitRange())
        {
            // ボールを打つ
            targetBall.Hit(direction, shotPower, spinFactor);
            
            // ヒット後の処理
            AfterHit();
        }
    }
    
    // ヒット後の処理
    private void AfterHit()
    {
        // ターゲットボールをクリア
        targetBall = null;
        
        // ポジショニングに戻る
        ChangeState(AIState.Positioning);
    }
    
    // ヒット可能範囲かどうか
    private bool IsInHitRange()
    {
        if (targetBall == null) return false;
        
        // ラケットとボールの距離
        float distance = Vector3.Distance(racketTransform.position, targetBall.transform.position);
        return distance < 2.0f; // ヒット範囲
    }
    
    // 目標方向の計算
    private Vector3 CalculateTargetDirection()
    {
        // ショットの精度に基づく方向のばらつき
        float accuracy = Mathf.Lerp(0.5f, 0.9f, shotAccuracy);
        
        // 基本的な方向（コートの奥へ）
        Vector3 baseDirection = new Vector3(0, 0, 1); // プレイヤー側への方向
        
        // 戦略的なターゲット選択
        Vector3 strategicTarget = SelectStrategicTarget();
        
        // 方向の決定（基本方向と戦略的方向の混合）
        Vector3 direction = Vector3.Lerp(baseDirection, strategicTarget, accuracy);
        
        // 上方向の調整（ロブやスマッシュ）
        if (selectedShotType == 3) // ロブ
        {
            direction.y = 0.8f; // 高い弧を描く
        }
        else if (selectedShotType == 4) // スマッシュ
        {
            direction.y = -0.2f; // 下向きの軌道
        }
        else
        {
            direction.y = Random.Range(0.1f, 0.3f); // 通常の上方向成分
        }
        
        return direction.normalized;
    }
    
    // 戦略的なターゲット選択
    private Vector3 SelectStrategicTarget()
    {
        // プレイヤーから遠い位置を狙う
        Vector3 playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
        
        // コートの四隅を選択肢として
        Vector3[] courtCorners = new Vector3[4]
        {
            new Vector3(-sideBoundary, 0, playerCourtZ - 1), // 左奥
            new Vector3(sideBoundary, 0, playerCourtZ - 1),  // 右奥
            new Vector3(-sideBoundary, 0, 1),                // 左前（ネット際）
            new Vector3(sideBoundary, 0, 1)                  // 右前（ネット際）
        };
        
        // プレイヤーから最も遠い隅を選択
        int bestCornerIndex = 0;
        float maxDistance = 0f;
        
        for (int i = 0; i < courtCorners.Length; i++)
        {
            float distance = Vector3.Distance(playerPosition, courtCorners[i]);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                bestCornerIndex = i;
            }
        }
        
        // アグレッション係数に基づいて、ネット際を狙うかどうか
        if (aggressionFactor > 0.6f && Random.value < aggressionFactor)
        {
            // ネット際を狙う（インデックス2または3）
            bestCornerIndex = Random.value < 0.5f ? 2 : 3;
        }
        
        // ターゲット方向の計算
        Vector3 targetPos = courtCorners[bestCornerIndex];
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0; // 水平方向のみ
        
        return direction;
    }
    
    // アニメーションの更新
    private void UpdateAnimations()
    {
        // 移動速度をアニメーションに反映
        float speed = navAgent.velocity.magnitude / navAgent.speed;
        animator.SetFloat(speedHash, speed, 0.1f, Time.deltaTime);
        
        // 移動方向（左右）をアニメーションに反映
        if (navAgent.velocity.magnitude > 0.1f)
        {
            float direction = Vector3.Dot(transform.right, navAgent.velocity.normalized);
            animator.SetFloat(directionHash, direction, 0.1f, Time.deltaTime);
        }
    }
    
    // サーブの準備
    public void PrepareForServe(TennisBall ball)
    {
        targetBall = ball;
        ChangeState(AIState.ServePrepare);
    }
    
    // サーブの実行
    public void ExecuteServe()
    {
        if (targetBall == null) return;
        
        // サーブのショットタイプは通常フラットかトップスピン
        selectedShotType = Random.value < 0.7f ? 0 : 1;
        animator.SetInteger(shotTypeHash, selectedShotType);
        
        // サーブのパワーとスピン
        shotPower = Mathf.Lerp(10f, 18f, Random.value);
        spinFactor = selectedShotType == 1 ? 0.6f : 0.2f;
        
        // サーブの方向
        Vector3 serveDirection = new Vector3(Random.Range(-0.3f, 0.3f), 0.2f, 1).normalized;
        
        // サーブを打つ
        targetBall.Hit(serveDirection, shotPower, spinFactor);
        
        // サーブ後はポジショニングへ
        ChangeState(AIState.Positioning);
    }
    
    // AIのアクティブ化/非アクティブ化
    public void SetActive(bool active)
    {
        if (active)
        {
            // AIをアクティブにする
            if (currentState == AIState.Idle)
            {
                ChangeState(AIState.Positioning);
            }
        }
        else
        {
            // AIを非アクティブにする
            ChangeState(AIState.Idle);
            navAgent.isStopped = true;
        }
    }
    
    // AIのリセット
    public void ResetAI()
    {
        // 状態をリセット
        ChangeState(AIState.Idle);
        
        // ターゲットをクリア
        targetBall = null;
        
        // エージェントの停止
        navAgent.isStopped = true;
        navAgent.ResetPath();
    }
}
